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
using System.Windows.Resources;

namespace Arakuma.Ui.ImageTool {
    /// <summary>
    /// 文件系统图片缓存机制
    /// </summary>
    public class FsBitmapCache : BitmapCache {
        private IsolatedStorageFile             _storage;
        private readonly int                    IMAGE_QUALITY = 85;
        private readonly string                 CACHE_FOLDER = "imagecache";

        /// <summary>
        /// 构造器
        /// </summary>
        public FsBitmapCache() {
            try {
                _storage = IsolatedStorageFile.GetUserStoreForApplication();
            }
            catch ( IsolatedStorageException ex ) {
                ex.ToString();
            }

            //EnableHashedKey();
            EnsureCacheFolder();
        }

        #region 覆盖的方法
        /// <summary>
        /// 覆盖基类实现，因为还有硬盘缓存
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public override bool Contains( string url ) {
            bool cachedInMem = base.Contains( url );
            if ( cachedInMem ) {
                return true;
            }
            else {
                using ( _storage = IsolatedStorageFile.GetUserStoreForApplication() ) {
                    String filePath = GetFilePath( GetFileName( url ) );
                    return _storage.FileExists( filePath );
                }
            }
        }

        public override BitmapSource Load( string url ) {
            string fileName = GetFileName( url );
            BitmapSource retBitmap = null;
            if ( _memCache.ContainsKey( fileName ) ) {
                retBitmap = _memCache[fileName];
            }
            if ( retBitmap != null ) {
                return retBitmap;
            }

            // 内存中无缓存
            fileName = GetFilePath( GetFileName( url ) );
            using ( _storage = IsolatedStorageFile.GetUserStoreForApplication() ) {
                if ( !_storage.FileExists( fileName ) ) {
                    return BitmapCache.DEFAULT_IMAGE;
                }
                BitmapImage bitmap = null;
                try {
                    IsolatedStorageFileStream fileStream = _storage.OpenFile( fileName, System.IO.FileMode.Open );
                    Deployment.Current.Dispatcher.BeginInvoke( () => {
                        bitmap = new BitmapImage();
                        bitmap.SetSource( fileStream );
                        fileStream.Dispose();
                    } );
                }
                catch ( Exception e ) {
                    e.ToString();
                }
                return bitmap;
            }
        }

        public override void Store( string url, BitmapSource bitmap ) {
            Deployment.Current.Dispatcher.BeginInvoke( () => {
                string fileName = GetFilePath( GetFileName( url ) );
                using ( _storage = IsolatedStorageFile.GetUserStoreForApplication() ) {
                    if ( _storage.FileExists( fileName ) ) {
                        _storage.DeleteFile( fileName );
                    }

                    using ( IsolatedStorageFileStream fileStream = _storage.CreateFile( fileName ) ) {
                        WriteableBitmap writeableBitmap = new WriteableBitmap( bitmap );
                        writeableBitmap.SaveJpeg( fileStream, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, 0, IMAGE_QUALITY );
                    }
                }
            } );
        }

        public override void Clear() {
            base.Clear();
            using ( _storage = IsolatedStorageFile.GetUserStoreForApplication() ) {
                try {
                    foreach ( string file in _storage.GetFileNames( CACHE_FOLDER + @"\*" ) ) {
                        _storage.DeleteFile( GetFilePath( file ) );
                    }
                }
                catch ( Exception e ) {
                    e.ToString();
                }
            }
        }

        public override void Finish() {
            base.Finish();
            if ( _storage != null ) {
                _storage.Dispose();
            }
        }
        #endregion

        /// <summary>
        /// 获取文件相对路径
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>带有Cache文件夹名称的路径</returns>
        private string GetFilePath( string fileName ) {
            return CACHE_FOLDER + @"\" + fileName;
        }

        /// <summary>
        /// 确保Cache文件夹存在
        /// </summary>
        private void EnsureCacheFolder() {
            using ( _storage = IsolatedStorageFile.GetUserStoreForApplication() ) {
                if ( !_storage.DirectoryExists( CACHE_FOLDER ) ) {
                    _storage.CreateDirectory( CACHE_FOLDER );
                }
            }
        }
    }
}
