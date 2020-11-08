using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInducedPointer : MonoBehaviour
{
    // When using touch controls, at least on my Dell touch monitor,
    // each touch generates 1 PointerClick event, as well as 2
    // PointerEnter events, once on touch start, once 1 frame after
    // touch end. We don't want either of these events to play the
    // select sound - each touch should play one click sound and
    // that's it.
    //
    // This class is somewhat able to tell touch-induced PointerEnter
    // events from mouse-incuded ones, using some heuristic
    // methods. It's ugly, but it works... for now.

    private static int lastTouchExitFrameNumber;
    private static Vector2 lastRejectedPosition;

    // Start is called before the first frame update
    void Start()
    {
        lastTouchExitFrameNumber = -2;
        lastRejectedPosition = Vector2.zero;
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
                lastTouchExitFrameNumber = Time.frameCount;
                break;
            }
        }
    }

    public static bool EventIsFromActualMouse(PointerEventData eventData)
    {
        // If the event thinks it's eligible for click, it's
        // probably from a touch.
        if (eventData.eligibleForClick)
        {
            Reject(eventData);
            return false;
        }

        // If the event happens exactly 1 frame after the last touch
        // exit event, it's probably from a touch.
        if (Time.frameCount == lastTouchExitFrameNumber + 1)
        {
            Reject(eventData);
            return false;
        }

        // If the event happens at exactly the same position as the
        // last rejected event, it's probably from a touch.
        if (eventData.position == lastRejectedPosition) return false;

        return true;
    }

    private static void Reject(PointerEventData eventData)
    {
        lastRejectedPosition = eventData.position;
    }
}
