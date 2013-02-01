using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Reactive;

namespace Arakuma.NetworkUtil {
    /// <summary>
    /// External call back for request
    /// </summary>
    /// <param name="isOk"></param>
    /// <param name="returnedText"></param>
    /// <param name="errorResult"></param>
    public delegate void RequestDoneCallback( bool isOk, string returnedText, string errorResult );

    /// <summary>
    /// Http method
    /// </summary>
    public enum HttpMethod {
        Get,
        Post
    }

    /// <summary>
    /// Networking type
    /// </summary>
    public enum NetworkType {
        Unknown,
        None,
        Cellular,
        Cellular_2G,
        Cellular_3G,
        WiFi,
        Ethernet
    }

    public delegate void NetworkTypeResolvedCallback( NetworkType type );
    public delegate void NetworkAvailabilityChangedCallback( NetworkNotificationEventArgs notification );

    public static class Networking {
        /// <summary>
        /// Provides functionality for retrieving the carrier name
        /// </summary>
        /// <returns>carrier name</returns>
        public static string CellularMobileOperator {
            get { return DeviceNetworkInformation.CellularMobileOperator; }
        }

        /// <summary>
        /// Provides functionality for determining if network is available
        /// </summary>
        /// <returns>Boolean (True/False)</returns>
        public static bool IsNetworkAvailable {
            get { return DeviceNetworkInformation.IsNetworkAvailable; }
        }

        /// <summary>
        /// Provides functionality for determining if wifi is enabled
        /// </summary>
        /// <returns>Boolean (True/False)</returns>
        public static bool IsWiFiEnabled {
            get { return DeviceNetworkInformation.IsWiFiEnabled; }
        }

        /// <summary>
        /// Provides functionality for determining if cellular data is enabled
        /// </summary>
        /// <returns>Boolean (True/False)</returns>
        public static bool IsCellularDataEnabled {
            get { return DeviceNetworkInformation.IsCellularDataEnabled; }
        }

        /// <summary>
        /// Provides functionality for determining if cellular roaming is enabled
        /// </summary>
        /// <returns>Boolean (True/False)</returns>
        public static bool IsCellularDataRoamingEnabled {
            get { return DeviceNetworkInformation.IsCellularDataRoamingEnabled; }
        }

        /// <summary>
        /// Get network availability changed event stream
        /// </summary>
        /// <returns></returns>
        private static IObservable<NetworkNotificationEventArgs> GetNetworkAvailabilityChangedEventStream() {
            return Observable.Create<NetworkNotificationEventArgs>( observer => {
                EventHandler<NetworkNotificationEventArgs> handler = ( s, e ) => {
                    observer.OnNext( e );
                };
                DeviceNetworkInformation.NetworkAvailabilityChanged += handler;
                return () => { DeviceNetworkInformation.NetworkAvailabilityChanged -= handler; };
            }
            );
        }

        /// <summary>
        /// Notify network availability changes
        /// </summary>
        /// <param name="callback"></param>
        public static void NotifyNetworkAvailabilityChanges( NetworkAvailabilityChangedCallback callback ) {
            var networkChanges = from networkChanged in GetNetworkAvailabilityChangedEventStream()
                                 select networkChanged;

            networkChanges.Subscribe( networkChanged => { callback( networkChanged ); } );
        }

        /// <summary>
        /// Get current network type synchronously
        /// Cellular 2G and 3G couldn't be identified
        /// </summary>
        public static NetworkType GetCurrentNetworkTypeSync() {
            var info = NetworkInterface.NetworkInterfaceType;

            switch ( info ) {
                case NetworkInterfaceType.MobileBroadbandCdma:
                case NetworkInterfaceType.MobileBroadbandGsm:
                return NetworkType.Cellular;
                case NetworkInterfaceType.Wireless80211:
                return NetworkType.WiFi;
                case NetworkInterfaceType.Ethernet:
                return NetworkType.Ethernet;
                case NetworkInterfaceType.None:
                return NetworkType.None;
                default:
                return NetworkType.Unknown;
            }
        }

