﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboText : MonoBehaviour
{
    public RectTransform comboTextLayout;
    public Image judgementText;
    public RectTransform space;
    public List<Image> comboDigits;
    public Animator animator;

    private Transform transformToFollow;
    private UnityEngine.UIElements.VisualElement elementToFollow;

    private RectTransform rect;
    private SpriteSheet judgementSpriteSheet;
    private List<SpriteSheet> comboDigitSpriteSheet;
    private float startTime;

    // Start is called before the first frame update
    void Start()
    {
        ResetSize();
        Hide();

        transformToFollow = null;
        elementToFollow = null;
        rect = GetComponent<RectTransform>();
        judgementSpriteSheet = null;
        comboDigitSpriteSheet = new List<SpriteSheet>();
        foreach (Image i in comboDigits)
        {
            comboDigitSpriteSheet.Add(null);
        }
        startTime = 0f;
    }

    private float GetWidth(SpriteSheet spriteSheet)
    {
        if (spriteSheet.sprites.Count == 0) return 0f;
        float height = GlobalResource.comboSkin.height;
        float ratio = spriteSheet.sprites[0].rect.height /
            spriteSheet.sprites[0].rect.width;
        return height / ratio;
    }

    public void ResetSize()
    {
        if (GlobalResource.comboSkin != null)
        {
            comboTextLayout.anchoredPosition = new Vector2(
                0f, GlobalResource.comboSkin.distanceToNote);
            comboTextLayout.sizeDelta = new Vector2(
                comboTextLayout.sizeDelta.x,
                GlobalResource.comboSkin.height);
            space.sizeDelta = new Vector2(
                GlobalResource.comboSkin.spaceBetweenJudgementAndCombo,
                space.sizeDelta.y);
        }
    }

    public void Hide()
    {
        judgementText.gameObject.SetActive(false);
        space.gameObject.SetActive(false);
        comboDigits.ForEach(i => i.gameObject.SetActive(false));
    }

    // Deprecated.
    public void Show(NoteObject n, Judgement judgement)
    {
        if (n != null)
        {
            transformToFollow = n.GetComponent<NoteAppearance>()
                .noteImage.transform;
        }
        Follow();

        // Draw judgement.

        List<SpriteSheet> comboDigitSpriteSheetList = null;
        if (Game.feverState == Game.FeverState.Active &&
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

        if (Game.currentCombo > 0)
        {
            space.gameObject.SetActive(true);

            List<int> digits = new List<int>();
            int remainingCombo = Game.currentCombo;
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
        UpdateSprites();
        animator.SetTrigger("Activate");
    }

    public void Show(NoteElements n, Judgement judgement,
        ScoreKeeper scoreKeeper)
    {
        if (n != null)
        {
            elementToFollow = n.noteImage;
        }
        Follow();

        // Draw judgement.

        List<SpriteSheet> comboDigitSpriteSheetList = null;
        if (scoreKeeper.feverState == ScoreKeeper.FeverState.Active &&
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

        if (scoreKeeper.currentCombo > 0)
        {
            space.gameObject.SetActive(true);

            List<int> digits = new List<int>();
            int remainingCombo = scoreKeeper.currentCombo;
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
        UpdateSprites();
        animator.SetTrigger("Activate");
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
        Follow();
        UpdateSprites();
    }

    private void Follow()
    {
        if (elementToFollow != null)
        {
            Vector2 center = elementToFollow.worldBound.center;
            center.y = Screen.height - center.y;
            rect.anchoredPosition = center;
        }
        else if (transformToFollow != null)
        {
            transform.position = transformToFollow.position;
        }
    }
}
