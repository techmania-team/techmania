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
        levelText.text = p.level.ToString();
        nameText.SetUp(p.patternName);
    }
}
