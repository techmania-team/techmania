using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialToggleButton : MonoBehaviour,
    ISelectHandler, IDeselectHandler, ISubmitHandler,
    IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler
{
    public Image icon;
    public Color iconOffColor;
    public Color iconOnColor;
    public Image toggleOverlay;
    public Color toggleOverlayOffColor;
    public Color toggleOverlayOnColor;
    public GameObject selectedOutline;

    private Button button;
    private RectTransform rippleRect;
    private RectTransform rippleParentRect;
    private Animator rippleAnimator;
    private bool interactable;
    private bool selected;
    private bool on;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        rippleAnimator = GetComponentInChildren<Animator>();
        rippleRect = rippleAnimator.GetComponent<RectTransform>();
        rippleParentRect = rippleRect.parent.GetComponent<RectTransform>();
        interactable = true;
        on = false;
        UpdateAppearance();
    }

    // Update is called once per frame
    void Update()
    {
        interactable = button.IsInteractable();
    }

    public void Toggle()
    {
        on = !on;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        icon.color = on ? iconOnColor : iconOffColor;
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

        if (eventData is AxisEventData)
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
        if (!interactable)
        {
            return;
        }

        MenuSfx.instance.PlayClickSound();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TouchInducedPointer.EventIsFromActualMouse(eventData))
        {
            if (interactable)
            {
                MenuSfx.instance.PlaySelectSound();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable)
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
