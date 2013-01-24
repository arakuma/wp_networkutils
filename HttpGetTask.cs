using System;
using System.Net;
using System.IO;

namespace Arakuma.Threading {
    /// <summary>
    /// 网络任务完成回调
    /// </summary>
    /// <param name="stream">获取到的Stream</param>
    public delegate void HttpGetCallback( Stream stream );

    /// <summary>
    /// 网络请求任务
    /// </summary>
    public class HttpGetTask : ICancelableTask {
        public event Action TaskCompleted;                  // 任务完成回调
        public event HttpGetCallback OnHttpGetCompleted;    // 任务无错完成回调

        private WebClient _webClient;
        private string    _url;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="url"></param>
        public HttpGetTask( string url ) {
            _url = url;
            _webClient = new WebClient();
            _webClient.OpenReadCompleted += new OpenReadCompletedEventHandler( _webClient_OpenReadCompleted );
        }

        /// <summary>
        /// 任务启动，开始Http请求
        /// </summary>
        public void Start() {
            try {
                _webClient.OpenReadAsync( new Uri( _url, UriKind.Absolute ) );
            }
            catch ( Exception ex ) {
                ex.ToString();
                if ( _webClient.IsBusy ) {
                    Cancel();
                }
            }
        }

        /// <summary>
        /// 任务取消
        /// </summary>
        public void Cancel() {
            try {
                _webClient.CancelAsync();
            }
            catch ( Exception ex ) {
                ex.ToString();
            }
        }

        /// <summary>
        /// 供子类覆盖进行获取到Stream的处理
        /// 只有Http访问正常（无error，无cancel）才会走到
        /// </summary>
        /// <param name="stream"></param>
        protected virtual void NotifyCompleted( Stream stream ) {
            if ( OnHttpGetCompleted != null ) {
                OnHttpGetCompleted( stream );
            }
        }

        /// <summary>
        /// webClient获取stream完毕的回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _webClient_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e ) {
            if ( e.Error == null && !e.Cancelled ) {
                byte[] streamBuffer = new byte[e.Result.Length];
                e.Result.Read( streamBuffer, 0, ( int )e.Result.Length );
                MemoryStream memStream = new MemoryStream( streamBuffer );
                NotifyCompleted( memStream );
                //NotifyCompleted( e.Result );
            }

            // 取消或者出错的情况下也会通知将此task从线程队列中拿掉
            if ( TaskCompleted != null ) {
                TaskCompleted();
            }
        }
    }
}
 