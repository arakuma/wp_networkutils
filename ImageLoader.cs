using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections.Generic;
using Arakuma.Threading;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace Arakuma.Ui.ImageTool {
    /// <summary>
    /// 图片缓存器基类
    /// </summary>
    public abstract class ImageLoader {
        protected class QueueItem {
            public string              Url;
            public ImageCreateCallback Action;
        }

        /// <summary>
        /// 默认的最大任务数
        /// </summary>
        private readonly int              DEFAULT_MAX_TASKS = 3;

        private AutoResetEvent            _resetEvent;
        private int                       _taskCount;
        private bool                      _isThreadActivated;
        private Queue<QueueItem>          _waitingQueue;
        private List<ICancelableTask>     _tasks;
        private object                    _lock;

        /// <summary>
        /// 构造器
        /// </summary>
        public ImageLoader() {
            _resetEvent = new AutoResetEvent( false );
            _waitingQueue = new Queue<QueueItem>();
            _tasks = new List<ICancelableTask>();
            _lock = new object();
            _taskCount = DEFAULT_MAX_TASKS;
            _isThreadActivated = false;
        }

        /// <summary>
        /// 设置图片到image控件
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <param name="imageControl">控件</param>
        public virtual void SetImage( string url, Image imageControl ) {
            if ( imageControl != null ) {
                imageControl.Source = 
                    Get( url, ( source ) => {
                    if ( !String.IsNullOrWhiteSpace( url ) && imageControl != null && source != null ) {
                        Deployment.Current.Dispatcher.BeginInvoke( () => {
                            imageControl.Source = source;
                        } );
                    }
                } );
            }
        }

        /// <summary>
        /// 获取图片
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <param name="action">图片生成后的回调</param>
        /// <returns>默认图片</returns>
        public virtual BitmapSource Get( string url, ImageCreateCallback action ) {
            QueueItem item = new QueueItem() { Action = action, Url = url };
            BackgroundWorker worker = null;
            lock ( _waitingQueue ) {
                _waitingQueue.Enqueue( item );
                if ( !_isThreadActivated ) {
                    _isThreadActivated = true;
                    worker = new BackgroundWorker();
                }
            }

            if ( worker != null ) {
                worker.DoWork += ( ( send, ev ) => StartThread() );
                worker.RunWorkerCompleted += ( ( s, e ) => {
                    lock ( _waitingQueue ) {
                        _isThreadActivated = false;
                    }
                } );
                worker.RunWorkerAsync();
            }
            return BitmapCache.DEFAULT_IMAGE;
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAll() {
            lock ( _waitingQueue ) {
                _isThreadActivated = false;
                _waitingQueue.Clear();
            }
            lock ( _tasks ) {
                foreach ( ICancelableTask item in _tasks ) {
                    item.Cancel();
                }
            }
        }

        /// <summary>
        /// 新图片下载完毕。
        /// 本来想用event方式进行传递，后来觉得还不如直接用虚函数
        /// </summary>
        /// <param name="bitmap"></param>
        protected virtual void OnNewImageDownloaded( string url, BitmapSource bitmap ) { }

        /// <summary>
        /// 创建相应的任务项目，启动线程
        /// </summary>
        private void StartThread() {
            while ( true ) {
                if ( _taskCount == 0 ) {
                    _resetEvent.WaitOne();
                }
                QueueItem item = null;
                // 取等待队列中的任务
                lock ( _waitingQueue ) {
                    if ( !_isThreadActivated || _waitingQueue.Count == 0 ) {
                        break;
                    }
                    item = _waitingQueue.Dequeue();
                    Interlocked.Decrement( ref _taskCount );
                }

                // 生成task
                ImageHttpGetTask imageTask = GetTask( item );
                imageTask.Start();

                // 加到等待列表里
                lock ( _tasks ) {
                    _tasks.Add( imageTask );
                }

                Thread.Sleep( 1 );
            }
        }

        /// <summary>
        /// 从排队的项目中生成任务，加入下载列表
        /// </summary>
        /// <param name="item">排队项</param>
        /// <returns>对应的任务</returns>
        private ImageHttpGetTask GetTask( QueueItem item ) {
            ImageHttpGetTask imageTask = new ImageHttpGetTask( item.Url );
            imageTask.OnImageCreated += ( bitmap ) => {
                item.Action( bitmap );
                OnNewImageDownloaded( item.Url, bitmap );
            };
            imageTask.TaskCompleted += () => {
                lock ( _tasks ) {
                    _tasks.Remove( imageTask );
                }
                if ( _taskCount == 0 ) {
                    _resetEvent.Set();
                }
                Interlocked.Increment( ref _taskCount );
            };
            return imageTask;
        }
    }
}
