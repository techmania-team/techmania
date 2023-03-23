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
}
