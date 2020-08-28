using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialButton : MonoBehaviour,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    public Color textColor;
    public Color disabledTextColor;
    public Color buttonColor;
    public Color disabledButtonColor;

    public GameObject selectedOutline;

    private Button button;
    private Image buttonImage;
    private Graphic buttonContent;
    private RectTransform rippleRect;
    private RectTransform rippleParentRect;
    private Animator rippleAnimator;
    private bool interactable;
    private bool selected;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        foreach (Graphic g in GetComponentsInChildren<Graphic>())
        {
            if (g.transform != transform)
            {
                buttonContent = g;
                break;
            }
        }
        buttonContent.color = textColor;
        rippleAnimator = GetComponentInChildren<Animator>();
        rippleRect = rippleAnimator.GetComponent<RectTransform>();
        rippleParentRect = rippleRect.parent.GetComponent<RectTransform>();
        interactable = true;
        selected = false;
    }

    // Update is called once per frame
    void Update()
    {
        bool newInteractable = button.IsInteractable();
        if (newInteractable != interactable)
        {
            buttonContent.color = newInteractable ?
                textColor : disabledTextColor;
            buttonImage.color = newInteractable ?
                buttonColor : disabledButtonColor;
        }
        interactable = newInteractable;
    }

    public void StartRipple(BaseEventData data)
    {
        if (!button.IsInteractable())
        {
            return;
        }

        Vector2 pointerPosition = (data as PointerEventData).position;
        Vector2 rippleStartPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rippleParentRect, pointerPosition, null, out rippleStartPosition))
        {
            StartRippleAt(rippleStartPosition);
        }
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
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        selectedOutline.SetActive(selected);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (selected)
        {
            StartRippleAt(Vector2.zero);
        }
    }
}
