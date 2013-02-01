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
using System.ComponentModel;

namespace Arakuma.NetworkUtil {
    /// <summary>
    /// Task pool for holding running threads in a queue and processing limited requests at the same time
    /// </summary>
    internal class TaskPool {
        protected class QueueItem {
            //for any
            public string                  Url;
            public HttpMethod              Method;
            public HttpRequestCallback     Action;
            //for posting
            public List<KeyValuePair<string, string>> PostData;
            //for posting and file
            public byte[]                     FileData;
            public string                     FileName;
            public string                     FileFieldName;
        }

        /// <summary>
        /// Max request count
        /// </summary>
        private readonly int              DEFAULT_MAX_TASKS = 3;

        private static TaskPool           _instance;
        private AutoResetEvent            _resetEvent;
        private int                       _taskCount;
        private bool                      _isThreadActivated;
        private Queue<QueueItem>          _waitingQueue;
        private List<ICancelableTask>     _tasks;
        private object                    _lock;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static TaskPool Instance {
            get {
                if ( _instance == null ) {
                    lock ( typeof( TaskPool ) ) {
                        if ( _instance == null ) {
                            _instance = new TaskPool();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Construct
        /// </summary>
        private TaskPool() {
            _resetEvent = new AutoResetEvent( false );
            _waitingQueue = new Queue<QueueItem>();
            _tasks = new List<ICancelableTask>();
            _lock = new object();
            _taskCount = DEFAULT_MAX_TASKS;
            _isThreadActivated = false;
        }

        /// <summary>
        /// Try to get web content
        /// </summary>
        /// <param name="url">requested url</param>
        /// <param name="action">callback for request done</param>
        public void Request( string url, HttpMethod method, List<KeyValuePair<string, string>> httpPostData, HttpRequestCallback action ) {
            QueueItem item = new QueueItem() { Action = action, Url = url, Method = method, PostData = httpPostData };
            AddQueueItem( item );
        }

        /// <summary>
        /// Request for uploading a file
        /// </summary>
        /// <param name="url">requested url</param>
        /// <param name="httpPostData">post data</param>
        /// <param name="fileUploadData">file data</param>
        /// <param name="fileFieldName">file name in header</param>
        /// <param name="fileName">file name</param>
        /// <param name="action">call back</param>
        public void RequestUploadFile( string url, List<KeyValuePair<string, string>> httpPostData, byte[] fileUploadData, string fileFieldName, string fileName, HttpRequestCallback action ) {
            QueueItem item = new QueueItem() {
                Method = HttpMethod.Post,
                Action = action,
                Url = url,
                PostData = httpPostData,
                FileData = fileUploadData,
                FileName = fileName,
                FileFieldName = fileFieldName
            };
            AddQueueItem( item );
        }

        /// <summary>
        /// Add a new queue item which will be processed later
        /// </summary>
        /// <param name="item"></param>
        private void AddQueueItem( QueueItem item ) {
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
        }

        /// <summary>
        /// Cancel all tasks
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
        /// Create a thread from item queue
        /// </summary>
        private void StartThread() {
            while ( true ) {
                if ( _taskCount == 0 ) {
                    _resetEvent.WaitOne();
                }
                QueueItem item = null;
                // Get one single queued task item
                lock ( _waitingQueue ) {
                    if ( !_isThreadActivated || _waitingQueue.Count == 0 ) {
                        break;
                    }
                    item = _waitingQueue.Dequeue();
                    Interlocked.Decrement( ref _taskCount );
                }

                // Get the task
                var task = GetTask( item );
                task.Start();

                // then it'll be in the working pool
                lock ( _tasks ) {
                    _tasks.Add( task );
                }

                Thread.Sleep( 1 );
            }
        }

        /// <summary>
        /// Get a task from a queued item
        /// </summary>
        /// <param name="item">queue request item</param>
        /// <returns>task</returns>
        private HttpTask GetTask( QueueItem item ) {
            HttpTask httpTask = null;
            if ( item.Method == HttpMethod.Get ) {
                httpTask = new GetTextHttpTask( item.Url );
            }
            else if ( item.FileData == null ) {
                httpTask = new PostTextHttpTask( item.Url, item.PostData );
            }
            else {
                //httpTask = new FileUploadHttpTask( item.Url, item.PostData, item.FileData, item.FileFieldName, item.FileName );
            }
            httpTask.OnHttpRequestCompleted += ( state, stream, error ) => {
                // first clean up the task pool internally
                lock ( _tasks ) {
                    _tasks.Remove( httpTask );
                }
                if ( _taskCount == 0 ) {
                    _resetEvent.Set();
                }
                Interlocked.Increment( ref _taskCount );
                
                // here all things done! May be error or canceled
                item.Action( state, stream, error );
            };
            return httpTask;
        }
    }
}
