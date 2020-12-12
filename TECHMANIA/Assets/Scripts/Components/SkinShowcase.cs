using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinShowcase : MonoBehaviour
{
    public GameObject explosionMax;
    public GameObject explosionCool;
    public GameObject explosionGood;
    public GameObject holdComplete;
    public GameObject repeatHoldComplete;
    public GameObject dragComplete;
    public RectTransform explosionMaxSpawnPoint;
    public RectTransform explosionCoolSpawnPoint;
    public RectTransform explosionGoodSpawnPoint;
    public RectTransform holdCompleteSpawnPoint;
    public RectTransform repeatHoldCompleteSpawnPoint;
    public RectTransform dragCompleteSpawnPoint;

    private const float explosionSize = 300f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SpawnExplosions()
    {
        SpawnOneExplosion(explosionMax, explosionMaxSpawnPoint);
        SpawnOneExplosion(explosionCool, explosionCoolSpawnPoint);
        SpawnOneExplosion(explosionGood, explosionGoodSpawnPoint);
        SpawnOneExplosion(holdComplete, holdCompleteSpawnPoint);
        SpawnOneExplosion(repeatHoldComplete, 
            repeatHoldCompleteSpawnPoint);
        SpawnOneExplosion(dragComplete, dragCompleteSpawnPoint);
    }

    private void SpawnOneExplosion(GameObject prefab,
        RectTransform spawnPoint)
    {
        GameObject vfx = Instantiate(prefab, transform);
        RectTransform rect = vfx.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(explosionSize, explosionSize);
        rect.position = spawnPoint.position;
    }
}
