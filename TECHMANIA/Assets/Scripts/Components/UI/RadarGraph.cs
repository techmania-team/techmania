using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarGraph : Image
{
    [HideInInspector]
    public Pattern.Radar radar;
    [HideInInspector]
    public float size;

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear();

        UIVertex vert = UIVertex.simpleVert;
        vert.position = Vector2.zero;
        vert.color = color;
        toFill.AddVert(vert);

        Action<int, float> addPoint =
            (int normalized, float angleInDegree) =>
        {
            UIVertex vert = UIVertex.simpleVert;

            float distance = normalized * 0.01f * size;
            float angleInRadian = angleInDegree * Mathf.Deg2Rad;
            vert.position = new Vector2(
                Mathf.Cos(angleInRadian),
                Mathf.Sin(angleInRadian)
                ) * distance;
            vert.color = color;
            toFill.AddVert(vert);
        };
        addPoint(radar.density.normalized, 90f);
        addPoint(radar.async.normalized, 162f);
        addPoint(radar.chaos.normalized, 234f);
        addPoint(radar.speed.normalized, 306f);
        addPoint(radar.peak.normalized, 18f);

        for (int i = 0; i < 5; i++)
        {
            toFill.AddTriangle(i + 1, (i + 1) % 5 + 1, 0);
        }
    }
}
