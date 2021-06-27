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
        Debug.Log("current culture:" + CultureInfo.CurrentCulture.Name);
        Debug.Log(float.Parse("1,23"));
        Debug.Log(float.Parse("1.23"));
        CultureInfo infoBackup = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("it");
        Debug.Log("current culture:" + CultureInfo.CurrentCulture.Name);
        Debug.Log(float.Parse("1,23"));
        Debug.Log(float.Parse("1.23"));
        CultureInfo.CurrentCulture = infoBackup;
        Debug.Log("current culture:" + CultureInfo.CurrentCulture.Name);
        Debug.Log(float.Parse("1,23"));
        Debug.Log(float.Parse("1.23"));
    }
}
