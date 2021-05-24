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
    public TextMeshProUGUI modifiersText;
    public TextMeshProUGUI specialModifiersText;

    [Header("Buttons")]
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

    public void OnPlayButtonClick()
    {
        GameSetup.pattern = patternList.GetSelectedPattern();
        if (GameSetup.pattern == null) return;
        GameSetup.noFail = Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        GameSetup.autoPlay = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
