using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverMessage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject body;

    public void OnPointerEnter(PointerEventData eventData)
    {
        body.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        body.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        body.SetActive(false);
    }
}
