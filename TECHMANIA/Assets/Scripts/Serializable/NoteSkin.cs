using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpriteSheet
{
    public string filename;
    public int rows;
    public int columns;
    public int firstIndex;
    public int lastIndex;
    public bool bilinearFilter;

    [NonSerialized]  // Loaded at runtime
    public Texture2D texture;
    [NonSerialized]
    public List<Sprite> sprites;

    public SpriteSheet()
    {
        rows = 1;
        columns = 1;
        firstIndex = 0;
        lastIndex = 0;
        bilinearFilter = true;
    }

    // Call after loading texture.
    public void GenerateSprites()
    {
        if (texture == null)
        {
            throw new Exception("Texture not yet loaded.");
        }
        texture.filterMode = bilinearFilter ? FilterMode.Bilinear :
            FilterMode.Point;
        sprites = new List<Sprite>();
        int spriteWidth = texture.width / columns;
        int spriteHeight = texture.height / rows;
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            int row = i / columns;
            // Unity thinks (0, 0) is bottom left but we think
            // (0, 0) is top left. So we inverse y here.
            int inverseRow = rows - 1 - row;
            int column = i % columns;
            Sprite s = Sprite.Create(texture,
                new Rect(column * spriteWidth,
                    inverseRow * spriteHeight,
                    spriteWidth,
                    spriteHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f,
                extrude: 0,
                // The default is Tight, whose performance is
                // atrocious.
                meshType: SpriteMeshType.FullRect);
            sprites.Add(s);
        }
    }

    public Sprite GetSpriteForFloatBeat(float beat)
    {
        beat = beat - Mathf.Floor(beat);
        int index = Mathf.FloorToInt(beat * sprites.Count);
        index = Mathf.Clamp(index, 0, sprites.Count - 1);
        return sprites[index];
    }
}

[Serializable]
public class SpriteSheetForNote : SpriteSheet
{
    public float scale;  // Relative to 1x lane height

    public SpriteSheetForNote() : base()
    {
        scale = 1f;
    }
}

[Serializable]
public class SpriteSheetForCombo : SpriteSheet
{
    public float speed;  // Relative to 60 fps
    public SpriteSheetForCombo() : base()
    {
        speed = 1f;
    }

    // Returns null if the end of animation is reached.
    public Sprite GetSpriteForTime(float time, bool loop)
    {
        float fps = 60f * speed;
        int index = Mathf.FloorToInt(time * fps);
        if (loop)
        {
            index = index % sprites.Count;
        }
        if (index < 0 || index >= sprites.Count) return null;
        return sprites[index];
    }
}

[Serializable]
public class SpriteSheetForVfx : SpriteSheetForCombo
{
    public float scale;  // Relative to 1x lane height
    public bool additiveShader;

    public SpriteSheetForVfx() : base()
    {
        scale = 1f;
        additiveShader = false;
    }
}

[Serializable]
[FormatVersion(NoteSkin.kVersion, typeof(NoteSkin), isLatest: true)]
public class NoteSkinBase : Serializable<NoteSkinBase> {}

[Serializable]
public class NoteSkin : NoteSkinBase
{
    public const string kVersion = "1";

    // Note skin's name is the folder's name.

    public SpriteSheetForNote basic;

    public SpriteSheetForNote chainHead;
    public SpriteSheetForNote chainNode;
    public SpriteSheetForNote chainPath;

    public SpriteSheetForNote dragHead;
    public SpriteSheetForNote dragCurve;

    public SpriteSheetForNote holdHead;
    public SpriteSheetForNote holdTrail;
    public SpriteSheet holdTrailEnd;
    public SpriteSheetForNote holdOngoingTrail;
    public SpriteSheet holdOngoingTrailEnd;

    public SpriteSheetForNote repeatHead;
    public SpriteSheetForNote repeat;
    public SpriteSheetForNote repeatHoldTrail;
    public SpriteSheet repeatHoldTrailEnd;
    public SpriteSheetForNote repeatPath;

    public NoteSkin()
    {
        version = kVersion;
    }

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(basic);

        list.Add(chainHead);
        list.Add(chainNode);
        list.Add(chainPath);

        list.Add(dragHead);
        list.Add(dragCurve);

