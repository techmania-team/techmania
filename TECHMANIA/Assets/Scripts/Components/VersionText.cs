using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VersionText : MonoBehaviour
{
    public MessageDialog messageDialog;

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            Ruleset.RefreshInstance();
        }
        catch (Exception ex)
        {
            if (messageDialog != null)
            {
                messageDialog.Show(
                    Locale.GetStringAndFormat(
                        "custom_ruleset_load_error_format",
                        ex.Message));
            }
        }

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();
        versionText.text = Application.version;
        if (Ruleset.instance != null &&
            Ruleset.instance.isCustom)
        {
            versionText.text += "\n" + Locale.GetString(
                "custom_ruleset_indicator");
        }
    }
}
