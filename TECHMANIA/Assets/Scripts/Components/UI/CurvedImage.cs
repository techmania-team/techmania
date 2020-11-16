using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NoteObject))]
public class CurvedImage : Image
{
    private void OnPopulateMeshBackup(VertexHelper vh)
    {
        Vector2 corner1 = Vector2.zero;
        Vector2 corner2 = Vector2.zero;

        corner1.x = 0f;
        corner1.y = 0f;
        corner2.x = 1f;
        corner2.y = 1f;

        corner1.x -= rectTransform.pivot.x;
        corner1.y -= rectTransform.pivot.y;
        corner2.x -= rectTransform.pivot.x;
        corner2.y -= rectTransform.pivot.y;

        corner1.x *= rectTransform.rect.width;
        corner1.y *= rectTransform.rect.height;
        corner2.x *= rectTransform.rect.width;
        corner2.y *= rectTransform.rect.height;

        vh.Clear();

        UIVertex vert = UIVertex.simpleVert;

        // Bottom left
        vert.position = new Vector2(corner1.x, corner1.y);
        vert.color = color;
        vert.uv0 = new Vector2(0f, 0f);
        vh.AddVert(vert);

        // Top left
        vert.position = new Vector2(corner1.x, corner2.y);
        vert.color = color;
        vert.uv0 = new Vector2(0f, 1f);
        vh.AddVert(vert);

        // Top right
        vert.position = new Vector2(corner2.x, corner2.y);
        vert.color = color;
        vert.uv0 = new Vector2(1f, 1f);
        vh.AddVert(vert);

        // Bottom right
        vert.position = new Vector2(corner2.x, corner1.y);
        vert.color = color;
        vert.uv0 = new Vector2(1f, 0f);
        vh.AddVert(vert);

        // Clockwise.
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        // Debug.Log("OnPopulateMesh called");
        vh.Clear();
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;
        if (dragNote == null)
        {
            return;
        }

        float curveWidth = rectTransform.rect.height;

        // TODO: replace with values from PatternPanel
        float LaneHeight = 100f;
        float ScanWidth = 800f;
        int bps = 4;

        // Convert (pulse, lane) to (x, y).
        float pulseWidth = ScanWidth / bps / Pattern.pulsesPerBeat;
        List<Vector2> pointsOnCurve = new List<Vector2>();
        foreach (FloatPoint p in dragNote.Interpolate())
        {
            Vector2 pointOnCurve = new Vector2(
                p.pulse * pulseWidth,
                p.lane * LaneHeight);
            pointsOnCurve.Add(pointOnCurve);
        }

        // TODO: smooth these points.
        
        // Calculate left vector on each point. Then generate
        // vertices.
        for (int i = 0; i < pointsOnCurve.Count; i++)
        {
            Vector2 forward = Vector2.zero;
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
            Vector2 left = new Vector2(-forward.y, forward.x);

            float u = (float)i / (pointsOnCurve.Count - 1);
            UIVertex vert = UIVertex.simpleVert;
            vert.position = pointsOnCurve[i] +
                curveWidth * 0.5f * left;
            vert.color = Color.white;
            vert.uv0 = new Vector2(u, 1f);
            vh.AddVert(vert);

            vert.position = pointsOnCurve[i] -
                curveWidth * 0.5f * left;
            vert.color = Color.white;
            vert.uv0 = new Vector2(u, 0f);
            vh.AddVert(vert);
        }

        // Triangles.
        for (int i = 0; i < pointsOnCurve.Count - 1; i++)
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
