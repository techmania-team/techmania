using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A data structure designed for drag notes. It holds an
// immutable list of Vector2s whose x is non-decreasing/
// non-increasing.
//
// The main operation is "interpolate": given an input X,
// find a line segment that contains the X, and interpolate
// between the segment's end points to get the output Y.
//
// A similar operation is "set starting point": given an
// input X, interpolate a Y and store the new point as the
// "starting point".
//
// When the outside world accesses this structure, it will
// see the starting point at index 0, and points in the original
// list whose x is larger/smaller than the starting point
// at indices 1 and beyond.
public class DragPointList
{
    // X is non-decreasing.
    private List<Vector2> points;
    // Whether the points were reversed at construction.
    private bool reversed;
    private Vector2? startPoint;
    // StartPoint is interpolated between
    // points[indexBeforeStartPoint]
    // and points[indexAfterStartPoint].
    private int indexBeforeStartPoint;
    private int indexAfterStartPoint;

    public DragPointList(List<Vector2> points)
    {
        if (points.Count < 2)
        {
            throw new System.ArgumentException("DragPointList cannot be constructed on 0 or 1 points.");
        }
        this.points = new List<Vector2>(points);

        // Possibly reverse the list so that its X is
        // non-decreasing.
        if (points[0].x <= points[^1].x)
        {
            reversed = false;
        }
        else
        {
            this.points.Reverse();
            reversed = true;
        }

        Reset();
    }

    public void Reset()
    {
        startPoint = null;
        indexBeforeStartPoint = 0;
    }

    public float InterpolateForY(float x)
    {
        if (x <= points[0].x) return points[0].y;
        if (x >= points[^1].x) return points[^1].y;
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (points[i].x <= x && x < points[i + 1].x)
            {
                float t = Mathf.InverseLerp(
                    points[i].x, points[i + 1].x, x);
                return Mathf.Lerp(points[i].y, points[i + 1].y, t);
            }
        }

        throw new System.Exception("Unable to interpolate.");
    }

    public void SetStart(float x)
    {
        x = Mathf.Clamp(x, points[0].x, points[^1].x);
        for (int i = 0; i < points.Count; i++)
        {
            if (x == points[i].x)
            {
                startPoint = points[i];
                indexBeforeStartPoint = i - 1;
                indexAfterStartPoint = i + 1;
                return;
            }
            if (i < points.Count - 1 &&
                points[i].x < x && x < points[i + 1].x)
            {
                float t = Mathf.InverseLerp(
                    points[i].x, points[i + 1].x, x);
                float y = Mathf.Lerp(
                    points[i].y, points[i + 1].y, t);
                startPoint = new Vector2(x, y);
                indexBeforeStartPoint = i;
                indexAfterStartPoint = i + 1;
                return;
            }
        }
    }

    public Vector2 this[int index]
    {
        get
        {
            if (startPoint == null)
            {
                return reversed ? points[^(index + 1)] : points[index];
            }
            if (index == 0) return startPoint.Value;
            if (reversed)
            {
                return points[indexBeforeStartPoint - index + 1];
            }
            else
            {
                return points[indexAfterStartPoint + index - 1];
            }
        }
    }

    public int Count
    {
        get
        {
            if (startPoint == null) return points.Count;
            if (reversed)
            {
                return indexBeforeStartPoint + 2;
            }
            else
            {
                return points.Count - indexAfterStartPoint + 1;
            }
        }
    }
}
