using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class TestClass
{
    public float f;
}

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(float.Parse("1.23", CultureInfo.GetCultureInfo("it")));
        Debug.Log(float.Parse("1,23", CultureInfo.GetCultureInfo("it")));
        Debug.Log(float.Parse("1.23", NumberFormatInfo.InvariantInfo));
        Debug.Log(float.Parse("1,23", NumberFormatInfo.InvariantInfo));

        string json = "{\"f\":1.23}";
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("it");
        Debug.Log(JsonUtility.FromJson<TestClass>(json).f);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Debug.Log(JsonUtility.FromJson<TestClass>(json).f);
    }
}
