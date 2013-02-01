using System;
using System.Net;
using System.IO;
using System.ComponentModel;

namespace Arakuma.NetworkUtil {
    /// <summary>
    /// GET/POST raw text request task
    /// </summary>
    internal abstract class TextHttpTask : HttpTask, ICancelableTask {

        protected WebClient  _webClient;
        protected string     _url;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="url"></param>
        public TextHttpTask( string url ) {
            _url = url;
            _webClient = new WebClient();
            _webClient.Encoding = System.Text.UTF8Encoding.UTF8;
        }

        /// <summary>
        /// Start to request
        /// </summary>
        public override void Start() {
            try {
                DoStart();
            }
            catch ( Exception ex ) {
                Cancel( ex.ToString() );
            }
        }

        protected abstract void DoStart();

        /// <summary>
        /// Cancel the task
        /// </summary>
        public override void Cancel() {
            Cancel( "cancelled" );
        }

        /// <summary>
        /// Cancel the task
        /// </summary>
        /// <param name="reason">reason for cancel the task</param>
        public void Cancel( string reason ) {
            try {
                OnError( reason );
                if ( _webClient.IsBusy ) {
                    _webClient.CancelAsync();
                }
            }
            catch ( Exception ex ) {
                ex.ToString();
            }
        }
    }
}
