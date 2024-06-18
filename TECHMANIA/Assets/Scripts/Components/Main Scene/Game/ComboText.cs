using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ThemeApi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboText : MonoBehaviour
{
    public RectTransform comboTextLayout;
    public UnityEngine.UI.Image judgementText;
    public RectTransform space;
    public List<UnityEngine.UI.Image> comboDigits;

    private UnityEngine.UIElements.VisualElement elementToFollow;
    private RectTransform rect;
    private SpriteSheet judgementSpriteSheet;
    private List<SpriteSheet> comboDigitSpriteSheet;
    private float startTime;
    private float sizeUnit;

    // Start is called before the first frame update
    void Start()
    {
        Hide();

        elementToFollow = null;
        rect = GetComponent<RectTransform>();
        judgementSpriteSheet = null;
        comboDigitSpriteSheet = new List<SpriteSheet>();
        foreach (UnityEngine.UI.Image i in comboDigits)
        {
            comboDigitSpriteSheet.Add(null);
        }
        startTime = 0f;
    }

    private float GetWidth(SpriteSheet spriteSheet)
    {
        if (spriteSheet.sprites.Count == 0) return 0f;
        float height = GlobalResource.comboSkin.height * sizeUnit;
        float ratio = spriteSheet.sprites[0].rect.height /
            spriteSheet.sprites[0].rect.width;
        return height / ratio;
    }

    public void ResetSize(float scanHeight)
    {
        if (GlobalResource.comboSkin == null) return;

        // Sizes in the combo skin are in unit
        // "1/500 of scan height".
        // To express this unit in Canvas terms, first normalize
        // scan height in UI Toolkit's world space.
        float worldHeight = TopLevelObjects.instance.mainUiDocument
            .rootVisualElement.contentRect.height;
        float normalizedScanHeight = scanHeight / worldHeight;

        // Now we can calculate size unit.
        sizeUnit = TopLevelObjects.instance.vfxComboCanvas
            .GetComponent<RectTransform>().sizeDelta.y
            * normalizedScanHeight / 500f;

        comboTextLayout.anchoredPosition = new Vector2(
            0f, GlobalResource.comboSkin.distanceToNote * sizeUnit);
        comboTextLayout.sizeDelta = new Vector2(
            0, GlobalResource.comboSkin.height * sizeUnit);
        space.sizeDelta = new Vector2(
            GlobalResource.comboSkin.spaceBetweenJudgementAndCombo
                * sizeUnit,
            0);
    }

    public void Hide()
    {
        elementToFollow = null;  // To stop Follow()
        judgementText.gameObject.SetActive(false);
        space.gameObject.SetActive(false);
        comboDigits.ForEach(i => i.gameObject.SetActive(false));
    }

    public void Show(UnityEngine.UIElements.VisualElement noteImage, 
        Judgement judgement, ScoreKeeper scoreKeeper)
    {
        Show(noteImage, judgement,
            scoreKeeper.feverState == ScoreKeeper.FeverState.Active,
            scoreKeeper.currentCombo);
    }

    public void Show(UnityEngine.UIElements.VisualElement noteImage, 
        Judgement judgement, bool fever, int combo)
    {
        if (noteImage != null)
        {
            elementToFollow = noteImage;
        }
        Follow();

        // Draw judgement.

        List<SpriteSheet> comboDigitSpriteSheetList = null;
        if (fever &&
            (judgement == Judgement.RainbowMax ||
             judgement == Judgement.Max ||
             judgement == Judgement.Cool))
        {
            judgementSpriteSheet = GlobalResource.comboSkin
                .feverMaxJudgement;
            comboDigitSpriteSheetList = GlobalResource.comboSkin
                .feverMaxDigits;
        }
        else
        {
            switch (judgement)
            {
                case Judgement.RainbowMax:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .rainbowMaxJudgement;
                    comboDigitSpriteSheetList =
                        GlobalResource.comboSkin
                        .rainbowMaxDigits;
                    break;
                case Judgement.Max:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .maxJudgement;
                    comboDigitSpriteSheetList =
                        GlobalResource.comboSkin
                        .maxDigits;
                    break;
                case Judgement.Cool:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .coolJudgement;
                    comboDigitSpriteSheetList =
                        GlobalResource.comboSkin
                        .coolDigits;
                    break;
                case Judgement.Good:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .goodJudgement;
                    comboDigitSpriteSheetList =
                        GlobalResource.comboSkin
                        .goodDigits;
                    break;
                case Judgement.Miss:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .missJudgement;
                    break;
                case Judgement.Break:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .breakJudgement;
                    break;
            }
        }
        judgementText.GetComponent<RectTransform>().sizeDelta =
            new Vector2(GetWidth(judgementSpriteSheet),
                0f);
        judgementText.gameObject.SetActive(true);

        // Draw combo, if applicable.

        if (judgement != Judgement.Miss &&
            judgement != Judgement.Break &&
            combo > 0)
        {
            space.gameObject.SetActive(true);

            List<int> digits = new List<int>();
            int remainingCombo = combo;
            for (int i = 0; i < comboDigits.Count; i++)
            {
                digits.Insert(0, remainingCombo % 10);
                remainingCombo /= 10;
            }
            for (int i = 0; i < comboDigits.Count; i++)
            {
                comboDigitSpriteSheet[i] =
                    comboDigitSpriteSheetList[digits[i]];
            }

            // Turn off the left-most 0 digits.
            comboDigits.ForEach(i => i.gameObject.SetActive(true));
            for (int i = 0; i < comboDigits.Count; i++)
            {
                if (digits[i] == 0)
                {
                    comboDigits[i].gameObject.SetActive(false);
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < comboDigits.Count; i++)
            {
                if (!comboDigits[i].gameObject.activeSelf) continue;
                comboDigits[i].GetComponent<RectTransform>()
                    .sizeDelta = new Vector2(
                        GetWidth(comboDigitSpriteSheet[i]),
                        0f);
            }
        }
        else
        {
            space.gameObject.SetActive(false);
            comboDigits.ForEach(i => i.gameObject.SetActive(false));
        }

        startTime = Time.time;
        ResetAllAnimationAttributes();
        UpdateAnimationCurves();
        UpdateSprites();
    }

    private void UpdateSprites()
    {
        float time = Time.time - startTime;

        if (judgementText.gameObject.activeSelf)
        {
            judgementText.sprite = judgementSpriteSheet
                .GetSpriteForTime(time, loop: true);
        }
        for (int i = 0; i < comboDigits.Count; i++)
        {
            if (comboDigits[i].gameObject.activeSelf)
            {
                comboDigits[i].sprite = comboDigitSpriteSheet[i]
                    .GetSpriteForTime(time, loop: true);
            }
        }
    }

    private void Update()
    {
        if (elementToFollow != null)
        {
            Follow();
            UpdateAnimationCurves();
            UpdateSprites();
        }
    }

    private void Follow()
    {
        if (elementToFollow == null) return;

        Vector2 viewportPoint = VisualElementTransform
            .ElementCenterToViewportSpace(elementToFollow);
        rect.anchorMin = viewportPoint;
        rect.anchorMax = viewportPoint;
    }

    #region Animation
    private void SetTranslationX(float value)
    {
        comboTextLayout.anchoredPosition = new Vector2(
            value * sizeUnit,
            comboTextLayout.anchoredPosition.y);
    }

    private void SetTranslationY(float value)
    {
        comboTextLayout.anchoredPosition = new Vector2(
            comboTextLayout.anchoredPosition.x,
            GlobalResource.comboSkin.distanceToNote * sizeUnit + value);
    }

    private void SetRotationInDegrees(float value)
    {
        comboTextLayout.localRotation = Quaternion.Euler(
            0f, 0f, value);
    }

    private void SetScaleX(float value)
    {
        comboTextLayout.localScale = new Vector3(value,
            comboTextLayout.localScale.y, 1f);
    }

    private void SetScaleY(float value)
    {
        comboTextLayout.localScale = new Vector3(
            comboTextLayout.localScale.x, value, 1f);
    }

    private void SetAlpha(float value)
    {
        comboTextLayout.GetComponent<CanvasGroup>().alpha =
            value;
    }

    private void ResetAllAnimationAttributes()
    {
        SetTranslationX(0f);
        SetTranslationY(0f);
        SetRotationInDegrees(0f);
        SetScaleX(1f);
        SetScaleY(1f);
        SetAlpha(1f);
    }

    private void UpdateAnimationCurves()
    {
        float time = Time.time - startTime;

        foreach (Tuple<AnimationCurve, string> tuple in
            GlobalResource.comboAnimationCurvesAndAttributes)
        {
            AnimationCurve curve = tuple.Item1;
            string attribute = tuple.Item2;

            float value = curve.Evaluate(time);
            switch (attribute)
            {
                case "translationX":
                    SetTranslationX(value);
                    break;
                case "translationY":
                    SetTranslationY(value);
                    break;
                case "rotationInDegrees":
                    SetRotationInDegrees(value);
                    break;
                case "scaleX":
                    SetScaleX(value);
                    break;
                case "scaleY":
                    SetScaleY(value);
                    break;
                case "alpha":
                    SetAlpha(value);
                    break;
                default:
                    Debug.LogWarning("Unknown attribute in combo animation: " + attribute);
                    break;
            }
        }
    }

    #endregion
}
