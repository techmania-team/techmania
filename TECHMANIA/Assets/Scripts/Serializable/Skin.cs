using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[MoonSharpUserData]
public class SpriteSheet
{
    public string filename;
    public int rows;
    public int columns;
    public int firstIndex;
    public int lastIndex;
    public int padding;
    public bool bilinearFilter;

    // Not used by all skins

    public float scale;  // Relative to 1x lane height
    public float speed;  // Relative to 60 fps
    public bool additiveShader;
    public bool flipWhenScanningLeft;

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
        padding = 0;
        bilinearFilter = true;

        scale = 1f;
        speed = 1f;
        additiveShader = false;
        flipWhenScanningLeft = false;
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
        texture.wrapMode = TextureWrapMode.Clamp;
        sprites = new List<Sprite>();
        float spriteWidth = (float)texture.width / columns;
        float spriteHeight = (float)texture.height / rows;
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            int row = i / columns;
            // Unity thinks (0, 0) is bottom left but we think
            // (0, 0) is top left. So we inverse y here.
            int inverseRow = rows - 1 - row;
            int column = i % columns;

            float rectX = column * spriteWidth + padding;
            float rectY = inverseRow * spriteHeight + padding;
            float rectWidth = spriteWidth - padding * 2;
            float rectHeight = spriteHeight - padding * 2;
            // In rare cases, floating number division may cause
            // sprite rects to go very slightly out of bounds of
            // the texture, causing Unity to freak out.
            rectWidth = Mathf.Min(rectWidth,
                texture.width - rectX);
            rectHeight = Mathf.Min(rectHeight,
                texture.height - rectY);
            Sprite s = Sprite.Create(texture,
                new Rect(rectX, rectY,
                    rectWidth, rectHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f,
                extrude: 0,
                // The default is Tight, whose performance is
                // atrocious.
                meshType: SpriteMeshType.FullRect);
            sprites.Add(s);
        }
    }

    // For animations that cycle once per beat/scan, pass in
    // the float beat/scan number.
    // Integer part of the input number is removed.
    public Sprite GetSpriteAtFloatIndex(float floatIndex)
    {
        if (sprites == null) return null;
        floatIndex = floatIndex - Mathf.Floor(floatIndex);
        int index = Mathf.FloorToInt(floatIndex * sprites.Count);
        index = Mathf.Clamp(index, 0, sprites.Count - 1);
        return sprites[index];
    }

    // For animations that cycle on a fixed time. Relies on speed.
    // Returns null if the end of animation is reached.
    public Sprite GetSpriteForTime(float time, bool loop)
    {
        if (sprites == null) return null;
        float fps = 60f * speed;
        int index = Mathf.FloorToInt(time * fps);
        if (loop)
        {
            while (index < 0) index += sprites.Count;
            index = index % sprites.Count;
        }
        if (index < 0 || index >= sprites.Count) return null;
        return sprites[index];
    }

    #region Empty sprite sheet
    // Used in place of null sprite sheets when a skin is missing
    // items.
    public static Texture2D emptyTexture;
    public static void PrepareEmptySpriteSheet()
    {
        emptyTexture = new Texture2D(1, 1);
        emptyTexture.SetPixel(0, 0, Color.clear);
        emptyTexture.Apply();
    }

    public void MakeEmpty()
    {
        texture = emptyTexture;
        GenerateSprites();
    }

    public static SpriteSheet MakeNewEmptySpriteSheet()
    {
        SpriteSheet s = new SpriteSheet();
        s.MakeEmpty();
        return s;
    }
    #endregion
}

[Serializable]
[MoonSharpUserData]
public class SkinAnimationKeyframe
{
    // All values default to 0.

    public float time;
    public float value;
    public float inTangent;
    public float outTangent;
    public float inWeight;
    public float outWeight;
    public int weightedMode;

