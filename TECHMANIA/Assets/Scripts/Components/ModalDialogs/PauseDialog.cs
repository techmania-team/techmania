using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseDialog : MonoBehaviour
{
    public void OnRestartButtonClick()
    {
        Curtain.DrawCurtainThenGoToScene("Game");
    }

    public void OnQuitButtonClick()
    {
        Navigation.goToSelectTrackPanelOnStart = true;
        Curtain.DrawCurtainThenGoToScene("Main Menu");
    }
}
