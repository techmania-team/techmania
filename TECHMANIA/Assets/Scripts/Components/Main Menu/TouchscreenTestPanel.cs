using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TouchscreenTestPanel : MonoBehaviour
{
    public GameObject fingerIndicator;

    private Dictionary<int, GameObject> fingerIdToIndicator;

    private void OnEnable()
    {
        fingerIdToIndicator = new Dictionary<int, GameObject>();
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<int, GameObject> pair in 
            fingerIdToIndicator)
        {
            Destroy(pair.Value);
        }
    }

    private Vector2 TouchPositionToAnchoredPosition(
        Vector2 touchPosition)
    {
        Vector2 anchoredPosition = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(),
            touchPosition,
            null,
            out anchoredPosition);
        return anchoredPosition;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    {
                        GameObject indicator = Instantiate(
                            fingerIndicator, transform);
                        indicator.GetComponent<RectTransform>()
                            .anchoredPosition = 
                            TouchPositionToAnchoredPosition(
                                t.position);
                        indicator.GetComponentInChildren
                            <TextMeshProUGUI>()
                            .text = Locale.GetStringAndFormat(
                                "touchscreen_test_finger_indicator",
                                t.fingerId);
                        fingerIdToIndicator.Add(t.fingerId, indicator);
                    }
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    {
                        Destroy(fingerIdToIndicator[t.fingerId]);
                        fingerIdToIndicator.Remove(t.fingerId);
                    }
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    {
                        fingerIdToIndicator[t.fingerId]
                            .GetComponent<RectTransform>()
                            .anchoredPosition = 
                            TouchPositionToAnchoredPosition(
                                t.position);
                    }
                    break;
            }
        }
    }
}
