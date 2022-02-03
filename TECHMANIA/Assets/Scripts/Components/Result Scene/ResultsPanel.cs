using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultsPanel : MonoBehaviour
{
    public TextMeshProUGUI title;

    [Header("Track and Pattern")]
    public EyecatchSelfLoader eyecatch;
    public TextMeshProUGUI trackTitle;
    public TextMeshProUGUI trackArtist;
    public PatternBanner patternBanner;

    [Header("Tallies")]
    public TextMeshProUGUI rainbowMax;
    public TextMeshProUGUI max;
    public TextMeshProUGUI cool;
    public TextMeshProUGUI good;
    public TextMeshProUGUI miss;
    public TextMeshProUGUI breakText;
    public TextMeshProUGUI maxCombo;
    public TextMeshProUGUI feverBonus;
    public GameObject comboBonusContainer;
    public TextMeshProUGUI comboBonus;

    [Header("Other")]
    public TextMeshProUGUI totalScore;
    public GameObject performanceMedalText;
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI recordText;
    public GameObject newRecordMedalText;
    public TextMeshProUGUI ruleset;
    public ScrollingText modifierDisplay;
    public Color specialModifierColor;
    public GameObject legacyRulesetOverrideIndicator;

    // Start is called before the first frame update
    void Start()
    {
        title.text = Locale.GetString(Game.score.stageFailed ?
            "result_panel_stage_failed_title" :
            "result_panel_stage_clear_title");
        GameSetup.patternBeforeApplyingModifier
            .CalculateFingerprint();

        // Track and Pattern
        TrackMetadata track = GameSetup.track.trackMetadata;
        eyecatch.LoadImage(GameSetup.trackFolder, track);
        trackTitle.text = track.title;
        trackArtist.text = track.artist;

        PatternMetadata pattern = GameSetup
            .patternBeforeApplyingModifier.patternMetadata;
        patternBanner.Initialize(pattern);

        // Tallies
        rainbowMax.text = Game.score.notesPerJudgement
            [Judgement.RainbowMax].ToString();
        max.text = Game.score.notesPerJudgement
            [Judgement.Max].ToString();
        cool.text = Game.score.notesPerJudgement
            [Judgement.Cool].ToString();
        good.text = Game.score.notesPerJudgement
            [Judgement.Good].ToString();
        miss.text = Game.score.notesPerJudgement
            [Judgement.Miss].ToString();
        breakText.text = Game.score.notesPerJudgement
            [Judgement.Break].ToString();
        maxCombo.text = Game.maxCombo.ToString();
        comboBonusContainer.SetActive(Ruleset.instance.comboBonus);
        Game.score.CalculateComboBonus();
        comboBonus.text = Game.score.comboBonus.ToString();
        feverBonus.text = Game.score.totalFeverBonus.ToString();

        // Score and medal
        int score = Game.score.CurrentScore()
            + Game.score.comboBonus
            + Game.score.totalFeverBonus;
        totalScore.text = score.ToString();

        TextMeshProUGUI medalText = performanceMedalText
            .GetComponentInChildren<TextMeshProUGUI>();
        medalText.text = Record.MedalToString(Game.score.Medal());

        // Rank
        string rank;
        if (Game.score.stageFailed)
        {
            rank = "F";
        }
        else
        {
            rank = Score.ScoreToRank(score);
        }
        rankText.text = rank;

        // My record
        Record record = Records.instance.GetRecord(
            GameSetup.patternBeforeApplyingModifier);  // May be null
        if (record == null)
        {
            recordText.text = Record.EmptyRecordString();
        }
        else
        {
            recordText.text = record.ToString();
        }

        Score scoreForRecord = Game.score;
        if (Options.instance.ruleset == Options.Ruleset.Custom ||
            Modifiers.instance.HasAnySpecialModifier() ||
            Game.score.stageFailed)
        {
            scoreForRecord = null;
        }

        bool newRecord;
        Records.instance.UpdateRecord(
            GameSetup.patternBeforeApplyingModifier,
            scoreForRecord,
            record,
            out newRecord);
        newRecordMedalText.SetActive(newRecord);
        Records.instance.SaveToFile(Paths.GetRecordsFilePath());

        // Ruleset
        legacyRulesetOverrideIndicator.SetActive(false);
        switch (Options.instance.ruleset)
        {
            case Options.Ruleset.Standard:
                ruleset.text = Locale.GetString(
                    "options_ruleset_standard");
                break;
            case Options.Ruleset.Legacy:
                ruleset.text = Locale.GetString(
                    "options_ruleset_legacy");
                if (GameSetup.pattern.legacyRulesetOverride.HasAny())
                {
                    ruleset.text = ruleset.text + "*";
                    legacyRulesetOverrideIndicator.SetActive(true);
                }
                break;
            case Options.Ruleset.Custom:
                ruleset.text = Locale.GetString(
                    "options_ruleset_custom");
                break;
        }

        // Modifier display
        modifierDisplay.SetUp(ModifierSidesheet.GetDisplayString(
            noVideo: false, specialModifierColor));
    }

    public void OnSelectTrackButtonClick()
    {
        MainMenuPanel.skipToTrackSelect = true;
        Curtain.DrawCurtainThenGoToScene("Main Menu");
    }

    public void OnRetryButtonClick()
    {
        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
