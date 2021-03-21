using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuPanel : MonoBehaviour
{
    // When exiting from the Game/Result scene, set this flag to skip
    // the welcome mat, loading text and menu buttons, and navigate to
    // the select track panel.
    public static bool skipToTrackSelect;

    // When the note skin changed, set this flag to show the loading
    // text again.
    public static bool returnToLoadingText;

    public GameObject selectTrackPanel;
    public GameObject welcomeMat;
    public TextMeshProUGUI loadingText;
    public GlobalResourceLoader globalResourceLoader;
    public GameObject menuButtons;
    public GameObject firstMenuButton;
    public MessageDialog messageDialog;

    static MainMenuPanel()
    {
        skipToTrackSelect = false;
        returnToLoadingText = false;
    }

    private void Start()
    {
        if (skipToTrackSelect)
        {
            Debug.Log("skipping to track select");
            skipToTrackSelect = false;
            ShowMenuButtons();
            gameObject.SetActive(false);
            selectTrackPanel.SetActive(true);
        }
        else
        {
            ShowWelcomeMat();
        }
    }

    private void OnEnable()
    {
        if (returnToLoadingText)
        {
            returnToLoadingText = false;
            ShowLoadingText();
        }
    }

    public void ShowWelcomeMat()
    {
        Debug.Log("ShowWelcomeMat");
        welcomeMat.SetActive(true);
        loadingText.gameObject.SetActive(false);
        menuButtons.SetActive(false);

        // Welcome mat will call ShowLoadingText.
    }

    public void ShowLoadingText()
    {
        Debug.Log("ShowLoadingText");
        welcomeMat.SetActive(false);
        loadingText.gameObject.SetActive(true);
        menuButtons.SetActive(false);

        // Wait for GlobalResourceLoader to complete, then Update
        // will call ShowMenuButtons.
    }

    public void ShowMenuButtons()
    {
        Debug.Log("ShowMenuButtons");
        welcomeMat.SetActive(false);
        loadingText.gameObject.SetActive(false);
        menuButtons.SetActive(true);
        EventSystem.current.SetSelectedGameObject(
            firstMenuButton.gameObject);
    }

    private void Update()
    {
        if (loadingText.gameObject.activeSelf)
        {
            loadingText.text = globalResourceLoader.statusText;
            if (globalResourceLoader.state !=
                GlobalResourceLoader.State.Loading)
            {
                if (globalResourceLoader.state ==
                    GlobalResourceLoader.State.Error)
                {
                    messageDialog.Show(globalResourceLoader.error);
                }
                ShowMenuButtons();
            }
        }
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
