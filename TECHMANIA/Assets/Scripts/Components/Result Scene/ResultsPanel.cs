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
    public TextMeshProUGUI totalScore;

    [Header("Rank")]
    public TextMeshProUGUI rankText;

    [Header("Medals")]
    public GameObject performanceMedal;
    public GameObject newRecordMedal;

    // Start is called before the first frame update
    void Start()
    {
        title.text = Locale.GetString(Game.score.stageFailed ?
            "result_panel_stage_failed_title" :
            "result_panel_stage_clear_title");

        // Track and Pattern
        TrackMetadata track = GameSetup.track.trackMetadata;
        eyecatch.LoadImage(GameSetup.trackFolder, track);
        trackTitle.text = track.title;
        trackArtist.text = track.artist;

        PatternMetadata pattern = GameSetup.pattern.patternMetadata;
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

        int score = Game.score.CurrentScore() + Game.score.comboBonus;
        totalScore.text = score.ToString();

        // Rank
        // The choice of rank is quite arbitrary.
        string rank = "C";
        if (score > 220000) rank = "B";
        if (score > 260000) rank = "A";
        if (score > 270000) rank = "A<size=64>+</size>";
        if (score > 280000) rank = "A<size=64>++</size>";
        if (score > 285000) rank = "S";
        if (score > 290000) rank = "S<size=64>+</size>";
        if (score > 295000) rank = "S<size=64>++</size>";
        if (Game.score.stageFailed) rank = "F";
        rankText.text = rank;

        // Medals
        newRecordMedal.SetActive(false);
        if (Game.score.notesPerJudgement[Judgement.Miss] == 0 &&
            Game.score.notesPerJudgement[Judgement.Break] == 0)
        {
            // Qualified for performance medal.
            performanceMedal.SetActive(true);
            TextMeshProUGUI medalText = performanceMedal
                .GetComponentInChildren<TextMeshProUGUI>();
            if (Game.score.notesPerJudgement[Judgement.Cool] == 0 &&
                Game.score.notesPerJudgement[Judgement.Good] == 0)
            {
                if (score == 300000)
                {
                    medalText.text = Locale.GetString(
                        "result_panel_absolute_perfect_medal");
                }
                else
                {
                    medalText.text = Locale.GetString(
                        "result_panel_perfect_play_medal");
                }
            }
            else
            {
                medalText.text = Locale.GetString(
                    "result_panel_full_combo_medal");
            }
        }
        else
        {
            performanceMedal.SetActive(false);
        }
    }

    public void OnContinueButtonClick()
    {
        MainMenuPanel.skipToTrackSelect = true;
        Curtain.DrawCurtainThenGoToScene("Main Menu");
    }

    public void OnRetryButtonClick()
    {
        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
