using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatternBanner : MonoBehaviour
{
    public Image controlIcon;
    public Sprite touchIcon;
    public Sprite keysIcon;
    public Sprite kmIcon;
    public TextMeshProUGUI levelText;
    public ScrollingText nameText;
    public Color defaultColor;
    public Color overrideColor;

    private ControlScheme intendedScheme;
    
    public void Initialize(PatternMetadata p)
    {
        intendedScheme = p.controlScheme;
        RefreshControlIcon();
        levelText.text = p.level.ToString();
        nameText.SetUp(p.patternName);
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
