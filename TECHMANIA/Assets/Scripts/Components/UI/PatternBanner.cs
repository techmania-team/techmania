using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatternBanner : MonoBehaviour
{
    public bool inEditor;

    [Header("Control scheme")]
    public Image controlIcon;
    public Sprite touchIcon;
    public Sprite keysIcon;
    public Sprite kmIcon;
    public Color defaultColor;
    public Color overrideColor;

    [Header("Playable lanes")]
    public Image laneIcon;
    public Sprite twoLaneIcon;
    public Sprite threeLaneIcon;
    public Sprite fourLaneIcon;
    
    [Header("Level")]
    public TextMeshProUGUI levelText;

    [Header("Name")]
    public ScrollingText nameText;
    
    public void Initialize(PatternMetadata p)
    {
        switch (p.controlScheme)
        {
            case ControlScheme.Touch:
                controlIcon.sprite = touchIcon;
                break;
            case ControlScheme.Keys:
                controlIcon.sprite = keysIcon;
                break;
            case ControlScheme.KM:
                controlIcon.sprite = kmIcon;
                break;
        }

        switch (p.playableLanes)
        {
            case 2:
                laneIcon.sprite = twoLaneIcon;
                break;
            case 3:
                laneIcon.sprite = threeLaneIcon;
                break;
            case 4:
                laneIcon.sprite = fourLaneIcon;
                break;
            default:
                laneIcon.sprite = null;
                break;
        }

        levelText.text = p.level.ToString();

        nameText.SetUp(p.patternName);
    }

    public void InitializeNonExistant()
    {
        controlIcon.sprite = null;
        controlIcon.color = Color.clear;
        laneIcon.sprite = null;
        laneIcon.color = Color.clear;
        levelText.text = "";
        nameText.SetUp("");
    }

    public void MakeControlIconRed()
    {
        controlIcon.color = overrideColor;
    }
}
