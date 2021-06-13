using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class MaterialToggle : MonoBehaviour,
    ISelectHandler, ISubmitHandler,
    IPointerEnterHandler, IPointerClickHandler
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
    [Range(0f, 1f)]
    public float opacityWhenNotInteractable;
    public bool silent;

    private Toggle toggle;
    private RectTransform thumbRect;
    private bool displayedValue;
    private bool interactable;

    // Start is called before the first frame update
    void Start()
    {
        UpdateAppearance();
    }

    // Update is called once per frame
    void Update()
    {
        if (toggle.isOn != displayedValue ||
            toggle.IsInteractable() != interactable)
        {
            UpdateAppearance();
        }
    }

    public void UpdateAppearance()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
        if (thumbRect == null)
        {
            thumbRect = thumb.GetComponent<RectTransform>();
        }
        displayedValue = toggle.isOn;
        interactable = toggle.IsInteractable();

        Color trackColor = toggle.isOn ? trackColorOn : trackColorOff;
        if (!interactable) trackColor.a *= opacityWhenNotInteractable;
        track.color = trackColor;

        Color thumbColor = toggle.isOn ? thumbColorOn : thumbColorOff;
        if (!interactable) thumbColor.a *= opacityWhenNotInteractable;
        thumb.color = thumbColor;

        overlay.color = toggle.isOn ? overlayColorOn : overlayColorOff;
        thumbRect.anchorMin = new Vector2(
            toggle.isOn ? 1f : 0f, 0f);
        thumbRect.anchorMax = new Vector2(
            toggle.isOn ? 1f : 0f, 1f);
        thumbRect.pivot = new Vector2(
            toggle.isOn ? 1f : 0f, 0.5f);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData && interactable && !silent)
        {
            // Only play sound if selected with keyboard navigation.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (interactable && !silent)
        {
            MenuSfx.instance.PlayClickSound();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TouchInducedPointer.EventIsFromActualMouse(eventData)
            && interactable && !silent)
        {
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (interactable && !silent)
        {
            MenuSfx.instance.PlayClickSound();
        }
    }
}
