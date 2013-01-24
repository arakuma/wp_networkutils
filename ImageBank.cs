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
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;

namespace Arakuma.Ui.ImageTool {
    /// <summary>
    /// 带有缓存的图片管理器
    /// 单例
    /// </summary>
    public class ImageBank : ImageLoader {
        private static ImageBank _instance;
        private BitmapCache      _cache;
        private readonly long    CACHE_CLEAN_INTERVAL = 36000000 * 24 * 2;  // 清理缓存时间：每两天。单位毫秒
        private readonly string  CACHE_CLEAN_LAST     = "imagecacheclean";

        /// <summary>
        /// 构造器
        /// </summary>
        private ImageBank()
            : base() {
            _cache = new FsBitmapCache();
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if ( settings.Contains( CACHE_CLEAN_LAST ) ) {
                double lastCleanTime = Double.Parse( settings[CACHE_CLEAN_LAST].ToString() );
                if ( DateTime.Now.Ticks / 10000 - lastCleanTime > CACHE_CLEAN_INTERVAL ) {
                    _cache.Clear();
                }
            }
            else {
                // 没有设置过清理缓存的时间，第一次启动或者不小心删掉了，重新设置
                settings[CACHE_CLEAN_LAST] = ( DateTime.Now.Ticks / 10000 ).ToString();
            }
        }

        /// <summary>
        /// 单例入口
        /// </summary>
        public static ImageBank Instance {
            get {
                if ( _instance == null ) {
                    lock ( typeof( ImageBank ) ) {
                        if ( _instance == null ) {
                            _instance = new ImageBank();
                        }
                    }
                }
                return _instance;
            }
        }

        public void Clear() {
            _cache.Clear();
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Finish() {
            CancelAll();
            _cache.Finish();
        }

        /// <summary>
        /// 设置图片，首先查找缓存，没有再下载
        /// </summary>
        /// <param name="url">图片网址</param>
        /// <param name="imageControl">需要设置图片的控件</param>
        /// <returns>有缓存情况下为需要图片，否则先返回默认图片</returns>
        public override void SetImage( string url, Image imageControl ) {
            BitmapSource retBitmap = null;
            if ( _cache.Contains( url ) ) {
                retBitmap = _cache.Load( url );
                if ( retBitmap != null && imageControl != null ) {
                    imageControl.Source = retBitmap;
                    return;
                }
            }
            base.SetImage( url, imageControl );
        }

        public override BitmapSource Get( string url, ImageCreateCallback action ) {
            BitmapSource retBitmap = null;
            if ( _cache.Contains( url ) ) {
                retBitmap = _cache.Load( url );
                if ( retBitmap != null && action != null ) {
                    action( retBitmap );
                    return retBitmap;
                }
            }
            return base.Get( url, action );
        }

        protected override void OnNewImageDownloaded( string url, BitmapSource bitmap ) {
            base.OnNewImageDownloaded( url, bitmap );
            if ( bitmap != null ) {
                _cache.Store( url, bitmap );
            }
        }
    }
}
