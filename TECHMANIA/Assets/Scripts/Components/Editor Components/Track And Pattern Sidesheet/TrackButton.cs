using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TrackAndPatternSidesheet
{
    public class TrackButton : MonoBehaviour
    {
        public TextMeshProUGUI title;

        private Track minimizedTrack;
        private TrackAndPatternSideSheet sidesheet;

        public void SetUp(TrackAndPatternSideSheet sidesheet,
            GlobalResource.TrackInFolder trackInFolder)
        {
            minimizedTrack = trackInFolder.minimizedTrack;
            title.text = minimizedTrack.trackMetadata.title;
            this.sidesheet = sidesheet;
        }

        public void OnClick()
        {
            sidesheet.OnTrackButtonClick(minimizedTrack);
        }
    }
}