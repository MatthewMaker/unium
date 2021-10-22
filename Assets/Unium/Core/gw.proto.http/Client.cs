// Copyright (c) 2017 Gwaredd Mountain, https://opensource.org/licenses/MIT
#if !UNIUM_DISABLE && ( DEVELOPMENT_BUILD || UNITY_EDITOR || UNIUM_ENABLE )

using System.IO;
using System.Net.Sockets;

namespace gw.proto.http
{
    ////////////////////////////////////////////////////////////////////////////////

    public class Client
	{
        public Stream       Stream          { get; private set; }
        public Dispatcher   Dispatch        { get; private set; }
        public uint         ID              { get; private set; }
        public string       Address         { get { return mClient != null ? mClient.Client.RemoteEndPoint.ToString() : "unknown"; } }
        public int          SendBufferSize  { get { return mClient != null ? mClient.SendBufferSize : 128 * 1024; } }

        private TcpClient mClient;

        public Client( Dispatcher dispatcher, TcpClient client )
        {
            mClient     = client;
            Dispatch    = dispatcher;
            Stream      = client.GetStream();
        }


        public Client( Dispatcher dispatcher, Stream stream )
        {
            Dispatch    = dispatcher;
            Stream      = stream;
		}


        // client started on new thread from ThreadPool

        public void OnConnect()
        {
            //Dispatch.Log.Print( "connection from {0}", Address );
            (new HttpRequest(this)).Process( Stream, Dispatch );
        }
	}
}

#endif
