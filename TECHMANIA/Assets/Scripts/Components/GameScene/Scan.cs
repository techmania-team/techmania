using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scan : MonoBehaviour
{
    [HideInInspector]
    public int scanNumber;

    public const float kSpaceBeforeScan = 0.15f;
    public const float kSpaceAfterScan = 0.1f;

    private void OnDestroy()
    {
        Game.ScanChanged -= OnScanChanged;
    }

    public void Initialize()
    {
        Game.ScanChanged += OnScanChanged;

        Rect rect = GetComponent<RectTransform>().rect;
        Scanline scanline = GetComponentInChildren<Scanline>();
        scanline.scanNumber = scanNumber;
        scanline.Initialize(rect.width, rect.height);
    }

    public void SpawnNoteObject(GameObject prefab, Note n, string sound)
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnScanChanged(int scan)
    {

    }
}
