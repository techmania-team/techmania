using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageSelfLoader : MonoBehaviour
{
    private Image image;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadImage(string fullPath)
    {
        StartCoroutine(InnerLoadImage(fullPath));
    }

    private IEnumerator InnerLoadImage(string fullPath)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(
            Paths.FilePathToUri(fullPath), nonReadable: true);
        yield return request.SendWebRequest();
        
        Texture texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
        {
            Debug.LogError($"Could not load {fullPath}. Details: {request.error}");
            yield break;
        }
        Texture2D t2d = texture as Texture2D;
        if (t2d == null)
        {
            Debug.LogError($"{fullPath} did not load as Texture2D.");
        }

        int width = t2d.width;
        int height = t2d.height;
        Sprite sprite = Sprite.Create(t2d,
            new Rect(0f, 0f, width, height),
            new Vector2(width * 0.5f, height * 0.5f));
        GetComponent<Image>().sprite = sprite;
    }
}
