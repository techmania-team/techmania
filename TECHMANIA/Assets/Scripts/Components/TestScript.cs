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
        ListView<int> list = new ListView<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.LogToConsole();
        list.RemoveFirst();
        list.LogToConsole();
        list.RemoveFirst();
        list.LogToConsole();
        list.RemoveFirst();
        list.LogToConsole();
        list.RemoveFirst();
        list.LogToConsole();
    }
}
