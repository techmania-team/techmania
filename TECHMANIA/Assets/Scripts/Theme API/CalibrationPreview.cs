using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThemeApi
{
    // This is a MonoBehaviour so we can receive Update() events
    // without relying on GameController.
    [MoonSharpUserData]
    public class CalibrationPreview : MonoBehaviour
    {
        [MoonSharpHidden]
        public static CalibrationPreview instance;

        [MoonSharpHidden]
        public VisualTreeAsset calibrationPreviewTemplate;
        [MoonSharpHidden]
        public VFXManager vfxManager;

        [HideInInspector]
        public VisualElementWrap previewContainer;
        [HideInInspector]
        public List<string> timingDisplayClasses;
        [HideInInspector]
        public string earlyString;
        [HideInInspector]
        public string lateString;
        [HideInInspector]
        public bool setEarlyLateColors;
        [HideInInspector]
        public Color earlyColor;
        [HideInInspector]
        public Color lateColor;

        private bool running;

        // Start is called before the first frame update
        private void Start()
        {
            running = false;
            instance = this;
        }

        public void Begin()
        {

        }   
        
        public void ResetSize()
        {

        }

        public void SwitchToTouch()
        {

        }

        public void SwitchToKeyboardMouse()
        {

        }

        // Update is called once per frame
        private void Update()
        {
            if (!running) return;
        }

        public void Conclude()
        {

        }
    }
}