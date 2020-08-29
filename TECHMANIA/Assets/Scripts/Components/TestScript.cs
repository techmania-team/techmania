using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestScript : MonoBehaviour, ISubmitHandler, ICancelHandler
{
    Text text;

    public void OnCancel(BaseEventData eventData)
    {
        // Debug.Log("Cancel event received at " + gameObject.name);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // Debug.Log("Submit event received at " + gameObject.name);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("IsExpanded: " + GetComponent<TMP_Dropdown>().IsExpanded);
    }
}
