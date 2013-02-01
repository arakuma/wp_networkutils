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

namespace Arakuma.NetworkUtil {
    /// <summary>
    /// Task for get request
    /// </summary>
    internal class GetTextHttpTask : TextHttpTask {

        /// <summary>
        /// Consturctor
        /// </summary>
        /// <param name="url">requested url</param>
        public GetTextHttpTask( string url )
            : base( url ) {
            _webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler( _webClient_DownloadStringCompleted );
        }

        /// <summary>
        /// Callback from webclient
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _webClient_DownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e ) {
            if ( e.Error == null && !e.Cancelled ) {
                NotifyCompleted( true, e.Result, string.Empty );
            }
            else {
                NotifyCompleted( false, string.Empty, e.Error.Message );
            }
        }

        /// <summary>
        /// Start to download strings from url
        /// </summary>
        protected override void DoStart() {
            _webClient.DownloadStringAsync( new Uri( _url, UriKind.Absolute ) );
        }
    }
}
