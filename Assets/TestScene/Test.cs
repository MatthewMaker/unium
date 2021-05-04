using System;
using gw.proto.utils;
using gw.unium;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

////////////////////////////////////////////////////////////////////////////////////////////////////

public class Test : MonoBehaviour
{
    public Text IPText;


    //----------------------------------------------------------------------------------------------------

    void Start()
    {
        // check we can add routes dynamically
        // TODO: gw - temporarily remove test route - RoutesHTTP is protected
        Unium.RoutesHTTP.AddImmediate( "/test", (req, path) => req.Respond( @"{""test"":""ok""}" ) );

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

        if( Random.value > 0.8f )
        {
            type = Random.value < 0.6f ? LogType.Warning : LogType.Error;
        }

#if UNITY_5
        Debug.logger.Log( type, string.Format( "Level time is {0}", Time.timeSinceLevelLoad ) );
#else
        Debug.unityLogger.Log( type, $"Level time is {Time.timeSinceLevelLoad}");
#endif
    }


    //----------------------------------------------------------------------------------------------------
    // custom event

    public int FrameCounter;

    public event Action<object> TickEvent;

    float mTimer;

    void Update()
    {
        FrameCounter++;

        mTimer += Time.deltaTime;

        if( mTimer < 1.0f )
        {
            return;
        }

        mTimer = 0.0f;

        TickEvent?.Invoke( new { levelTime = Time.timeSinceLevelLoad } );
    }
}
