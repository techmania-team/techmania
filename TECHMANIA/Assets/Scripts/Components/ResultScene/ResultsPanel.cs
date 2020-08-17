using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultsPanel : MonoBehaviour
{
    public Text rMax;
    public Text max;
    public Text cool;
    public Text good;
    public Text miss;
    public Text breakText;
    public Text maxCombo;
    public Text feverBonus;
    public Text totalScore;
    public Text rankText;

    // Start is called before the first frame update
    void Start()
    {
        rMax.text = Game.score.notesPerJudgement[Judgement.RainbowMax].ToString();
        max.text = Game.score.notesPerJudgement[Judgement.Max].ToString();
        cool.text = Game.score.notesPerJudgement[Judgement.Cool].ToString();
        good.text = Game.score.notesPerJudgement[Judgement.Good].ToString();
        miss.text = Game.score.notesPerJudgement[Judgement.Miss].ToString();
        breakText.text = Game.score.notesPerJudgement[Judgement.Break].ToString();
        maxCombo.text = Game.maxCombo.ToString();
        feverBonus.text = Game.score.totalFeverBonus.ToString();

        int score = Game.score.CurrentScore();
        totalScore.text = score.ToString();

        // The choice of rank is quite arbitrary.
        string rank = "F";
        if (score > 150000) rank = "C";
        if (score > 200000) rank = "B";
        if (score > 250000) rank = "A";
        if (score > 290000) rank = "S";
        rankText.text = rank;
    }

    public void OnBackButtonClick()
    {
        Navigation.goToSelectTrackPanelOnStart = true;
        SceneManager.LoadScene("Main Menu");
    }
}
