using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SerializableDemoV1 d = new SerializableDemoV1()
        {
            v1field = "field"
        };
        string json = d.Serialize(optimizeForSaving: false);
        Debug.Log(json);

        SerializableDemoV2 upgraded = SerializableDemoV2.Deserialize(json) as SerializableDemoV2;
        Debug.Log(upgraded.version);
        Debug.Log(upgraded.v2field);

        upgraded = SerializableDemoV2.Deserialize("{\"version\":\"1\"}") as SerializableDemoV2;
    }
}
