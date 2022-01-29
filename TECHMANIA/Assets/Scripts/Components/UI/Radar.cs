using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Radar : MonoBehaviour
{
    public Transform frame;
    public Color skeletonColor;
    public Color frameColor;
    public RadarGraph graph;
    public float size;

    [Header("Value display")]
    public TextMeshProUGUI density;
    public TextMeshProUGUI peak;
    public TextMeshProUGUI speed;
    public TextMeshProUGUI chaos;
    public TextMeshProUGUI async;

    // Start is called before the first frame update
    void Start()
    {
        int child = 0;
        // Skeleton
        for (int i = 0; i < 5; i++)
        {
            RectTransform rect = frame.GetChild(child)
                .GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(
                size * 5f,
                rect.sizeDelta.y);
            rect.localRotation = Quaternion.Euler(0f, 0f,
                90f + 72f * i);
            rect.GetComponent<Image>().color = skeletonColor;

            child++;
        }

        // Pentagons
        float unitSideLength = size * 5f * Mathf.Sin(
            36f * Mathf.Deg2Rad) * 2f * 0.2f;
        for (int i = 0; i < 5; i++)
        {
            float sideLength = unitSideLength * (i + 1);
            for (int side = 0; side < 5; side++)
            {
                float angleInDegree = 90f + 72f * side;
                float angleInRadian = angleInDegree * Mathf.Deg2Rad;
                RectTransform rect = frame.GetChild(child)
                    .GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    Mathf.Cos(angleInRadian),
                    Mathf.Sin(angleInRadian)
                    ) * size * (i + 1);
                rect.sizeDelta = new Vector2(
                    sideLength,
                    rect.sizeDelta.y);
                rect.localRotation = Quaternion.Euler(0f, 0f,
                    72f * side - 36f);
                rect.GetComponent<Image>().color =
                    (i == 4) ? skeletonColor : frameColor;

                child++;
            }
        }

        // Value display
        float displayDistance = size * 5f + 40f;
        Action<TextMeshProUGUI, float> placeDisplay =
            (TextMeshProUGUI display, float angleInDegree) =>
            {
                float angleInRadian = angleInDegree * Mathf.Deg2Rad;
                display.transform.parent.GetComponent<RectTransform>()
                    .anchoredPosition = new Vector2(
                    Mathf.Cos(angleInRadian),
                    Mathf.Sin(angleInRadian)) * displayDistance;
            };
        placeDisplay(density, 90f);
        placeDisplay(peak, 18f);
        placeDisplay(speed, -54f);
        placeDisplay(chaos, -126f);
        placeDisplay(async, 162f);
    }

    public void SetRadar(Pattern.Radar radar)
    {
        graph.radar = radar;
        graph.size = size * 5f;
        graph.SetVerticesDirty();

        density.text = radar.density.normalized.ToString();
        peak.text = radar.peak.normalized.ToString();
        speed.text = radar.speed.normalized.ToString();
        chaos.text = radar.chaos.normalized.ToString();
        async.text = radar.async.normalized.ToString();
    }

    public void SetEmpty()
    {
        Pattern.Radar defaultRadar = new Pattern.Radar();
        SetRadar(defaultRadar);
    }
}
