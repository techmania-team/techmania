using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Assign all SystemLanguage to Dropdown and functions
    /// </summary>
    public class LanguageDropdown : MonoBehaviour
    {
        //Inspector Settings
        public Dropdown targetDropdown;                 //Assign target
        public bool addAllLanguages = true;             //Add all SystemLanguage

        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.Unknown;   //When setting the default
        public bool selectDefaultOnStart = false;       //When setting the default, search the index (note that 'OnValueChanged' will occur)


        //Assign all SystemLanguage to Dropdown.
        private void AddAllSystemLanguageToDropdown()
        {
            if (targetDropdown == null)
                return;

            List<string> prev = targetDropdown.options.Select(e => e.text).ToList();
            HashSet<string> set = new HashSet<string>(prev);

            List<string> list = Enum.GetNames(typeof(SystemLanguage)).ToList();
            List<string> add = new List<string>();
            foreach (var item in list)
            {
                if (!set.Contains(item))
                    add.Add(item);      //add unique
                set.Add(item);
            }
            
            if (add.Count > 0)
                targetDropdown.AddOptions(add);
        }

        //Search index from character string.
        public int FindIndex(string lang, bool ignoreCase = true)
        {
            if (targetDropdown == null)
                return -1;

            if (ignoreCase)
                lang = lang.ToLower();

            for (int i = 0; i < targetDropdown.options.Count; i++)
            {
                string cap = targetDropdown.options[i].text;
                if (ignoreCase)
                    cap = cap.ToLower();
                if (cap == lang)
                    return i;
            }
            return -1;  //not found
        }

        //Search index from SystemLanguage.
        public int FindIndex(SystemLanguage lang)
        {
            return FindIndex(lang.ToString());
        }

        //Change language with index
        public int SelectedIndex {
            get {
                if (targetDropdown != null)
                    return targetDropdown.value;
                return -1;
            }
            set {
                if (targetDropdown != null)
                    return;
                if (0 <= value && value < targetDropdown.options.Count)
                    targetDropdown.value = value;   //'OnValueChanged' event occurs
            }
        }

        //Change language with string
        public int SetLanguage(string lang, bool ignoreCase = true)
        {
            int idx = FindIndex(lang, ignoreCase);
            if (idx >= 0)
            {
                targetDropdown.value = idx;     //'OnValueChanged' event occurs
                return idx;
            }
            return -1;
        }

        //Change language with SystemLanguage
        public int SetLanguage(SystemLanguage lang)
        {
            return SetLanguage(lang.ToString());
        }

        //Get the text of the currently selected language
        public string CaptionText {
            get {
                return (targetDropdown != null) ? targetDropdown.captionText.text : "";
            }
        }

        //Check if it is the currently selected language in the string
        public bool IsSelectedLanguage(string lang, bool ignoreCase = true)
        {
            if (targetDropdown == null)
                return false;

            string cap = targetDropdown.captionText.text;
            if (ignoreCase)
            {
                lang = lang.ToLower();
                cap = cap.ToLower();
            }
            return (lang == cap);
        }

        //Check if it is the currently selected language in SystemLanguage
        public bool IsSelectedLanguage(SystemLanguage lang)
        {
            return IsSelectedLanguage(lang.ToString());
        }



        // Use this for initialization
        private void Start()
        {
            if (addAllLanguages)
                AddAllSystemLanguageToDropdown();

            if (selectDefaultOnStart)
                SetLanguage(defaultLanguage);
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}
    }
}
