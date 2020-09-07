using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EyecatchSelfLoader : MonoBehaviour
{
    public Image image;
    public GameObject progressIndicator;
    public GameObject noImageIndicator;

    private void NoImage()
    {
        image.gameObject.SetActive(false);
        progressIndicator.SetActive(false);
        noImageIndicator.SetActive(true);
    }

    public void LoadImage(string folder, TrackMetadata t)
    {
        if (t.eyecatchImage != UIUtils.kNone)
        {
            string fullPath = folder + "\\" + t.eyecatchImage;
            StartCoroutine(InnerLoadImage(fullPath));
        }
        else
        {
            NoImage();
        }
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
            NoImage();
            yield break;
        }
        Texture2D t2d = texture as Texture2D;
        if (t2d == null)
        {
            Debug.LogError($"{fullPath} did not load as Texture2D.");
            NoImage();
            yield break;
        }

        int width = t2d.width;
        int height = t2d.height;
        Sprite sprite = Sprite.Create(t2d,
            new Rect(0f, 0f, width, height),
            new Vector2(width * 0.5f, height * 0.5f));
        image.sprite = sprite;

        image.gameObject.SetActive(true);
        progressIndicator.SetActive(false);
        noImageIndicator.SetActive(false);
    }
}
