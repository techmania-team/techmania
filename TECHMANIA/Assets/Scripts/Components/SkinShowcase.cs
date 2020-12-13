using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkinShowcase : MonoBehaviour
{
    public GameObject explosionMax;
    public GameObject explosionCool;
    public GameObject explosionGood;
    public GameObject repeatExplosion;
    public GameObject holdComplete;
    public GameObject repeatHoldComplete;
    public GameObject dragComplete;
    public RectTransform explosionMaxSpawnPoint;
    public RectTransform explosionCoolSpawnPoint;
    public RectTransform explosionGoodSpawnPoint;
    public RectTransform repeatHoldExplosionSpawnPoint;
    public RectTransform holdCompleteSpawnPoint;
    public RectTransform repeatHoldCompleteSpawnPoint;
    public RectTransform dragCompleteSpawnPoint;

    public List<Image> ongoingVfx;
    public List<GameObject> feverOverlay;

    private const float explosionSize = 300f;

    // Start is called before the first frame update
    void Start()
    {
        ToggleOngoingVfx(true);
        ToggleFeverOverlay(true);
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
        SpawnOneExplosion(repeatExplosion, 
            repeatHoldExplosionSpawnPoint);
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

    public void ToggleOngoingVfx(bool visible)
    {
        foreach (Image i in ongoingVfx)
        {
            i.enabled = visible;
        }
    }

    public void ToggleFeverOverlay(bool visible)
    {
        foreach (GameObject o in feverOverlay)
        {
            o.GetComponent<Image>().color = visible ?
                Color.white :
                Color.clear;
            o.GetComponent<Animator>().enabled = visible;
        }
    }
}
