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

        // Calculate left vector on each point. Then generate
        // vertices.
        Vector2 forward, left;
        UIVertex vert = UIVertex.simpleVert;
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
            u = u * 0.5f;

            vert = new UIVertex();
            vert.position = pointsOnCurve[i] +
                halfCurveWidth * left;
            vert.color = color;
            vert.uv0 = ProjectUvOnTexture(u, 1f);
            vh.AddVert(vert);

            vert.position = pointsOnCurve[i] -
                halfCurveWidth * left;
            vert.color = color;
            vert.uv0 = ProjectUvOnTexture(u, 0f);
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
        vert.uv0 = ProjectUvOnTexture(1f, 1f);
        vh.AddVert(vert);

        vert.position = pointsOnCurve[pointsOnCurve.Count - 1]
            + halfCurveWidth * forward
            - halfCurveWidth * left;
        vert.color = color;
        vert.uv0 = ProjectUvOnTexture(1f, 0f);
        vh.AddVert(vert);

        // Triangles.
        for (int i = 0; i < pointsOnCurve.Count; i++)
        {
            // #2i: left
            // #2i+1: right
            // #2i+2: next left
            // #2i+3: next right
            vh.AddTriangle(2 * i + 1, 2 * i, 2 * i + 2);
            vh.AddTriangle(2 * i + 3, 2 * i + 1, 2 * i + 2);
        }
    }

    private Vector2 ProjectUvOnTexture(float u, float v)
    {
        Rect rect = sprite.rect;
        u = Mathf.Lerp(rect.xMin, rect.xMax, u);
        v = Mathf.Lerp(rect.yMin, rect.yMax, v);
        return new Vector2(
            Mathf.InverseLerp(0f, sprite.texture.width, u),
            Mathf.InverseLerp(0f, sprite.texture.height, v));
    }

    public override void Cull(Rect clipRect, bool validRect)
    {
        // For simplicity, never cull this image.
        canvasRenderer.cull = false;
    }
}
