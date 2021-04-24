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
public class TestClass
{
    public string s;
}

public class TestScript : MonoBehaviour
{
    private bool done;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitForWorker());
    }

    private IEnumerator WaitForWorker()
    {
        BackgroundWorker worker = new BackgroundWorker();
        done = false;
        worker.DoWork += DoWork;
        worker.RunWorkerCompleted += RunWorkerCompleted;
        worker.RunWorkerAsync();
        yield return new WaitUntil(() => done);
        Debug.Log("Done!");
    }

    private void DoWork(object sender, DoWorkEventArgs e)
    {
        Thread.Sleep(1000);
        Debug.Log("Can I log from a worker?");
        TestClass deserialized = JsonUtility.FromJson<TestClass>("{\"s\": \"123\"}");
        Debug.Log("Deserialized: " + deserialized.s);
        done = true;
    }

    private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            Debug.Log(e.Error);
        }
    }
}
