using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VersionText : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();
        versionText.text = Application.version;
    }
}
