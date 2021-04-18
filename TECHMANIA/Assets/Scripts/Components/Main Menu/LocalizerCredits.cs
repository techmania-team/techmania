using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalizerCredits : MonoBehaviour
{
    void Start()
    {
        TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();

        List<string> lines = new List<string>();
        foreach (KeyValuePair<string, List<string>> pair in
            Locale.GetLanguageNameToLocalizerNames())
        {
            if (pair.Value.Count == 0) continue;
            lines.Add(string.Join(", ", pair.Value) +
                $" ({pair.Key})");
        }
        text.text = string.Join("\n", lines);
    }
}
