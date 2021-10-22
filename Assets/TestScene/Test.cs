﻿using UnityEngine;
using UnityEngine.UI;
using System;

using gw.unium;
using gw.gql;
using gw.proto.utils;

////////////////////////////////////////////////////////////////////////////////////////////////////

public class Test : MonoBehaviour
{
    public Text IPText;


    //----------------------------------------------------------------------------------------------------

    void Start()
    {
        // check we can add routes dynamically
        // TODO: gw - temporarily remove test route - RoutesHTTP is protected
        Unium.RoutesHTTP.AddImmediate( "/test", ( RequestAdapter req, string path ) => req.Respond( @"{""test"":""ok""}" ) );

        if( IPText != null )
        {
            IPText.text = Util.DetectPublicIPAddress();
        }
    }

    //----------------------------------------------------------------------------------------------------
    // enable a stream of debug output for debug events test

    public void RandomDebugMessage()
    {
        var type = LogType.Log;

        // 20% chance of warning or error

        if( UnityEngine.Random.value > 0.8f )
        {
            type = UnityEngine.Random.value < 0.6f ? LogType.Warning : LogType.Error;
        }

#if UNITY_5
        Debug.logger.Log( type, string.Format( "Level time is {0}", Time.timeSinceLevelLoad ) );
#else
        Debug.unityLogger.Log( type, string.Format( "Level time is {0}", Time.timeSinceLevelLoad ) );
#endif
    }


    //----------------------------------------------------------------------------------------------------
    // custom event

    public int FrameCounter = 0;

    public event Action<object> TickEvent;

    float mTimer = 0.0f;

    void Update()
    {
        FrameCounter++;

        mTimer += Time.deltaTime;

        if( mTimer < 1.0f )
        {
            return;
        }

        mTimer = 0.0f;

        if( TickEvent != null )
        {
            TickEvent( new { levelTime = Time.timeSinceLevelLoad } );
        }
    }

    //----------------------------------------------------------------------------------------------------
    // polymorph

    public int p( int a )
    {
        return a;
    }

    public int p( int a, int b = 1 )
    {
        return a + b;
    }

    public int p2( int a )
    {
        return a;
    }

    public int p2( float a )
    {
        return (int)a + 10;
    }
}
