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
        SerializableDemoV1 d = new SerializableDemoV1("field");
        string json = d.Serialize();
        Debug.Log(json);

        SerializableDemoV2 de = SerializableDemoV2.Deserialize(json) as SerializableDemoV2;
        Debug.Log(de.version);
        Debug.Log(de.v2field);
    }
}
