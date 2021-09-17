using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// SavedChecked Switcher
    ///･Switch the processing (callback) depends on the SavedChecked state.
    ///･When first time (before saving) always false (SavedChecked).
    /// </summary>
    public class SavedCheckedSwitcher : MonoBehaviour
    {
        //Inspector Settings
        public SavedCheckedBehaviour target;    //YesNoWithCheckBoxDialogController or OKWithCheckBoxDialogController

        //Callbacks
        public UnityEvent OnTrue;               //Execute (callback) when SavedChecked is true.
        public UnityEvent OnFalse;              //Execute (callback) when SavedChecked is false. Note: At first time (before saving), always false.


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        //Switch the processing by SavedChecked and execute (callback).
        public void StartSwitch()
        {
            if (target == null)
                return;

            if (target.SavedChecked)
            {
                if (OnTrue != null)
                    OnTrue.Invoke();
            }
            else
            {
                if (OnFalse != null)
                    OnFalse.Invoke();
            }
        }
    }
}
