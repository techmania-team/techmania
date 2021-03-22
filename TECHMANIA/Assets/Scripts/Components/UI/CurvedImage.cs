using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CurvedImage : Image
{
    public float scale;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        // Debug.Log("OnPopulateMesh called");
        vh.Clear();

        IPointsOnCurveProvider pointsProvider =
            GetComponentInParent<IPointsOnCurveProvider>();
        if (pointsProvider == null) return;
        IList<Vector2> pointsOnCurve = pointsProvider
            .GetVisiblePointsOnCurve();
        if (pointsOnCurve == null ||
            pointsOnCurve.Count == 0)
        {
            return;
        }
        float curveWidth = rectTransform.rect.height;
        float halfCurveWidth = curveWidth * 0.5f * scale;

        // Add 2 points before the curve.
        // TODO: these 2 points are not needed.
        Vector2 forward = (pointsOnCurve[1] -
            pointsOnCurve[0]).normalized;
        Vector2 left = new Vector2(-forward.y, forward.x);
        UIVertex vert = UIVertex.simpleVert;
        vert.position = pointsOnCurve[0]
            - halfCurveWidth * forward
            + halfCurveWidth * left;
        vert.color = color;
        vert.uv0 = new Vector2(0f, 1f);
        vh.AddVert(vert);

        vert.position = pointsOnCurve[0]
            - halfCurveWidth * forward
            - halfCurveWidth * left;
        vert.color = color;
        vert.uv0 = new Vector2(0f, 0f);
        vh.AddVert(vert);

        // Calculate left vector on each point. Then generate
        // vertices.
        for (int i = 0; i < pointsOnCurve.Count; i++)
        {
            forward = Vector2.zero;
            if (i < pointsOnCurve.Count - 1)
            {
                forward += (pointsOnCurve[i + 1] -
                    pointsOnCurve[i]).normalized;
            }
            if (i > 0)
            {
                forward += (pointsOnCurve[i] -
                    pointsOnCurve[i - 1]).normalized;
            }
            forward.Normalize();
            left = new Vector2(-forward.y, forward.x);

            float u = (float)i / (pointsOnCurve.Count - 1);
            u = u * 0.5f + 0.25f;

            vert.position = pointsOnCurve[i] +
                halfCurveWidth * left;
            vert.color = color;
            vert.uv0 = new Vector2(u, 1f);
            vh.AddVert(vert);

            vert.position = pointsOnCurve[i] -
                halfCurveWidth * left;
            vert.color = color;
            vert.uv0 = new Vector2(u, 0f);
            vh.AddVert(vert);
        }

        // Add 2 points after the curve.
        forward = (pointsOnCurve[pointsOnCurve.Count - 1] -
            pointsOnCurve[pointsOnCurve.Count - 2]).normalized;
        left = new Vector2(-forward.y, forward.x);
        vert.position = pointsOnCurve[pointsOnCurve.Count - 1]
            + halfCurveWidth * forward
            + halfCurveWidth * left;
        vert.color = color;
        vert.uv0 = new Vector2(1f, 1f);
        vh.AddVert(vert);

        vert.position = pointsOnCurve[pointsOnCurve.Count - 1]
            + halfCurveWidth * forward
            - halfCurveWidth * left;
        vert.color = color;
        vert.uv0 = new Vector2(1f, 0f);
        vh.AddVert(vert);

        // Triangles.
        for (int i = 0; i < pointsOnCurve.Count + 1; i++)
        {
            // #2i: left
            // #2i+1: right
            // #2i+2: next left
            // #2i+3: next right
            vh.AddTriangle(2 * i + 1, 2 * i, 2 * i + 2);
            vh.AddTriangle(2 * i + 3, 2 * i + 1, 2 * i + 2);
        }
    }
}
