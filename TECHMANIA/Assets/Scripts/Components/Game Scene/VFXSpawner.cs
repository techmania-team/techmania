using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXSpawner : MonoBehaviour
{
    public GameObject explosionMax;
    public GameObject explosionCool;
    public GameObject explosionGood;
    public GameObject holdCompleted;

    private void SpawnPrefabAt(GameObject prefab, NoteObject note)
    {
        float size = Scan.laneHeight * 3f;

        RectTransform rect = Instantiate(prefab, transform)
            .GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.position = note.transform.position;
    }

    public void SpawnBasicOrChainExplosion(NoteObject note,
        Judgement judgement)
    {
        switch (judgement)
        {
            case Judgement.RainbowMax:
            case Judgement.Max:
                SpawnPrefabAt(explosionMax, note);
                break;
            case Judgement.Cool:
                SpawnPrefabAt(explosionCool, note);
                break;
            case Judgement.Good:
                SpawnPrefabAt(explosionGood, note);
                break;
        }
    }
}
