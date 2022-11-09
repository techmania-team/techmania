using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This would be called VFXRenderer, but apparently
// "Script 'VFXRenderer' has the same name as built-in Unity component."
//
// What the heck.
public class VFXDrawer : MonoBehaviour
{
    public Material additiveMaterial;

    private SpriteSheet spriteSheet;
    private Vector3 position;
    private float laneHeight;
    private bool loop;

    private RectTransform rect;
    private Image image;
    private float startTime;

    public void Initialize(Vector3 position,
        SpriteSheet spriteSheet, float laneHeight, bool loop)
    {
        this.spriteSheet = spriteSheet;
        this.position = position;
        this.laneHeight = laneHeight;
        this.loop = loop;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (spriteSheet.sprites == null ||
            spriteSheet.sprites.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        rect = GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        float height = laneHeight * spriteSheet.scale;
        float width = spriteSheet.sprites[0].rect.width /
            spriteSheet.sprites[0].rect.height * height;
        rect.sizeDelta = new Vector2(width, height);

        image = GetComponent<Image>();
        image.sprite = spriteSheet.sprites[0];
        if (spriteSheet.additiveShader)
        {
            image.material = additiveMaterial;
        }

        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float time = Time.time - startTime;
        Sprite sprite = spriteSheet.GetSpriteForTime(time, loop);
        if (sprite == null)
        {
            Destroy(gameObject);
        }
        else
        {
            image.sprite = sprite;
        }
    }
}
