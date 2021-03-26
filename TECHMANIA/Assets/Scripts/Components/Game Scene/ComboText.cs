using System.Collections;
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

    private Transform noteToFollow;

    private SpriteSheetForCombo judgementSpriteSheet;
    private List<SpriteSheetForCombo> comboDigitSpriteSheet;
    private float startTime;

    // Start is called before the first frame update
    void Start()
    {
        comboTextLayout.anchoredPosition = new Vector2(
            0f, GlobalResource.comboSkin.distanceToNote);
        comboTextLayout.sizeDelta = new Vector2(
            comboTextLayout.sizeDelta.x,
            GlobalResource.comboSkin.height);
        space.sizeDelta = new Vector2(
            GlobalResource.comboSkin.spaceBetweenJudgementAndCombo,
            space.sizeDelta.y);

        // Turn off everything for now.
        judgementText.gameObject.SetActive(false);
        space.gameObject.SetActive(false);
        comboDigits.ForEach(i => i.gameObject.SetActive(false));

        noteToFollow = null;
        judgementSpriteSheet = null;
        comboDigitSpriteSheet = new List<SpriteSheetForCombo>();
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

    public void Show(NoteObject n, Judgement judgement)
    {
        noteToFollow = n.GetComponent<NoteAppearance>()
            .noteImage.transform;
        Follow();

        // Draw judgement.

        List<SpriteSheetForCombo> comboDigitSpriteSheetList = null;
        if (Game.feverState == Game.FeverState.Active &&
            (judgement == Judgement.RainbowMax ||
             judgement == Judgement.Max))
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
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
                        .rainbowMaxDigits;
                    break;
                case Judgement.Max:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .maxJudgement;
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
                        .maxDigits;
                    break;
                case Judgement.Cool:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .coolJudgement;
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
                        .coolDigits;
                    break;
                case Judgement.Good:
                    judgementSpriteSheet = GlobalResource.comboSkin
                        .goodJudgement;
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
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
            bool seenAnyNonZeroDigit = false;
            for (int i = 0; i < comboDigits.Count; i++)
            {
                if (digits[i] == 0)
                {
                    comboDigits[i].gameObject.SetActive(
                        seenAnyNonZeroDigit);
                }
                else
                {
                    seenAnyNonZeroDigit = true;
                    comboDigits[i].gameObject.SetActive(true);
                }
            }

            for (int i = 0; i < comboDigits.Count; i++)
            {
                if (!comboDigits[i].gameObject.activeSelf) continue;
                comboDigits[i].GetComponent<RectTransform>().sizeDelta =
                    new Vector2(GetWidth(comboDigitSpriteSheet[i]),
                        0f);
            }
        }
        else
        {
            space.gameObject.SetActive(false);
            comboDigits.ForEach(i => i.gameObject.SetActive(false));
        }

        startTime = Game.Time;
        UpdateSprites();
        animator.SetTrigger("Activate");
    }

    private void UpdateSprites()
    {
        float time = Game.Time - startTime;

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
        if (noteToFollow != null)
        {
            Follow();
        }
        UpdateSprites();
    }

    private void Follow()
    {
        transform.position = noteToFollow.position;
    }
}
