using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectPatternDialog : MonoBehaviour
{
    public EyecatchSelfLoader eyecatchImage;
    public PreviewTrackPlayer previewPlayer;
    public TextMeshProUGUI genreText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;
    public PatternRadioList patternList;
    public Button backButton;
    public Button playButton;

    public void Show()
    {
        GetComponent<Dialog>().FadeIn();

        // Show track metadata.
        Track track = GameSetup.track;
        eyecatchImage.LoadImage(GameSetup.trackFolder, track.trackMetadata);
        genreText.text = track.trackMetadata.genre;
        titleText.text = track.trackMetadata.title;
        artistText.text = track.trackMetadata.artist;

        // Initialize pattern list.
        GameObject firstObject =
            patternList.InitializeAndReturnFirstPatternObject(track);

        // Other UI elements.
        RefreshPlayButton();
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

    private void OnEnable()
    {
        PatternRadioList.SelectedPatternChanged += OnSelectedPatternObjectChanged;
    }

    private void OnDisable()
    {
        PatternRadioList.SelectedPatternChanged -= OnSelectedPatternObjectChanged;
        previewPlayer.Stop();
    }

    private void RefreshPlayButton()
    {
        playButton.interactable = patternList.GetSelectedPattern() != null;
    }

    private void OnSelectedPatternObjectChanged(Pattern p)
    {
        RefreshPlayButton();
    }

    public void OnPlayButtonClick()
    {
        GameSetup.pattern = patternList.GetSelectedPattern();
        if (GameSetup.pattern == null) return;
        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
