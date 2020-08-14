using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXSpawner : MonoBehaviour
{
    public GameObject explosionBig;
    public GameObject explosionMedium;
    public GameObject explosionSmall;

    private void SpawnPrefabAt(GameObject prefab, NoteObject note)
    {
        Transform noteOriginalParent = note.transform.parent;
        note.transform.parent = transform;
        float size = Scan.laneHeight * 3f;

        RectTransform rect = Instantiate(prefab, transform)
            .GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = note.GetComponent<RectTransform>().anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);

        note.transform.parent = noteOriginalParent;
    }

    public void SpawnExplosionBigAt(NoteObject note)
    {
        SpawnPrefabAt(explosionBig, note);
    }

    public void SpawnExplosionMediumAt(NoteObject note)
    {
        SpawnPrefabAt(explosionMedium, note);
    }

    public void SpawnExplosionSmallAt(NoteObject note)
    {
        SpawnPrefabAt(explosionSmall, note);
    }
}
