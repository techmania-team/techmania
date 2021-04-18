using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    public TextAsset textAsset;

    // Start is called before the first frame update
    void Start()
    {
        StringReader stringReader = new StringReader(textAsset.text);
        NReco.Csv.CsvReader reader = new NReco.Csv.CsvReader(stringReader);
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldsCount; i++)
            {
                Debug.Log(reader[i]);
            }
        }
    }
}
