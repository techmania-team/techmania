using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultsPanel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnBackButtonClick()
    {
        Navigation.goToSelectTrackPanelOnStart = true;
        SceneManager.LoadScene("Main Menu");
    }
}
