using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (MaterialTextField.editingAnyTextField ||
                MaterialTextField.frameOfLastEndEdit == Time.frameCount)
            {
                return;
            }
            if (MaterialDropdown.anyDropdownExpanded ||
                MaterialDropdown.frameOfLastCollapse == Time.frameCount)
            {
                return;
            }
            if (!GetComponentInParent<CanvasGroup>().interactable)
            {
                return;
            }
            MenuSfx.instance.PlayBackSound();
            GetComponent<TransitionToPanel>()?.Invoke();
        }
    }
}
