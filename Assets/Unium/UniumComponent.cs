// Copyright (c) 2017 Gwaredd Mountain, https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using gw.proto.http;
using gw.proto.utils;
using gw.unium;
using UnityEngine;
using Debug = UnityEngine.Debug;


////////////////////////////////////////////////////////////////////////////////////////////////////
// UniumComponent

public class UniumComponent : MonoBehaviour
{
    public static UniumComponent Singleton;
    public static bool IsDebug => Singleton != null && Singleton.EnableDebug;

    public int      Port            = 8342;
    public bool     RunInBackground = true;
    public bool     EnableDebug;
    public bool     AutoStart       = true;
    public string   StaticFiles;

    public enum AddressStrategy
    {
        AllInterfaces,
        PublicIp,
        PublicIpInPlayer,
    }
    public AddressStrategy ServeOn = AddressStrategy.AllInterfaces;

#if !UNIUM_DISABLE && ( DEVELOPMENT_BUILD || UNITY_EDITOR || UNIUM_ENABLE )

    //----------------------------------------------------------------------------------------------------

    class GameThreadRequest
    {
        public RequestAdapterHTTP   Request;
        public Route                Route;
    }

    readonly List< GameThreadRequest >   mQueuedRequests = new List<GameThreadRequest>();
    readonly List< UniumSocket >         mSockets        = new List<UniumSocket>();
    Server                      mServer;


    //----------------------------------------------------------------------------------------------------

    void Awake()
    {
        // there can be only one!

        if( Singleton != null && Singleton != this )
        {
            Destroy( gameObject );
            return;
        }

        // and always one

        DontDestroyOnLoad( transform.gameObject );
        Singleton = this;

        // keep game running when editor does not have focus - needed as we need to dispatch on the game thread (which would be paused otherwise)

        if( RunInBackground )
        {
            Application.runInBackground = true;
        }

        var root = Path.Combine( Application.streamingAssetsPath, StaticFiles ?? "" );

        HandlerFile.Mount( "persistent", Application.persistentDataPath );
        HandlerFile.Mount( "streaming",  Application.streamingAssetsPath );
        HandlerFile.Mount( "root",       root );


        Application.logMessageReceivedThreaded += HandlerUtils.LogMessage;
        Events.Singleton.BindEvents();
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////

    void OnEnable()
    {
        if( AutoStart )
        {
            StartServer();
        }
    }


    //----------------------------------------------------------------------------------------------------

    void OnDisable()
    {
        if( Singleton == this )
        {
            Application.logMessageReceivedThreaded -= HandlerUtils.LogMessage;
            Events.Singleton.UnbindEvents();

            StopServer();
            Singleton = null;
        }
    }


    //----------------------------------------------------------------------------------------------------

    public void StartServer()
    {
        // start server

        mServer = new Server();

        if( ServeOn == AddressStrategy.PublicIp || (ServeOn == AddressStrategy.PublicIpInPlayer && !Application.isEditor) )
        {
            mServer.Settings.Address = IPAddress.Parse( Util.DetectPublicIPAddress() );
        }

        mServer.Settings.Port = Port;

        mServer.Dispatcher.OnWebRequest     = OnWebRequest;
        mServer.Dispatcher.OnSocketRequest  = OnSocketOpen;
        mServer.Dispatcher.OnSocketClose += OnWebSocketClose;

        mServer.Start();

        Log($"server listening on {mServer.Settings.Address}:{Port}");
    }


    //----------------------------------------------------------------------------------------------------

    public void StopServer()
    {
        if( mServer == null )
        {
            return;
        }

        Log( "server stopped" );

        mServer.Stop();
        mServer.Dispatcher.OnSocketClose -= OnWebSocketClose;
        mServer = null;

        lock( mQueuedRequests )
        {
            foreach( var req in mQueuedRequests )
            {
                req.Request.Reject( ResponseCode.InternalServerError );
            }

            mQueuedRequests.Clear();
        }

        lock( mSockets )
        {
            mSockets.Clear();
        }
    }


    //----------------------------------------------------------------------------------------------------

    void LateUpdate()
    {
        ProcessWebRequestsOnGameThread();
        ProcessSockets();
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // Note that OnWebRequest is executed on a worker thread

    void OnWebRequest( HttpRequest req )
    {
        Log( req.URL );

        if( string.IsNullOrEmpty( req.URL ) )
        {
            req.Reject( ResponseCode.NotFound );
            return;
        }


        // set default headers

        req.Response.Headers[ "Access-Control-Allow-Origin" ] = "*";
        req.Response.Headers[ "Cache-Control" ] = "no-store, must-revalidate";
        req.Response.Headers[ "Content-Type" ]  = "application/json";


        // handle request

        var route = Unium.RoutesHTTP.Find( req.URL );

        if( route?.Handler == null )
        {
            req.Reject( ResponseCode.NotFound );
            return;
        }

        if( route.DispatchOnGameThread )
        {
            lock( mQueuedRequests )
            {
                mQueuedRequests.Add( new GameThreadRequest { Request = new RequestAdapterHTTP( req ), Route = route } );
            }
        }
        else
        {
            route.Dispatch( new RequestAdapterHTTP( req ) );
        }
    }


    //----------------------------------------------------------------------------------------------------

    void ProcessWebRequestsOnGameThread()
    {
        // process all pending requests on the game thread

        lock( mQueuedRequests )
        {
            if( mQueuedRequests.Count == 0 )
            {
                return;
            }

            // dispatch one per frame

            var req = mQueuedRequests[ 0 ];

            req.Route.Dispatch( req.Request );
            mQueuedRequests.RemoveAt( 0 );
        }
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////

    void OnSocketOpen( WebSocket ws )
    {
        if( ws.URL != "/ws" )
        {
            Log($"[sock:{ws.ID}] rejected - {ws.URL}");
            ws.Reject();
            return;
        }

        lock( mSockets )
        {
            Log($"[sock:{ws.ID}] connected - {ws.URL}");
            mSockets.Add( new UniumSocket( ws ) );
            ws.Accept();
        }
    }


    //----------------------------------------------------------------------------------------------------

    void OnWebSocketClose( WebSocket ws )
    {
        lock( mSockets )
        {
            Log($"[sock:{ws.ID}] closed");

            var socket = ws.User as UniumSocket;
            socket?.OnClose();
            mSockets.Remove( socket );
        }
    }


    //----------------------------------------------------------------------------------------------------

    void ProcessSockets()
    {
        lock( mSockets )
        {
            foreach( var ws in mSockets )
            {
                ws.Tick();
            }
        }
    }

#endif

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    static int sLogNumber;

    [Conditional( "DEBUG" )]
    public static void Log( string msg, LogType type = LogType.Log )
    {
        if( Singleton == null || Singleton.EnableDebug == false )
        {
            return;
        }

        var str = $"[unium][{sLogNumber++}] {msg}";

        switch( type )
        {
            case LogType.Log:       Debug.Log( str ); break;
            case LogType.Warning:   Debug.LogWarning( str ); break;
            default:
            // ReSharper disable once RedundantCaseLabel
            case LogType.Error:     Debug.LogError( str ); break;
        }
    }

    [Conditional( "DEBUG" )]
    public static void Warn( string msg )
    {
        Log( msg, LogType.Warning );
    }

    [Conditional( "DEBUG" )]
    public static void Error( string msg )
    {
        Log( msg, LogType.Error );
    }
}
