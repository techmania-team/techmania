using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TrackAndPatternSidesheet
{
    public class TrackSubfolderButton : MonoBehaviour
    {
        public TextMeshProUGUI title;

        private string folderFullPath;
        private TrackAndPatternSideSheet sidesheet;

        public void SetUp(TrackAndPatternSideSheet sidesheet,
            GlobalResource.Subfolder subfolder)
        {
            title.text = subfolder.name;
            this.sidesheet = sidesheet;
            folderFullPath = subfolder.fullPath;
        }

        public void OnClick()
        {
            sidesheet.OnTrackSubfolderButtonClick(folderFullPath);
        }
    }
}