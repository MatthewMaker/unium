using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class Tutorial : MonoBehaviour
{
    public bool OpenBrowser = true;
    public Text PickupText;
    public GameObject Reminder;

    static bool OpenOnce    = true;

    int         mNumPickups;

    // exposed event for tutorial script to hook into
    public event Action<object> OnPickupCollected;

    void Start()
    {
        var index = Path.Combine( Application.streamingAssetsPath, "tutorial/index.html" );

        if( File.Exists( index ) )
        {
            Reminder.gameObject.SetActive( false );

            if( OpenBrowser && OpenOnce )
            {
                OpenOnce = false;
                Process.Start( "http://localhost:8342/tutorial/index.html" );
            }
        }
        else
        {
            Reminder.gameObject.SetActive( true );
            Debug.LogError( "Failed to find /StreamingAssets/tutorial/index.html" );
            Debug.LogError( "Please unzip the tutorial.zip file to the StreamingAssets folder" );
        }
    }

    void PickupCreated()
    {
        mNumPickups++;
        PickupText.text = mNumPickups.ToString();
    }

    void PickupCollected()
    {
        mNumPickups--;
        PickupText.text = mNumPickups.ToString();

        GetComponent<AudioSource>().Play();

        Debug.Log( "Pickup collected" );

        if( OnPickupCollected != null )
        {
            OnPickupCollected( "Pickup Collected" );
        }
    }

    public void SayHello()
    {
        Debug.Log( "Hello from Unium" );
    }
}