        /// <summary>
        /// Get current network type by host name asynchronously
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="callback"></param>
        public static void GetCurrentNetworkTypeAsync( string hostName, NetworkTypeResolvedCallback callback ) {
            NetworkType currentType = NetworkType.None;
            DeviceNetworkInformation.ResolveHostNameAsync(
                new DnsEndPoint( hostName, 80 ),
                new NameResolutionCallback( handle => {
                    NetworkInterfaceInfo info = handle.NetworkInterface;
                    if ( info != null ) {
                        switch ( info.InterfaceType ) {
                            case NetworkInterfaceType.None:
                            currentType = NetworkType.None;
                            break;
                            case NetworkInterfaceType.Ethernet:
                            currentType = NetworkType.Ethernet;
                            break;
                            case NetworkInterfaceType.MobileBroadbandCdma:
                            case NetworkInterfaceType.MobileBroadbandGsm:
                            switch ( info.InterfaceSubtype ) {
                                case NetworkInterfaceSubType.Cellular_3G:
                                case NetworkInterfaceSubType.Cellular_EVDO:
                                case NetworkInterfaceSubType.Cellular_EVDV:
                                case NetworkInterfaceSubType.Cellular_HSPA:
                                currentType = NetworkType.Cellular_3G;
                                break;
                                case NetworkInterfaceSubType.Cellular_GPRS:
                                case NetworkInterfaceSubType.Cellular_EDGE:
                                case NetworkInterfaceSubType.Cellular_1XRTT:
                                currentType = NetworkType.Cellular_2G;
                                break;
                                default:
                                currentType = NetworkType.None;
                                break;
                            }
                            break;
                            case NetworkInterfaceType.Wireless80211:
                            currentType = NetworkType.WiFi;
                            break;
                            default:
                            currentType = NetworkType.Unknown;
                            break;
                        }
                    }
                    else {
                        currentType = NetworkType.Unknown;
                    }
                    callback( currentType );
                } ), null );
        }

        /// <summary>
        /// Request for content (GET)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        public static void DownloadString( string url, RequestDoneCallback callback ) {
            TaskPool.Instance.Request(
                url,
                HttpMethod.Get,
                null,
                new HttpRequestCallback( ( isOk, result, errResult ) => {
                    callback( isOk, result, errResult );
                } )
                );
        }

        /// <summary>
        /// Request for content (POST)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="callback"></param>
        public static void UploadString( string url, List<KeyValuePair<string, string>> postData, RequestDoneCallback callback ) {
            TaskPool.Instance.Request(
                url,
                HttpMethod.Post,
                postData,
                new HttpRequestCallback( ( isOk, result, errResult ) => {
                    callback( isOk, result, errResult );
                } )
                );
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="fileData"></param>
        /// <param name="fileName"></param>
        /// <param name="callback"></param>
        public static void UploadFile( string url, List<KeyValuePair<string, string>> postData, byte[] fileData, string fileFieldName, string fileName, RequestDoneCallback callback ) {
            TaskPool.Instance.RequestUploadFile(
                url,
                postData,
                fileData,
                fileFieldName,
                fileName,
                new HttpRequestCallback( ( isOk, result, errResult ) => {
                    callback( isOk, result, errResult );
                } ) );
        }

        /// <summary>
        /// Cancel all networking requests
        /// </summary>
        public static void CancelAll() {
            TaskPool.Instance.CancelAll();
        }

        /// <summary>
        /// Get string from stream with UTF-8 encoding
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Converted string, string.Empty if failed</returns>
        private static string GetTextFromStream( Stream stream ) {
            return GetTextFromStream( stream, Encoding.UTF8 );
        }

        /// <summary>
        /// Get string from stream, default encoding is UTF-8
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Converted string, string.Empty if failed</returns>
        private static string GetTextFromStream( Stream stream, Encoding encoding ) {
            string ret = string.Empty;
            StreamReader reader = new StreamReader( stream, encoding );
            try {
                ret = reader.ReadToEnd();
            }
            catch ( Exception ) { }
            finally {
                reader.Close();
            }
            return ret;
        }
    }
}
