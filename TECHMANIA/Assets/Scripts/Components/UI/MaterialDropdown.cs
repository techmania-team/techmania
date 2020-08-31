using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MaterialDropdown : MonoBehaviour,
    ISelectHandler, IPointerEnterHandler
{
    // Refer to comments on MaterialTextField.frameOfEndEdit.
    public static bool anyDropdownExpanded;
    public static int frameOfLastCollapse;

    private TMP_Dropdown dropdown;
    private bool expandedOnPreviousFrame;

    static MaterialDropdown()
    {
        anyDropdownExpanded = false;
        frameOfLastCollapse = -1;
    }

    // Start is called before the first frame update
    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        expandedOnPreviousFrame = false;
    }

    // Update is called once per frame
    void Update()
    {
        bool expanded = dropdown.IsExpanded;
        if (expandedOnPreviousFrame != expanded)
        {
            anyDropdownExpanded = expanded;
            if (expanded)
            {
                OnExpand();
            }
            else
            {
                OnCollapse();
            }
        }
        expandedOnPreviousFrame = expanded;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!expandedOnPreviousFrame)
        {
            // PointerEnter covers all items when the dropdown
            // is expanded. We don't want that.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData)
        {
            // Only play sound if selected with keyboard navigation.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnExpand()
    {
        MenuSfx.instance.PlayClickSound();
    }

    public void OnCollapse()
    {
        MenuSfx.instance.PlayClickSound();
        frameOfLastCollapse = Time.frameCount;
    }
}
