using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class TestElement
{
    public int n;
}

[Serializable]
public class TestClassWithDict
{
    public Dictionary<string, TestElement> dict;
}

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Mathf.InverseLerp(5f, 15f, 7f));
        Debug.Log(Mathf.InverseLerp(15f, 5f, 7f));
    }
}
