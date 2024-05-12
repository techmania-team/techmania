using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrackAndPatternSidesheet
{
    public class PatternButton : MonoBehaviour
    {
        public PatternBanner banner;

        private Pattern pattern;
        private TrackAndPatternSideSheet sidesheet;

        public void SetUp(TrackAndPatternSideSheet sidesheet,
            Pattern pattern)
        {
            banner.Initialize(pattern.patternMetadata);
            this.sidesheet = sidesheet;
            this.pattern = pattern;
        }

        public void OnClick()
        {
            sidesheet.OnPatternButtonClick(pattern);
        }
    }
}