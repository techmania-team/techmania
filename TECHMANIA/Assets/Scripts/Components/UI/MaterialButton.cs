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

    private Button button;
    private TextMeshProUGUI text;
    private RectTransform rippleRect;
    private RectTransform rippleParentRect;
    private Animator rippleAnimator;
    private bool interactable;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        text.color = textColor;
        rippleAnimator = GetComponentInChildren<Animator>();
        rippleRect = rippleAnimator.GetComponent<RectTransform>();
        rippleParentRect = rippleRect.parent.GetComponent<RectTransform>();
        interactable = true;
    }

    // Update is called once per frame
    void Update()
    {
        bool newInteractable = button.IsInteractable();
        if (newInteractable != interactable)
        {
            text.color = newInteractable ?
                textColor : disabledTextColor;
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
            rippleRect.anchoredPosition = rippleStartPosition;
        }
        rippleAnimator.SetTrigger("Activate");
    }
}
