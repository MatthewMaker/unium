// Copyright (c) 2017 Gwaredd Mountain, https://opensource.org/licenses/MIT

//#define GW_LOGGING

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Debug = UnityEngine.Debug;
#if UNITY_2017_3_OR_NEWER
using UnityEngine.Networking;
#endif

namespace gw.proto.utils
{
    public static class Util
    {
        public static Random Rnd = new Random();
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static string RandomString( int length = 8 )
        {
            return new string( Enumerable.Repeat( chars, length ).Select( s => s[ Rnd.Next( s.Length ) ] ).ToArray() );
        }

        [Conditional("GW_LOGGING")]
        public static void Print( string msg, params object[] args )
        {
            Debug.Log($"[{DateTime.Now:HH:mm:ss.ffff}] {string.Format(msg, args)}");
        }

        [Conditional( "GW_LOGGING" )]
        public static void Warn( string msg, params object[] args )
        {
            Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.ffff}] {string.Format(msg, args)}");
        }

        [Conditional( "GW_LOGGING" )]
        public static void Error( string msg, params object[] args )
        {
            Debug.LogError($"[{DateTime.Now:HH:mm:ss.ffff}] {string.Format(msg, args)}");
        }

        public static string DetectPublicIPAddress()
        {
            if( NetworkInterface.GetIsNetworkAvailable() == false )
            {
                return IPAddress.Any.ToString(); // 0.0.0.0
            }

            var hostname = Dns.GetHostName();
            var foo = Dns.GetHostEntry( hostname );
            var addrs = foo.AddressList;
            var theaddr = addrs.LastOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback( addr ));
            if (theaddr != null) return theaddr.ToString();
            return "";
        }

        public static NameValueCollection ParseQueryString( string query )
        {
            var bag = new NameValueCollection();

            if( string.IsNullOrEmpty( query ) )
            {
                return bag;
            }

            var parameterDelimiters = new[] { '?', '&' };
            var keyValueDelimiters  = new[] { '=' };

            var parameters = query.Split( parameterDelimiters, StringSplitOptions.RemoveEmptyEntries );

            foreach( var param in parameters )
            {
                var keyValue = param.Split( keyValueDelimiters, StringSplitOptions.None );
                var value    = keyValue.Length >= 2 ? keyValue[ 1 ] : "";

#if UNITY_2017_3_OR_NEWER
                var ekey   = UnityWebRequest.UnEscapeURL( keyValue[ 0 ] );
                var evalue = UnityWebRequest.UnEscapeURL( value );
#else
                var ekey   = UnityEngine.WWW.UnEscapeURL( keyValue[ 0 ] );
                var evalue = UnityEngine.WWW.UnEscapeURL( value );
#endif

                bag.Add( ekey, evalue );
            }

            return bag;
        }
    }
}
