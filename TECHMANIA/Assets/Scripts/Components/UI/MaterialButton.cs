using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialButton : MonoBehaviour
{
    public Color textColor;
    public Color disabledTextColor;
    public Color buttonColor;
    public Color disabledButtonColor;

    public GameObject selectedOutline;

    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI text;
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
        text = GetComponentInChildren<TextMeshProUGUI>();
        text.color = textColor;
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
            text.color = newInteractable ?
                textColor : disabledTextColor;
            buttonImage.color = newInteractable ?
                buttonColor : disabledButtonColor;
        }
        interactable = newInteractable;

        bool newSelected = (EventSystem.current.currentSelectedGameObject == gameObject);
        if (newSelected != selected)
        {
            selectedOutline.SetActive(newSelected);
        }
        selected = newSelected;

        if (selected && Input.GetButtonDown("Submit"))
        {
            StartRippleAt(Vector2.zero);
        }
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
}
