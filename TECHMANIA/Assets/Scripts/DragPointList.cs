using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A data structure designed for drag notes. It holds an
// immutable list of Vector2s whose x is non-decreasing.
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
// list whose x is larger than the starting point at indices
// 1 and beyond.
public class DragPointList
{
    private List<Vector2> points;
    private Vector2? startPoint;
    private int indexAfterStart;  // Index 1 = this index in "points"

    public DragPointList(List<Vector2> points)
    {
        this.points = new List<Vector2>(points);
        if (points.Count == 0)
        {
            throw new System.ArgumentException("DragPointList cannot be constructed on 0 points.");
        }
        Reset();
    }

    public void Reset()
    {
        startPoint = null;
        indexAfterStart = 1;
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
        if (x <= points[0].x)
        {
            startPoint = points[0];
            indexAfterStart = 1;
            return;
        }
        if (x >= points[^1].x)
        {
            startPoint = points[^1];
            indexAfterStart = points.Count;
            return;
        }
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (points[i].x <= x && x < points[i + 1].x)
            {
                float t = Mathf.InverseLerp(
                    points[i].x, points[i + 1].x, x);
                float y = Mathf.Lerp(
                    points[i].y, points[i + 1].y, t);
                startPoint = new Vector2(x, y);
                indexAfterStart = i + 1;
                return;
            }
        }
    }

    public Vector2 this[int index]
    {
        get
        {
            if (startPoint == null) return points[index];
            if (index == 0) return startPoint.Value;
            return points[indexAfterStart + index - 1];
        }
    }

    public int Count
    {
        get
        {
            if (startPoint == null) return points.Count;
            return points.Count - indexAfterStart + 1;
        }
    }
}
