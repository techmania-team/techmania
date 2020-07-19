using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Navigation : MonoBehaviour
{
    private static Navigation instance;
    private static Navigation GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<Navigation>();
        }
        return instance;
    }

    public SelectTrackPanel selectTrackPanel;
    public TrackPanel trackPanel;

    public Text backButtonText;
    public Text title;

    public enum Location
    {
        SelectTrack,
        Track,
        Pattern,
        PatternMetadata
    }
    private Location location;

    public Track currentTrack;

    // Start is called before the first frame update
    void Start()
    {
        InternalGoTo(Location.SelectTrack);
        currentTrack = null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void SetCurrentTrack(Track track)
    {
        GetInstance().currentTrack = track;
    }

    public void GoBack()
    {
        switch (location)
        {
            case Location.SelectTrack:
                Debug.LogError("There is no main menu to go to.");
                break;
            case Location.Track:
                // TODO: Ask to save
                GoTo(Location.SelectTrack);
                break;
        }
    }

    public static void GoTo(Location location)
    {
        GetInstance().InternalGoTo(location);
    }

    private void InternalGoTo(Location location)
    {
        selectTrackPanel.gameObject.SetActive(location == Location.SelectTrack);
        trackPanel.gameObject.SetActive(location == Location.Track);
        this.location = location;

        switch (location)
        {
            case Location.SelectTrack:
                backButtonText.text = "< Main Menu";
                title.text = "TECHMANIA Editor";
                break;
            case Location.Track:
                backButtonText.text = "< All tracks";
                title.text = currentTrack.trackMetadata.title;
                break;
        }
    }
}
