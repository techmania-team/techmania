using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EditorSideSheet : MonoBehaviour
{
    public GameObject showButton;
    public GameObject hideButton;

    public void Show()
    {
        gameObject.SetActive(true);
        showButton.SetActive(false);
        hideButton.SetActive(true);
        EventSystem.current.SetSelectedGameObject(hideButton);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        showButton.SetActive(true);
        hideButton.SetActive(false);
        EventSystem.current.SetSelectedGameObject(showButton);
    }
}
