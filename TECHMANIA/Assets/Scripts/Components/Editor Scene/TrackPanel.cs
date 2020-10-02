using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// TODO: deprecate this.
public class TrackPanel : MonoBehaviour
{
    [Header("Patterns")]
    public VerticalLayoutGroup patternList;
    public GameObject patternTemplate;
    public Button deleteButton;
    public Button openButton;
    private int selectedPatternIndex;

    public void Open()
    {
        if (selectedPatternIndex < 0) return;
        EditorNavigation.SetCurrentPattern(selectedPatternIndex);
        EditorNavigation.GoTo(EditorNavigation.Location.PatternMetadata);
    }
}
