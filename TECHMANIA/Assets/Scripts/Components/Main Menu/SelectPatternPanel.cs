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
    public TextMeshProUGUI authorText;
    public TextMeshProUGUI lengthText;
    public TextMeshProUGUI notesText;
    public ScrollingText modifiersText;
    public ScrollingText specialModifiersText;

    [Header("Buttons")]
    public ModifierSidesheet modifierSidesheet;
    public Button playButton;

    private void OnEnable()
    {
        // Show track details.
        Track track = GameSetup.track;
        eyecatchImage.LoadImage(GameSetup.trackFolder,
            track.trackMetadata);
        genreText.text = track.trackMetadata.genre;
        titleText.text = track.trackMetadata.title;
        artistText.text = track.trackMetadata.artist;
        trackDetailsScrollingText.SetUp();

        // Initialize pattern list.
        GameObject firstObject =
            patternList.InitializeAndReturnFirstPatternObject(track);
        PatternRadioList.SelectedPatternChanged += 
            OnSelectedPatternObjectChanged;

        // Other UI elements.
        modifierSidesheet.Prepare();
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
    }

    private void OnDisable()
    {
        PatternRadioList.SelectedPatternChanged -= 
            OnSelectedPatternObjectChanged;
        ModifierSidesheet.ModifierChanged -=
            OnModifierChanged;
        previewPlayer.Stop();
    }

    private void RefreshPatternDetails(Pattern p)
    {
        if (p == null)
        {
            authorText.text = "-";
            lengthText.text = "-";
            notesText.text = "-";
            playButton.interactable = false;
        }
        else
        {
            p.PrepareForTimeCalculation();
            float length = p.GetLengthInSeconds();

            authorText.text = p.patternMetadata.author;
            lengthText.text = UIUtils.FormatTime(length,
                includeMillisecond: false);
            notesText.text = p.NumPlayableNotes().ToString();
            playButton.interactable = true;
        }
    }

    private void OnSelectedPatternObjectChanged(Pattern p)
    {
        RefreshPatternDetails(p);
    }

    private void OnModifierChanged()
    {
        string modifierLine1, modifierLine2;
        modifierSidesheet.GetModifierDisplay(
            out modifierLine1, out modifierLine2);

        modifiersText.SetUp(modifierLine1);
        specialModifiersText.SetUp(modifierLine2);
    }

    public void OnModifierButtonClick()
    {
        modifierSidesheet.GetComponent<Sidesheet>().FadeIn();
    }

    public void OnPlayButtonClick()
    {
        GameSetup.pattern = patternList.GetSelectedPattern();
        if (GameSetup.pattern == null) return;

        Modifiers.Mode mode = Modifiers.Mode.Normal;
        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
            mode = Modifiers.Mode.NoFail;
        }
        if (Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift))
        {
            mode = Modifiers.Mode.AutoPlay;
        }
        Modifiers.instance.mode = mode;

        // Save to disk because the game scene will reload options.
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());

        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
