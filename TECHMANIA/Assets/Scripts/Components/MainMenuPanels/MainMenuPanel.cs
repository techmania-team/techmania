using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    public void OnEditorButtonClick()
    {
        FindObjectOfType<Curtain>().DrawCurtainThenGoToScene("Editor");
    }

    public void OnQuitButtonClick()
    {
        FindObjectOfType<Curtain>().DrawCurtainThenQuit();
    }
}
