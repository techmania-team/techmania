using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class MaterialToggle : MonoBehaviour
{
    public Image track;
    public Image thumb;
    public Image overlay;
    public Color trackColorOn;
    public Color trackColorOff;
    public Color thumbColorOn;
    public Color thumbColorOff;
    public Color overlayColorOn;
    public Color overlayColorOff;

    private Toggle toggle;
    private RectTransform thumbRect;

    // Start is called before the first frame update
    void Start()
    {
        toggle = GetComponent<Toggle>();
        thumbRect = thumb.GetComponent<RectTransform>();
        UpdateAppearance();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateAppearance()
    {
        track.color = toggle.isOn ? trackColorOn : trackColorOff;
        thumb.color = toggle.isOn ? thumbColorOn : thumbColorOff;
        overlay.color = toggle.isOn ? overlayColorOn : overlayColorOff;
        thumbRect.anchorMin = new Vector2(
            toggle.isOn ? 1f : 0f, 0f);
        thumbRect.anchorMax = new Vector2(
            toggle.isOn ? 1f : 0f, 1f);
        thumbRect.pivot = new Vector2(
            toggle.isOn ? 1f : 0f, 0.5f);
    }
}
