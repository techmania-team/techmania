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

    [Header("Performance medal")]
    public Image medalIcon;
    public Sprite allComboIcon;
    public Sprite perfectPlayIcon;
   
    private ControlScheme intendedScheme;
    
    public void Initialize(PatternMetadata p, Record r = null)
    {
        intendedScheme = p.controlScheme;
        RefreshControlIcon();

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

        if (medalIcon != null)
        {
            PerformanceMedal medal = PerformanceMedal.NoMedal;
            if (r != null)
            {
                medal = r.medal;
            }
            switch (medal)
            {
                case PerformanceMedal.AbsolutePerfect:
                case PerformanceMedal.PerfectPlay:
                    medalIcon.sprite = perfectPlayIcon;
                    medalIcon.enabled = true;
                    break;
                case PerformanceMedal.AllCombo:
                    medalIcon.sprite = allComboIcon;
                    medalIcon.enabled = true;
                    break;
                case PerformanceMedal.NoMedal:
                    medalIcon.enabled = false;
                    break;
            }
        }
    }

    private void OnEnable()
    {
        ModifierSidesheet.ModifierChanged += RefreshControlIcon;
    }

    private void OnDisable()
    {
        ModifierSidesheet.ModifierChanged -= RefreshControlIcon;
    }

    private void RefreshControlIcon()
    {
        Sprite sprite = null;
        Color color = defaultColor;

        ControlScheme actualScheme = intendedScheme;
        if (!inEditor)
        {
            switch (Modifiers.instance.controlOverride)
            {
                case Modifiers.ControlOverride.OverrideToTouch:
                    actualScheme = ControlScheme.Touch;
                    break;
                case Modifiers.ControlOverride.OverrideToKeys:
                    actualScheme = ControlScheme.Keys;
                    break;
                case Modifiers.ControlOverride.OverrideToKM:
                    actualScheme = ControlScheme.KM;
                    break;
            }
        }
        if (actualScheme != intendedScheme)
        {
            color = overrideColor;
        }

        switch (actualScheme)
        {
            case ControlScheme.Touch:
                sprite = touchIcon;
                break;
            case ControlScheme.Keys:
                sprite = keysIcon;
                break;
            case ControlScheme.KM:
                sprite = kmIcon;
                break;
        }

        controlIcon.sprite = sprite;
        controlIcon.color = color;
    }
}
