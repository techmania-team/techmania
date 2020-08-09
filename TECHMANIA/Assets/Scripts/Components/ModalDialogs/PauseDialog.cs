using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseDialog : ModalDialog
{
    private static PauseDialog instance;
    private static PauseDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<PauseDialog>(includeInactive: true);
        }
        return instance;
    }

    public static void Show()
    {
        GetInstance().InternalShow();
    }
    public static bool IsResolved()
    {
        return GetInstance().resolved;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnResumeButtonClick();
        }
    }

    private void InternalShow()
    {
        resolved = false;
        gameObject.SetActive(true);
    }

    public void OnResumeButtonClick()
    {
        resolved = true;
        gameObject.SetActive(false);
    }

    public void OnRestartButtonClick()
    {
        SceneManager.LoadScene("Game");
    }

    public void OnQuitButtonClick()
    {
        Navigation.goToSelectTrackPanelOnStart = true;
        SceneManager.LoadScene("Main Menu");
    }
}
