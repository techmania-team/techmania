using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    public void OnEditorButtonClick()
    {
        Curtain.DrawCurtainThenGoToScene("Editor");
    }

    public void OnQuitButtonClick()
    {
        Curtain.DrawCurtainThenQuit();
    }
}
