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
    public TextMeshProUGUI totalScore;

    [Header("Rank")]
    public TextMeshProUGUI rankText;

    [Header("Medals")]
    public GameObject performanceMedal;
    public GameObject newRecordMedal;

    // Start is called before the first frame update
    void Start()
    {
        title.text = Game.score.stageFailed ? "Stage Failed" : "Stage Clear";

        // Track and Pattern
        TrackMetadata track = GameSetup.track.trackMetadata;
        eyecatch.LoadImage(GameSetup.trackFolder, track);
        trackTitle.text = track.title;
        trackArtist.text = track.artist;

        PatternMetadata pattern = GameSetup.pattern.patternMetadata;
        patternBanner.Initialize(pattern);

        // Tallies
        rainbowMax.text = Game.score.notesPerJudgement[Judgement.RainbowMax].ToString();
        max.text = Game.score.notesPerJudgement[Judgement.Max].ToString();
        cool.text = Game.score.notesPerJudgement[Judgement.Cool].ToString();
        good.text = Game.score.notesPerJudgement[Judgement.Good].ToString();
        miss.text = Game.score.notesPerJudgement[Judgement.Miss].ToString();
        breakText.text = Game.score.notesPerJudgement[Judgement.Break].ToString();
        maxCombo.text = Game.maxCombo.ToString();
        feverBonus.text = Game.score.totalFeverBonus.ToString();

        int score = Game.score.CurrentScore();
        totalScore.text = score.ToString();

        // Rank
        // The choice of rank is quite arbitrary.
        string rank = "F";
        if (score > 150000) rank = "C";
        if (score > 200000) rank = "B";
        if (score > 250000) rank = "A";
        if (score > 290000) rank = "S";
        if (Game.score.stageFailed) rank = "F";
        rankText.text = rank;

        // Medals
        newRecordMedal.SetActive(false);
        if (Game.score.notesPerJudgement[Judgement.Miss] == 0 &&
            Game.score.notesPerJudgement[Judgement.Break] == 0)
        {
            // Qualified for performance medal.
            performanceMedal.SetActive(true);
            TextMeshProUGUI medalText = performanceMedal.GetComponentInChildren<TextMeshProUGUI>();
            if (Game.score.notesPerJudgement[Judgement.Cool] == 0 &&
                Game.score.notesPerJudgement[Judgement.Good] == 0)
            {
                if (Game.score.notesPerJudgement[Judgement.Max] == 0)
                {
                    medalText.text = "ABSOLUTE PERFECT";
                }
                else
                {
                    medalText.text = "PERFECT PLAY";
                }
            }
            else
            {
                medalText.text = "FULL COMBO";
            }
        }
        else
        {
            performanceMedal.SetActive(false);
        }
    }

    public void OnContinueButtonClick()
    {
        WelcomeMat.skipToTrackSelect = true;
        Curtain.DrawCurtainThenGoToScene("Main Menu");
    }

    public void OnRetryButtonClick()
    {
        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
