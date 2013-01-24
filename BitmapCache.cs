using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System;
using System.Windows;

namespace Arakuma.Ui.ImageTool {
    /// <summary>
    /// 图片缓存基类
    /// 如果希望使用固定大小缓存可以用Cache.LRUCache进行扩展
    /// </summary>
    public abstract class BitmapCache {
        public static BitmapImage                  DEFAULT_IMAGE;
        protected Dictionary<string, BitmapSource> _memCache;
        protected HMACSHA1                         _hash;
        protected readonly string                  CHAR_DOT            = ".";
        protected readonly string                  CHAR_SLASH          = "/";
        protected readonly string                  DEFAULT_IMAGE_NAME  = "/defaultimage.jpg";

        /// <summary>
        /// 构造器
        /// </summary>
        public BitmapCache() {
            _memCache = new Dictionary<string, BitmapSource>();
            ReadDefaultImage();
        }

        /// <summary>
        /// 启用Hash功能
        /// 对于服务器给定的图片名称会重复的情况下启用
        /// </summary>
        protected void EnableHashedKey() {
            _hash = new HMACSHA1();
        }

        /// <summary>
        /// 获取文件名
        /// </summary>
        /// <param name="url">图片的地址</param>
        /// <returns>cache中存储的图片名</returns>
        protected string GetFileName( string url ) {
            string fileName = string.Empty;
            if ( _hash != null ) {
                byte[] urlBytes = Encoding.UTF8.GetBytes( url );
                byte[] hashedBytes = _hash.ComputeHash( urlBytes );
                // 最后加上扩展名
                fileName = ConvertUtf8BytesToString( hashedBytes ) + url.Substring( url.LastIndexOf( CHAR_DOT ) );
            }
            else {
                // 不需要Hash的情况只要截取实际名称即可
                fileName = url.Substring( url.LastIndexOf( CHAR_SLASH ) + 1 );
            }
            return fileName;
        }
        
        /// <summary>
        /// 是否已经缓存了某张图片
        /// 需要情况下子类可以覆盖
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <returns>是否有缓存</returns>
        public virtual bool Contains( string url ) {
            return _memCache.ContainsKey( GetFileName( url ) );
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public virtual void Clear() {
            _memCache.Clear();
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public virtual void Finish() {
            _memCache.Clear();
        }

        #region 需要子类实现的函数
        /// <summary>
        /// 获取图片
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <returns>图片资源</returns>
        public abstract BitmapSource Load( string url );

        /// <summary>
        /// 将下载好的图片保存到缓存
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <param name="bitmap">图片</param>
        public abstract void Store( string url, BitmapSource bitmap );
        #endregion

        /// <summary>
        /// 将Hash过后的bytes转化为string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string ConvertUtf8BytesToString( byte[] bytes ) {
            StringBuilder retString = new StringBuilder();
            foreach ( byte @byte in bytes ) {
                retString.Append( @byte.ToString( "X2" ) );  // 转为Hex形式
            }
            return retString.ToString();
        }

        /// <summary>
        /// 读取默认图片
        /// </summary>
        private void ReadDefaultImage() {
            DEFAULT_IMAGE = new BitmapImage {
                UriSource = new Uri( DEFAULT_IMAGE_NAME, UriKind.Relative )
            };
        }
    }
}
