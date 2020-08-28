using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class Panel : MonoBehaviour
{
    public static Panel current;

    // Start is called before the first frame update
    void Start()
    {
        if (current == null) current = this;
    }
}
