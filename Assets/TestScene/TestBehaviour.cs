using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////////////////
// class for testing GQL functionality

public class TestBehaviour : MonoBehaviour
{
    public int      SomeID      = 1;
    public float    RandomValue;

    void Start()
    {
        RandomValue = Random.value;
    }

    public int CallThisFunction()
    {
        Debug.Log($"CallThisFunction() called on {gameObject.name}:{SomeID}");
        return SomeID;
    }

    public int CallThisFunctionWithParams( int a, int b )
    {
        return a + b;
    }
}
