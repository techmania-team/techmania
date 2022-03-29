using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalizeString : MonoBehaviour
{
    public string key;

    private void OnEnable()
    {
        L10n.LocaleChanged += Localize;
        Localize();
    }

    private void OnDisable()
    {
        L10n.LocaleChanged -= Localize;
    }

    private void Localize()
    {
        if (key == "") return;
        GetComponent<TextMeshProUGUI>().text = L10n.GetString(key);
    }
}
