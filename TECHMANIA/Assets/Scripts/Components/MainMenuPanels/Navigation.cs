using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Navigation : MonoBehaviour
{
    private static Navigation instance;
    private static Navigation GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<Navigation>();
        }
        return instance;
    }

    public MainMenuPanel mainMenuPanel;
    public SelectTrackPanel selectTrackPanel;
    public OptionsPanel optionsPanel;
    public TouchscreenTestPanel touchscreenTestPanel;

    public Text backButtonText;
    public Text title;

    public enum Location
    {
        MainMenu,
        SelectTrack,
        Options,
        TouchscreenTest
    }
    private Location location;

    // Start is called before the first frame update
    void Start()
    {
        OptionsPanel.ApplyOptionsOnStartUp();
        InternalGoTo(Location.MainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }

    public void GoBack()
    {
        switch (location)
        {
            case Location.MainMenu:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
            case Location.SelectTrack:
                GoTo(Location.MainMenu);
                break;
            case Location.Options:
                GoTo(Location.MainMenu);
                break;
            case Location.TouchscreenTest:
                GoTo(Location.Options);
                break;
        }
    }

    public static void GoTo(Location location)
    {
        GetInstance().InternalGoTo(location);
    }

    private void InternalGoTo(Location location)
    {
        mainMenuPanel.gameObject.SetActive(location == Location.MainMenu);
        selectTrackPanel.gameObject.SetActive(location == Location.SelectTrack);
        optionsPanel.gameObject.SetActive(location == Location.Options);
        touchscreenTestPanel.gameObject.SetActive(location == Location.TouchscreenTest);
        this.location = location;

        RefreshNavigationPanel();
    }

    private void RefreshNavigationPanel()
    {
        switch (location)
        {
            case Location.MainMenu:
                backButtonText.text = "< Quit";
                title.text = "TECHMANIA";
                break;
            case Location.SelectTrack:
                backButtonText.text = "< Main Menu";
                title.text = "Select Track";
                break;
            case Location.Options:
                backButtonText.text = "< Main Menu";
                title.text = "Options";
                break;
            case Location.TouchscreenTest:
                backButtonText.text = "< Options";
                title.text = "Touchscreen Test";
                break;
        }
    }
}