        list.Add(holdHead);
        list.Add(holdTrail);
        list.Add(holdTrailEnd);
        list.Add(holdOngoingTrail);
        list.Add(holdOngoingTrailEnd);

        list.Add(repeatHead);
        list.Add(repeat);
        list.Add(repeatHoldTrail);
        list.Add(repeatHoldTrailEnd);
        list.Add(repeatPath);

        return list;
    }
}

[Serializable]
[FormatVersion(VfxSkin.kVersion, typeof(VfxSkin), isLatest: true)]
public class VfxSkinBase : Serializable<VfxSkinBase> { }

[Serializable]
public class VfxSkin : VfxSkinBase
{
    public const string kVersion = "1";

    // VFX skin's name is the folder's name.
    // Each VFX (except for feverOverlay) is defined as multiple
    // layers of sprite sheets, each element in List corresponding
    // to one layer.

    public SpriteSheetForVfx feverOverlay;

    public List<SpriteSheetForVfx> basicMax;
    public List<SpriteSheetForVfx> basicCool;
    public List<SpriteSheetForVfx> basicGood;

    public List<SpriteSheetForVfx> dragOngoing;
    public List<SpriteSheetForVfx> dragComplete;

    public List<SpriteSheetForVfx> holdOngoingHead;
    public List<SpriteSheetForVfx> holdOngoingTrail;
    public List<SpriteSheetForVfx> holdComplete;

    public List<SpriteSheetForVfx> repeatHead;
    public List<SpriteSheetForVfx> repeatNote;
    public List<SpriteSheetForVfx> repeatHoldOngoingHead;
    public List<SpriteSheetForVfx> repeatHoldOngoingTrail;
    public List<SpriteSheetForVfx> repeatHoldComplete;

    public VfxSkin()
    {
        version = kVersion;
    }

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(feverOverlay);

        list.AddRange(basicMax);
        list.AddRange(basicCool);
        list.AddRange(basicGood);

        list.AddRange(dragOngoing);
        list.AddRange(dragComplete);

        list.AddRange(holdOngoingHead);
        list.AddRange(holdOngoingTrail);
        list.AddRange(holdComplete);

        list.AddRange(repeatHead);
        list.AddRange(repeatNote);
        list.AddRange(repeatHoldOngoingHead);
        list.AddRange(repeatHoldOngoingTrail);
        list.AddRange(repeatHoldComplete);

        return list;
    }
}

[Serializable]
[FormatVersion(ComboSkin.kVersion, typeof(ComboSkin), isLatest: true)]
public class ComboSkinBase : Serializable<ComboSkinBase> { }

[Serializable]
public class ComboSkin : ComboSkinBase
{
    public const string kVersion = "1";

    // Combo skin's name is the folder's name.

    public float distanceToNote;  // In pixels
    public float height;  // In pixels
    public float spaceBetweenJudgementAndCombo;  // In pixels

    public SpriteSheetForCombo feverMaxJudgement;
    public SpriteSheetForCombo rainbowMaxJudgement;
    public SpriteSheetForCombo maxJudgement;
    public SpriteSheetForCombo coolJudgement;
    public SpriteSheetForCombo goodJudgement;
    public SpriteSheetForCombo missJudgement;
    public SpriteSheetForCombo breakJudgement;

    public List<SpriteSheetForCombo> feverMaxDigits;
    public List<SpriteSheetForCombo> rainbowMaxDigits;
    public List<SpriteSheetForCombo> maxDigits;
    public List<SpriteSheetForCombo> coolDigits;
    public List<SpriteSheetForCombo> goodDigits;

    public ComboSkin()
    {
        version = kVersion;
    }

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(feverMaxJudgement);
        list.Add(rainbowMaxJudgement);
        list.Add(maxJudgement);
        list.Add(coolJudgement);
        list.Add(goodJudgement);
        list.Add(missJudgement);
        list.Add(breakJudgement);

        feverMaxDigits.ForEach(s => list.Add(s));
        rainbowMaxDigits.ForEach(s => list.Add(s));
        maxDigits.ForEach(s => list.Add(s));
        coolDigits.ForEach(s => list.Add(s));
        goodDigits.ForEach(s => list.Add(s));

        return list;
    }
}