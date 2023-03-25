using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class TopLevelObjects : MonoBehaviour
{
    public Canvas mainCanvas;
    public UIDocument mainUiDocument;
    public Canvas editorCanvas;
    public TrackSetupPanel trackSetupPanel;
    public PatternPanel patternPanel;
    public EventSystem eventSystem;

    public static TopLevelObjects instance { get; private set; }

    private void Start()
    {
        instance = this;
    }

    #region UI document
    // We cannot disable/enable the UIDocument component or its
    // GameObject, doing so will clear all contents.
    // https://forum.unity.com/threads/does-uidocument-clear-contents-when-disabled.1097659/
    public void HideUiDocument()
    {
        mainUiDocument.rootVisualElement.style.display = 
            DisplayStyle.None;
    }

    public void ShowUiDocument()
    {
        mainUiDocument.rootVisualElement.style.display =
            DisplayStyle.Flex;
    }
    #endregion
}
