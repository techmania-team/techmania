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
        TestClassWithDict o = new TestClassWithDict();
        o.dict = new Dictionary<string, TestElement>();
        o.dict.Add("a", new TestElement() { n = 1 });

        Debug.Log(JsonUtility.ToJson(o));
    }
}
