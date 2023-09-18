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
    private Vector2 anchor;
    private float laneHeight;
    private bool loop;

    private RectTransform rect;
    private Image image;
    private float startTime;

    public void Initialize(Vector2 viewportPoint,
        SpriteSheet spriteSheet, float laneHeight, bool loop)
    {
        this.spriteSheet = spriteSheet;
        this.anchor = viewportPoint;
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
        SetViewportPoint(anchor);

        // To calculate the proper size, first calculate in
        // UI Toolkit's world space. laneHeight comes from layout's
        // resolved style, so it should take scale into account.
        float height = laneHeight * spriteSheet.scale;
        float width = spriteSheet.sprites[0].rect.width /
            spriteSheet.sprites[0].rect.height * height;

        // Then normalize the sizes.
        UnityEngine.UIElements.VisualElement root = 
            TopLevelObjects.instance.mainUiDocument.rootVisualElement;
        float rootHeight = root.contentRect.height;
        float rootWidth = root.contentRect.width;
        height /= rootHeight;
        width /= rootWidth;

        // Finally, multiply by canvas size.
        RectTransform canvasRect = 
            TopLevelObjects.instance.vfxComboCanvas
            .GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width * canvasRect.sizeDelta.x,
            height * canvasRect.sizeDelta.y);

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

    public void SetViewportPoint(Vector2 viewportPoint)
    {
        if (rect == null) return;
        rect.anchorMin = viewportPoint;
        rect.anchorMax = viewportPoint;
    }
}
