using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatternRadioButton : MonoBehaviour
{
    public Image controlIcon;
    public Sprite touchIcon;
    public Sprite keysIcon;
    public Sprite kmIcon;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI nameText;
    
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
        nameText.text = p.patternName;
    }
}
