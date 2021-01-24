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
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();
        versionText.text = Application.version;

        try
        {
            Ruleset.RefreshInstance();
        }
        catch (Exception ex)
        {
            if (messageDialog != null)
            {
                messageDialog.Show("An error occurred when loading custom ruleset:\n\n"
                    + ex.Message
                    + "\n\nFor this session TECHMANIA will use the default ruleset.");
            }
        }
        if (Ruleset.instance.isCustom)
        {
            versionText.text += "\nCustom ruleset active";
        }
    }
}
