using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MaterialDropdown : MonoBehaviour
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
            Debug.Log("MaterialDropdown.anyDropdownExpanded is now " +
                anyDropdownExpanded);
            if (!expanded)
            {
                frameOfLastCollapse = Time.frameCount;
                Debug.Log("MaterialDropdown.frameOfLastCollapse is now " +
                    frameOfLastCollapse);
            }
        }
        expandedOnPreviousFrame = expanded;
    }
}
