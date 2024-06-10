using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This component:
// - updates the TimeSpans in Statistics.instance every frame
// - saves stats to disk every 30 seconds
public class StatsMaintainer : MonoBehaviour
{
    public int savePeriodInSeconds;

    private bool active = false;
    private bool inGame;
    private bool inEditor;
    private DateTime lastSaved;

    public static StatsMaintainer instance;

    private void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (!active) return;

        UpdateStats();

        if (DateTime.Now - lastSaved >=
            TimeSpan.FromSeconds(savePeriodInSeconds))
        {
            Statistics.instance.SaveToFile();
            lastSaved = DateTime.Now;
        }
    }

    public void BeginWorking()
    {
        active = true;
        inGame = false;
        inEditor = false;
        lastSaved = DateTime.Now;
    }

    private void UpdateStats()
    {
        Statistics stats = Statistics.instance;
        if (stats == null) return;

        TimeSpan span = TimeSpan.FromSeconds(Time.deltaTime);
        stats.totalPlayTime += span;
        if (inGame) stats.timeInGame += span;
        if (inEditor) stats.timeInEditor += span;
    }

    public void OnEnterGame()
    {
        UpdateStats();
        inGame = true;
    }

    public void OnLeaveGame()
    {
        UpdateStats();
        inGame = false;
    }

    public void OnEnterEditor()
    {
        UpdateStats();
        inEditor = true;
    }

    public void OnLeaveEditor()
    {
        UpdateStats();
        inEditor = false;
    }
}
