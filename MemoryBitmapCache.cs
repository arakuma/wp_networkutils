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

namespace Arakuma.Ui.ImageTool {
    /// <summary>
    /// 内存图片缓存机制
    /// </summary>
    public class MemoryBitmapCache : BitmapCache {

        #region 覆盖的方法
        public override BitmapSource Load( string url ) {
            lock ( _memCache ) {
                BitmapSource bitmap = _memCache[GetFileName(url)];
                if ( bitmap == null ) {
                    return DEFAULT_IMAGE;
                }
                else {
                    return bitmap;
                }
            }
        }

        public override void Store( string url, BitmapSource bitmap ) {
            if ( String.IsNullOrWhiteSpace( url ) || bitmap == null ) {
                return;
            }

            _memCache[GetFileName(url)] = bitmap;
        }
        #endregion
    }
}
