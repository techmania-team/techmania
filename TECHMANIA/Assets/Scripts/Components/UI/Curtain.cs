using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Curtain : MonoBehaviour
{
    private Image image;
    private static bool transitioning;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        transitioning = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TransitionToScene(string name)
    {
        // TODO: fade to black, load scene, fade to transparent,
        // self destruct
        SceneManager.LoadScene(name);
    }
}
