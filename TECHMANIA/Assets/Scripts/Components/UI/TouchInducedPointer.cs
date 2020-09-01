using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInducedPointer : MonoBehaviour
{
    // When using touch controls, at least with Unity Remote on my
    // iPad, Unity seems to move a fictional mouse pointer after
    // touches. This always happens 1 frame after a touch exit,
    // and is not affected by Input.simulateMouseWithTouches.
    //
    // This phantom pointer creates unwanted PointerEnter events,
    // and I can't find a way to tell these PointerEnter events from
    // actual PointerEnter events, because they both claim to be
    // from pointer #-1 (left mouse button).
    //
    // This class is a reluctant workaround, by recording the position
    // of the last touch exit. All UI components should ignore
    // pointer events happening at this exact position.

    public static Vector2 lastTouchExitPosition { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        lastTouchExitPosition = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Ended ||
                t.phase == TouchPhase.Canceled)
            {
                lastTouchExitPosition = t.position;
            }
        }
    }

    public static bool EventIsFromActualMouse(PointerEventData eventData)
    {
        return eventData.pointerId < 0 &&
            eventData.position != lastTouchExitPosition;
    }
}
