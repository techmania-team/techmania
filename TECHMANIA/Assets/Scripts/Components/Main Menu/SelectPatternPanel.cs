using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectPatternPanel : MonoBehaviour
{
    public GameObject backButton;
    public PreviewTrackPlayer previewPlayer;

    [Header("Track details")]
    public EyecatchSelfLoader eyecatchImage;
    public ScrollingText trackDetailsScrollingText;
    public TextMeshProUGUI genreText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;

    [Header("Pattern list")]
    public PatternRadioList patternList;

    [Header("Pattern details")]
    public ScrollingText bpmText;
    public TextMeshProUGUI lengthText;
    public TextMeshProUGUI notesText;
    public Radar radar;
    public ScrollingText authorText;
    public TextMeshProUGUI recordText;
    public TextMeshProUGUI recordMedalText;
    public ScrollingText modifiersText;
    public Color specialModifierColor;

    [Header("Buttons")]
    public ModifierSidesheet modifierSidesheet;
    public Button playButton;

    private Dictionary<Pattern, Record> records;

    private void OnEnable()
    {
        // Load full track from disk.
        GameSetup.track = Track.LoadFromFile(GameSetup.trackPath)
            as Track;

        // Show track details.
        Track track = GameSetup.track;
        eyecatchImage.LoadImage(GameSetup.trackFolder,
            track.trackMetadata);
        genreText.text = track.trackMetadata.genre;
        titleText.text = track.trackMetadata.title;
        artistText.text = track.trackMetadata.artist;
        trackDetailsScrollingText.SetUp();

        // Read records of all patterns.
        records = new Dictionary<Pattern, Record>();
        foreach (Pattern p in track.patterns)
        {
            p.CalculateFingerprint();
            records.Add(p, Records.instance.GetRecord(p));
        }

        // Initialize pattern list.
        GameObject firstObject =
            patternList.InitializeAndReturnFirstPatternObject(
                track, records);
        PatternRadioList.SelectedPatternChanged += 
            OnSelectedPatternObjectChanged;

        // Other UI elements.
        ModifierSidesheet.ModifierChanged += OnModifierChanged;
        OnModifierChanged();
        RefreshPatternDetails(p: null);
        if (firstObject == null)
        {
            firstObject = backButton.gameObject;
        }
        EventSystem.current.SetSelectedGameObject(firstObject);

        // Play preview.
        previewPlayer.Play(GameSetup.trackFolder,
            GameSetup.track.trackMetadata,
            loop: true);

        DiscordController.SetActivity(DiscordActivityType.SelectingPattern);
    }

    private void OnDisable()
    {
        PatternRadioList.SelectedPatternChanged -= 
            OnSelectedPatternObjectChanged;
        ModifierSidesheet.ModifierChanged -=
            OnModifierChanged;
        previewPlayer.Stop();
    }

    private void Update()
    {
        // Synchronize alpha with sidesheet because the
        // CanvasGroup on the sidesheet ignores parent.
        if (PanelTransitioner.transitioning &&
            modifierSidesheet.gameObject.activeSelf)
        {
            modifierSidesheet.GetComponent<CanvasGroup>().alpha
                = GetComponent<CanvasGroup>().alpha;
        }
    }

    private void RefreshPatternDetails(Pattern p)
    {
        if (p == null)
        {
            bpmText.SetUp("-");
            lengthText.text = "-";
            notesText.text = "-";
            radar.SetEmpty();
            authorText.SetUp("-");
            recordText.text = Record.EmptyRecordString();
            recordMedalText.text = "";
            playButton.interactable = false;
        }
        else
        {
            p.PrepareForTimeCalculation();
            float length;
            p.GetLengthInSecondsAndScans(out length, out _);

            // Get BPM.
            double minBPM = p.patternMetadata.initBpm;
            double maxBPM = minBPM;
            foreach (BpmEvent e in p.bpmEvents)
            {
                if (e.bpm < minBPM) minBPM = e.bpm;
                if (e.bpm > maxBPM) maxBPM = e.bpm;
            }
            string bpmString;
            if (minBPM < maxBPM)
            {
                bpmString = 
                    $"{FormatBPM(minBPM)} - {FormatBPM(maxBPM)}";
            }
            else
            {
                bpmString = FormatBPM(minBPM);
            }

            bpmText.SetUp(bpmString);
            lengthText.text = UIUtils.FormatTime(length,
                includeMillisecond: false);
            notesText.text = p.NumPlayableNotes().ToString();
            radar.SetRadar(p.CalculateRadar());
            authorText.SetUp(p.patternMetadata.author);
            Record r = records[p];
            if (r != null)
            {
                recordText.text = r.ToString();
                recordMedalText.text = Record.MedalToString(
                    r.medal);
            }
            else
            {
                recordText.text = Record.EmptyRecordString();
                recordMedalText.text = "";
            }
            playButton.interactable = true;
        }
    }

    private string FormatBPM(double bpm)
    {
        int floored = Mathf.FloorToInt((float)bpm);
        if (Mathf.Abs(floored - (float)bpm) < Mathf.Epsilon)
        {
            return floored.ToString();
        }
        else
        {
            return bpm.ToString("F2");
        }
    }

    private void OnSelectedPatternObjectChanged(Pattern p)
    {
        RefreshPatternDetails(p);
    }

    private void OnModifierChanged()
    {
        bool noVideo = GameSetup.trackOptions.noVideo;
        modifiersText.SetUp(ModifierSidesheet.GetDisplayString(
            noVideo, specialModifierColor));
    }

    public void OnModifierButtonClick()
    {
        modifierSidesheet.GetComponent<Sidesheet>().FadeIn();
    }

    public void OnPlayButtonClick()
    {
        previewPlayer.Stop();

        // Create a clone of the pattern with modifiers applied.
        // Game will operate on the clone.
        // The original pattern is kept in memory so its GUID and
        // fingerprint are still available later.
        GameSetup.patternBeforeApplyingModifier = 
            patternList.GetSelectedPattern();
        if (GameSetup.patternBeforeApplyingModifier == null)
            return;
        GameSetup.pattern = GameSetup.patternBeforeApplyingModifier
            .ApplyModifiers(Modifiers.instance);

        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
            Modifiers.instance.mode = Modifiers.Mode.NoFail;
            OnModifierChanged();
        }
        if (Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift))
        {
            Modifiers.instance.mode = Modifiers.Mode.AutoPlay;
            OnModifierChanged();
        }

        // Save to disk because the game scene will reload options.
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());

        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
