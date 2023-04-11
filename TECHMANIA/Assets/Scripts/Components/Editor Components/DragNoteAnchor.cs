using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragNoteAnchor : MonoBehaviour
{
    public GameObject controlPointLeft;
    public GameObject controlPointRight;
    public RectTransform pathToControlPointLeft;
    public RectTransform pathToControlPointRight;
    [HideInInspector]
    public int anchorIndex;

    public GameObject GetControlPoint(int index)
    {
        if (index == 0)
            return controlPointLeft;
        else
            return controlPointRight;
    }

    public RectTransform GetPathToControlPoint(int index)
    {
        if (index == 0)
            return pathToControlPointLeft;
        else
            return pathToControlPointRight;
    }

    public int GetControlPointIndex(GameObject controlPoint)
    {
        if (controlPoint == controlPointLeft)
            return 0;
        else
            return 1;
    }
}
