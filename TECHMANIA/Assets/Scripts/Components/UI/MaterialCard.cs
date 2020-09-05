using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialCard : MonoBehaviour,
    ISelectHandler, IDeselectHandler, ISubmitHandler,
    IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler
{
    // We do not care if the card ever gets uninteractable. This is
    // because MaterialCard doesn't know about its contents,
    // and it can be difficult to apply disabled colors.

    public GameObject selectedOutline;

    private Button button;
    private RectTransform rippleRect;
    private RectTransform rippleParentRect;
    private Animator rippleAnimator;
    private bool selected;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        rippleAnimator = GetComponentInChildren<Animator>();
        rippleRect = rippleAnimator.GetComponent<RectTransform>();
        rippleParentRect = rippleRect.parent.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {

    }

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
        MenuSfx.instance.PlayClickSound();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TouchInducedPointer.EventIsFromActualMouse(eventData))
        {
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.IsInteractable())
        {
            return;
        }

        Vector2 pointerPosition = (eventData as PointerEventData).position;
        Vector2 rippleStartPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rippleParentRect, pointerPosition, null, out rippleStartPosition))
        {
            StartRippleAt(rippleStartPosition);
        }
    }
}
