using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialToggleButton : MonoBehaviour,
    ISelectHandler, IDeselectHandler, ISubmitHandler,
    IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler
{
    public Graphic targetGraphic;
    public Color graphicOffColor;
    public Color graphicOnColor;
    public Image toggleOverlay;
    public Color toggleOverlayOffColor;
    public Color toggleOverlayOnColor;
    public GameObject selectedOutline;
    public bool playSelectSound;

    private Button button;
    private RectTransform rippleRect;
    private RectTransform rippleParentRect;
    private Animator rippleAnimator;
    private bool selected;
    private bool on;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        rippleAnimator = GetComponentInChildren<Animator>();
        rippleRect = rippleAnimator.GetComponent<RectTransform>();
        rippleParentRect = rippleRect.parent.GetComponent<RectTransform>();
        on = false;
        UpdateAppearance();
    }

    public void SetIsOn(bool on)
    {
        this.on = on;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        targetGraphic.color = on ? graphicOnColor : graphicOffColor;
        toggleOverlay.color = on ? toggleOverlayOnColor : toggleOverlayOffColor;
    }

    #region UI event handlers
    private void StartRippleAt(Vector2 startPosition)
    {
        rippleRect.anchoredPosition = startPosition;
        rippleAnimator.SetTrigger("Activate");
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        selectedOutline.SetActive(selected);

        if (eventData is AxisEventData && playSelectSound)
        {
            // Only play sound if selected with keyboard navigation.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        selectedOutline.SetActive(selected);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        StartRippleAt(Vector2.zero);
        MenuSfx.instance.PlayClickSound();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!button.IsInteractable())
        {
            return;
        }

        MenuSfx.instance.PlayClickSound();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TouchInducedPointer.EventIsFromActualMouse(eventData))
        {
            if (button.IsInteractable() && playSelectSound)
            {
                MenuSfx.instance.PlaySelectSound();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.IsInteractable())
        {
            return;
        }

        Vector2 pointerPosition = eventData.position;
        Vector2 rippleStartPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rippleParentRect, pointerPosition, null, out rippleStartPosition))
        {
            StartRippleAt(rippleStartPosition);
        }
    }
    #endregion
}