    public Keyframe ToUnityKeyframe()
    {
        return new Keyframe(time, value, inTangent, outTangent)
        {
            inWeight = inWeight,
            outWeight = outWeight,
            weightedMode = (WeightedMode)weightedMode
        };
    }
}

[Serializable]
[MoonSharpUserData]
public class SkinAnimationCurve
{
    public List<SkinAnimationKeyframe> keys;

    // Which attribute this curve controls. Possible values:
    // translationX
    // translationY
    // rotationInDegrees
    // scaleX
    // scaleY
    // alpha
    public string attribute;

    // "once" (default)
    // "pingpong"
    // "loop"
    public string loopMode;

    public SkinAnimationCurve()
    {
        keys = new List<SkinAnimationKeyframe>();
    }

    public void AddKeyframe(float time, float value,
        float inTangent = 0f, float outTangent = 0f)
    {
        keys.Add(new SkinAnimationKeyframe()
        {
            time = time,
            value = value,
            inTangent = inTangent,
            outTangent = outTangent
        });
    }

    public Tuple<AnimationCurve, string> ToUnityCurveAndAttribute()
    {
        AnimationCurve curve = new AnimationCurve();
        foreach (SkinAnimationKeyframe k in keys)
        {
            curve.AddKey(k.ToUnityKeyframe());
        }
        switch (loopMode)
        {
            case "pingpong":
                curve.postWrapMode = WrapMode.PingPong;
                break;
            case "loop":
                curve.postWrapMode = WrapMode.Loop;
                break;
            default:
                curve.postWrapMode = WrapMode.Once;
                break;
        }
        return new Tuple<AnimationCurve, string>(curve, attribute);
    }
}

[Serializable]
[FormatVersion(NoteSkin.kVersion, typeof(NoteSkin), isLatest: true)]
public class NoteSkinBase : SerializableClass<NoteSkinBase> {}

// Most sprite sheets use scale, except for the "...end"s.
[Serializable]
[MoonSharpUserData]
public class NoteSkin : NoteSkinBase
{
    public const string kVersion = "1";
    public string author;

    // Note skin's name is the folder's name.

    public SpriteSheet basic;

    public SpriteSheet chainHead;
    public SpriteSheet chainNode;
    public SpriteSheet chainPath;

    public SpriteSheet dragHead;
    public SpriteSheet dragCurve;

    public SpriteSheet holdHead;
    public SpriteSheet holdTrail;
    public SpriteSheet holdTrailEnd;
    public SpriteSheet holdOngoingTrail;

    public SpriteSheet repeatHead;
    public SpriteSheet repeat;
    public SpriteSheet repeatHoldTrail;
    public SpriteSheet repeatHoldTrailEnd;
    public SpriteSheet repeatPath;
    public SpriteSheet repeatPathEnd;

    public NoteSkin()
    {
        version = kVersion;

        basic = new SpriteSheet();
        chainHead = new SpriteSheet();
        chainNode = new SpriteSheet();
        chainPath = new SpriteSheet();
        dragHead = new SpriteSheet();
        dragCurve = new SpriteSheet();
        holdHead = new SpriteSheet();
        holdTrail = new SpriteSheet();
        holdTrailEnd = new SpriteSheet();
        holdOngoingTrail = new SpriteSheet();
        repeatHead = new SpriteSheet();
        repeat = new SpriteSheet();
        repeatHoldTrail = new SpriteSheet();
        repeatHoldTrailEnd = new SpriteSheet();
        repeatPath = new SpriteSheet();
        repeatPathEnd = new SpriteSheet();
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

        list.Add(repeatHead);
        list.Add(repeat);
        list.Add(repeatHoldTrail);
        list.Add(repeatHoldTrailEnd);
        list.Add(repeatPath);
        list.Add(repeatPathEnd);

        return list;
    }
}

[Serializable]
[FormatVersion(VfxSkin.kVersion, typeof(VfxSkin), isLatest: true)]
public class VfxSkinBase : SerializableClass<VfxSkinBase> { }

