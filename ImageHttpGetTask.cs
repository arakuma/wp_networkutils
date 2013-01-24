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
using Arakuma.Threading;

namespace Arakuma.Ui.ImageTool {
    /// <summary>
    /// 获取网络图片的回调
    /// </summary>
    /// <param name="bitmapSource">从Stream中转换出来的Bitmap</param>
    public delegate void ImageCreateCallback( BitmapSource bitmapSource );

    /// <summary>
    /// 网络图片获取任务
    /// </summary>
    public class ImageHttpGetTask : HttpGetTask {
        // 基类中虽然有任务完成的event，但那个是提供给只需要基类功能即可的使用者使用的
        // 对于图片来说，提供的参数类型也不同
        public event ImageCreateCallback OnImageCreated;

        public ImageHttpGetTask( string url )
            : base( url ) {
        }

        /// <summary>
        /// 重写方法，将Stream转换为Bitmap
        /// </summary>
        /// <param name="stream"></param>
        protected override void NotifyCompleted( System.IO.Stream stream ) {
            base.NotifyCompleted( stream );
            BitmapSource bitmapSource = null;
            Deployment.Current.Dispatcher.BeginInvoke( () => {
                bitmapSource = new BitmapImage();
                if ( stream != null ) {
                    bitmapSource.SetSource(stream);
                }
                if ( OnImageCreated != null ) {
                    OnImageCreated( bitmapSource );
                }
            } );

            //stream.Close();
        }
    }
}
