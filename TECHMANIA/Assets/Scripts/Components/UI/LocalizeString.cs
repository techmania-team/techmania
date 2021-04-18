using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalizeString : MonoBehaviour
{
    public string key;

    private void OnEnable()
    {
        Locale.LocaleChanged += Localize;
        Localize();
    }

    private void OnDisable()
    {
        Locale.LocaleChanged -= Localize;
    }

    private void Localize()
    {
        if (key == "") return;
        GetComponent<TextMeshProUGUI>().text = Locale.GetString(key);
    }
}
