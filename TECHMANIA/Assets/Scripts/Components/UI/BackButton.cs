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
            if (MaterialTextField.IsEditingAnyTextField())
            {
                return;
            }
            if (MaterialDropdown.IsEditingAnyDropdown())
            {
                return;
            }
            if (!GetComponent<Button>().interactable)
            {
                return;
            }
            if (!GetComponentInParent<CanvasGroup>().interactable)
            {
                return;
            }
            MenuSfx.instance.PlayBackSound();
            GetComponent<TransitionToPanel>()?.Invoke();
            GetComponent<TransitionToScene>()?.Invoke();
        }
    }
}
