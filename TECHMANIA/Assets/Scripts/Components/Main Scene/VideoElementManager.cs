using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoElementManager : MonoBehaviour
{
    public static VideoElementManager instance { get; private set; }
    public GameObject videoElementPrefab;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public static VideoPlayer InstantiatePlayer()
    {
        return Instantiate(
            instance.videoElementPrefab, instance.transform)
            .GetComponent<VideoPlayer>();
    }

    public static void DestroyPlayer(VideoPlayer player)
    {
        if (player.targetTexture != null &&
            player.targetTexture.IsCreated())
        {
            player.targetTexture.Release();
        }
        Destroy(player.gameObject);
    }
}
