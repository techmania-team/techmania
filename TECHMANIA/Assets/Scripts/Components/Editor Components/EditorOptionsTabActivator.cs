using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorOptionsTabActivator : MonoBehaviour
{
    public CanvasGroup horizontalLayout;
    public GameObject optionsTab;

    private void OnEnable()
    {
        horizontalLayout.alpha = 0f;
        optionsTab.SetActive(true);
    }

    private void OnDisable()
    {
        horizontalLayout.alpha = 1f;
        optionsTab.SetActive(false);
    }
}
