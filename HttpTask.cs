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
using System.IO;

namespace Arakuma.NetworkUtil {
    /// <summary>
    /// Callback for web request done
    /// </summary>
    /// <param name="stream">retrieved stream, may be null</param>
    internal delegate void HttpRequestCallback( bool succeeded, string result, string errorResult );

    /// <summary>
    /// Base task for http request
    /// </summary>
    internal abstract class HttpTask : ICancelableTask {
        public event HttpRequestCallback OnHttpRequestCompleted;    // call back for work done

        public abstract void Start();
        public abstract void Cancel();

        /// <summary>
        /// Error occured
        /// </summary>
        /// <param name="errorString">error message</param>
        protected void OnError( string errorString ) {
            NotifyCompleted( false, null, errorString );
        }

        /// <summary>
        /// Stream processing for sub-classes overriding
        /// No matter what kind of result of the request has been received
        ///   it will be notified from here
        /// </summary>
        /// <param name="stream"></param>
        protected virtual void NotifyCompleted( bool succeeded, string result, string errorResult ) {
            if ( OnHttpRequestCompleted != null ) {
                OnHttpRequestCompleted( succeeded, result, errorResult );
            }
        }
    }
}
