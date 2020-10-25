using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    public TextMeshProUGUI versionText;

    private void Start()
    {
        versionText.text = Application.version;
    }

    public void OnEditorButtonClick()
    {
        Curtain.DrawCurtainThenGoToScene("Editor");
    }

    public void OnQuitButtonClick()
    {
        Curtain.DrawCurtainThenQuit();
    }
}