// All sprite sheets use scale, speed and additiveShader.
[Serializable]
[MoonSharpUserData]
public class VfxSkin : VfxSkinBase
{
    public const string kVersion = "1";
    public string author;

    // VFX skin's name is the folder's name.
    // Each VFX (except for feverOverlay) is defined as multiple
    // layers of sprite sheets, each element in List corresponding
    // to one layer.

    public SpriteSheet feverOverlay;

    public List<SpriteSheet> basicMax;
    public List<SpriteSheet> basicCool;
    public List<SpriteSheet> basicGood;

    public List<SpriteSheet> dragOngoing;
    public List<SpriteSheet> dragComplete;

    public List<SpriteSheet> holdOngoingHead;
    public List<SpriteSheet> holdOngoingTrail;
    public List<SpriteSheet> holdComplete;

    public List<SpriteSheet> repeatHead;
    public List<SpriteSheet> repeatNote;
    public List<SpriteSheet> repeatHoldOngoingHead;
    public List<SpriteSheet> repeatHoldOngoingTrail;
    public List<SpriteSheet> repeatHoldComplete;

    public VfxSkin()
    {
        version = kVersion;

        feverOverlay = new SpriteSheet();

        basicMax = new List<SpriteSheet>();
        basicCool = new List<SpriteSheet>();
        basicGood = new List<SpriteSheet>();
        dragOngoing = new List<SpriteSheet>();
        dragComplete = new List<SpriteSheet>();
        holdOngoingHead = new List<SpriteSheet>();
        holdOngoingTrail = new List<SpriteSheet>();
        holdComplete = new List<SpriteSheet>();
        repeatHead = new List<SpriteSheet>();
        repeatNote = new List<SpriteSheet>();
        repeatHoldOngoingHead = new List<SpriteSheet>();
        repeatHoldOngoingTrail = new List<SpriteSheet>();
        repeatHoldComplete = new List<SpriteSheet>();
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
public class ComboSkinBase : SerializableClass<ComboSkinBase> { }

// All sprite sheets use speed.
[Serializable]
[MoonSharpUserData]
public class ComboSkin : ComboSkinBase
{
    public const string kVersion = "1";
    public string author;

    // Combo skin's name is the folder's name.

    public float distanceToNote;  // In pixels
    public float height;  // In pixels
    public float spaceBetweenJudgementAndCombo;  // In pixels

    public SpriteSheet feverMaxJudgement;
    public SpriteSheet rainbowMaxJudgement;
    public SpriteSheet maxJudgement;
    public SpriteSheet coolJudgement;
    public SpriteSheet goodJudgement;
    public SpriteSheet missJudgement;
    public SpriteSheet breakJudgement;

    public List<SpriteSheet> feverMaxDigits;
    public List<SpriteSheet> rainbowMaxDigits;
    public List<SpriteSheet> maxDigits;
    public List<SpriteSheet> coolDigits;
    public List<SpriteSheet> goodDigits;

    public List<SkinAnimationCurve> animationCurves;

    public ComboSkin()
    {
        version = kVersion;

        feverMaxJudgement = new SpriteSheet();
        rainbowMaxJudgement = new SpriteSheet();
        maxJudgement = new SpriteSheet();
        coolJudgement = new SpriteSheet();
        goodJudgement = new SpriteSheet();
        missJudgement = new SpriteSheet();
        breakJudgement = new SpriteSheet();

        feverMaxDigits = new List<SpriteSheet>();
        rainbowMaxDigits = new List<SpriteSheet>();
        maxDigits = new List<SpriteSheet>();
        coolDigits = new List<SpriteSheet>();
        goodDigits = new List<SpriteSheet>();

        for (int i = 0; i < 10; i++)
        {
            feverMaxDigits.Add(new SpriteSheet());
            rainbowMaxDigits.Add(new SpriteSheet());
            maxDigits.Add(new SpriteSheet());
            coolDigits.Add(new SpriteSheet());
            goodDigits.Add(new SpriteSheet());
        }

        animationCurves = new List<SkinAnimationCurve>();

        // Default curves

        SkinAnimationCurve scaleXCurve = new SkinAnimationCurve()
        {
            attribute = "scaleX"
        };
        scaleXCurve.AddKeyframe(0f, 1.2f);
        scaleXCurve.AddKeyframe(0.1f, 0.8f);
        scaleXCurve.AddKeyframe(0.133f, 1.1f);
        scaleXCurve.AddKeyframe(0.167f, 1f);
        scaleXCurve.AddKeyframe(1f, 1f);
        animationCurves.Add(scaleXCurve);

        SkinAnimationCurve scaleYCurve = new SkinAnimationCurve()
        {
            attribute = "scaleY"
        };
        scaleYCurve.AddKeyframe(0f, 1.2f);
        scaleYCurve.AddKeyframe(0.1f, 0.8f);
        scaleYCurve.AddKeyframe(0.133f, 1.1f);
        scaleYCurve.AddKeyframe(0.167f, 1f);
        scaleYCurve.AddKeyframe(0.833f, 1f);
        scaleYCurve.AddKeyframe(1f, 2f);
        animationCurves.Add(scaleYCurve);

        SkinAnimationCurve alphaCurve = new SkinAnimationCurve()
        {
            attribute = "alpha"
        };
        alphaCurve.AddKeyframe(0f, 0.5f, inTangent: 8f);
        alphaCurve.AddKeyframe(0.1f, 1f);
        alphaCurve.AddKeyframe(0.833f, 1f);
        alphaCurve.AddKeyframe(1f, 0f);
        animationCurves.Add(alphaCurve);
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

    public List<List<SpriteSheet>> GetReferenceToDigitLists()
    {
        List<List<SpriteSheet>> list = new List<List<SpriteSheet>>();
        list.Add(feverMaxDigits);
        list.Add(rainbowMaxDigits);
        list.Add(maxDigits);
        list.Add(coolDigits);
        list.Add(goodDigits);
        return list;
    }
}

[Serializable]
[FormatVersion(GameUISkin.kVersion, typeof(GameUISkin),
    isLatest: true)]
public class GameUISkinBase : SerializableClass<GameUISkinBase> { }

[Serializable]
[MoonSharpUserData]
public class GameUISkin : GameUISkinBase
{
    public const string kVersion = "1";
    public string author;

    // Scanline animations play one cycle per beat.
    public SpriteSheet scanline;
    public SpriteSheet autoPlayScanline;

    // Plays through the last 3 beats of every scan (or last 3
    // half-beats or quarter-beats, if bps is low).
    // Background is flipped for right-to-left scans, number is not.
    // These two sprite sheets use additiveShader.
    // If scanCountdownCoversFiveEighthScans is true, countdown
    // covers 5/8 scans instead of 3 beats.
    public SpriteSheet scanCountdownBackground;
    public SpriteSheet scanCountdownNumbers;
    public bool scanCountdownCoversFiveEighthScans;

    // Uses speed and additiveShader.
    public SpriteSheet touchClickFeedback;
    public float touchClickFeedbackSize;  // In pixels

    // Uses scale.
    public SpriteSheet approachOverlay;

    public GameUISkin()
    {
        version = kVersion;

        scanline = new SpriteSheet();
        autoPlayScanline = new SpriteSheet();
        scanCountdownBackground = new SpriteSheet();
        scanCountdownNumbers = new SpriteSheet();
        touchClickFeedback = new SpriteSheet();
        approachOverlay = new SpriteSheet();
    }

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(scanline);
        list.Add(autoPlayScanline);
        list.Add(scanCountdownBackground);
        list.Add(scanCountdownNumbers);
        list.Add(touchClickFeedback);
        list.Add(approachOverlay);

        return list;
    }
}