using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuPanel : MonoBehaviour
{
    public void OnStartButtonClick()
    {
        Navigation.GoTo(Navigation.Location.SelectTrack);
    }

    public void OnEditorButtonClick()
    {
        SceneManager.LoadScene("Editor");
    }

    public void OnOptionsButtonClick()
    {
        Navigation.GoTo(Navigation.Location.Options);
    }
}
