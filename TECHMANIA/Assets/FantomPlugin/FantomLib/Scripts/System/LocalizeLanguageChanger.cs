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
    /// Change the language of the localization function
    ///･When it is 'Unknown', it searches in order of 'system language -> default language' (Since of 'LocalizeString' specification).
    ///·When specified as a string type, when it is included in 'defaultStrings' (case insensitive), or when a language not defined in SystemLanguage is specified,
    /// it searches in order of 'system language -> default language' (That is, it becomes the same as 'Unknown' specification).
    ///
    ///・'Unknown'のとき、システム言語→デフォルト言語の順に検索される（'LocalizeString'の仕様のため）。 
    ///・文字列型で指定する場合は、'defaultStrings' に含まれるとき（大小文字を区別しない）、または SystemLanguage に定義されてない言語が指定されたとき、
    ///　システム言語→デフォルト言語の順に検索される（つまり 'Unknown' 指定と同じになる）。
    /// </summary>
    public class LocalizeLanguageChanger : MonoBehaviour
    {
        //Inspector Settings
        [SerializeField] private SystemLanguage language = SystemLanguage.Unknown;  //'Unknown' is default (system language)

        public bool applyOnStart = false;           //Execute 'ApplyLocalize()' on 'Start()'. (*) If 'saveSetting' is true, ignore 'applyOnStart'.


        //Language string for 'Unknown' (It is convert to lowercase letters)
        //·'LocalizeString' searches in order of system language → default language.
        //
        //'Unknown'と同義の文字列（小文字に変換される）
        //・'LocalizeString'では、システム言語→デフォルト言語の順に検索される。
        [SerializeField] private string[] defaultStrings = {
            "Unknown", "Default", "System", "System Language", "None" };

        //Search for objects with 'LocalizableBehaviour' on startup. (*) The current 'localizable' will be overwritten.
        public bool findLocalizable = false;                //When true, note that using 'FindObjectsOfType' is a high load.

        //Object (LocalizableBehaviour) to change language. (Note that if 'findLocalizable' is true, it will be overwritten.)
        [SerializeField] private LocalizableBehaviour[] localizableObjects;     

        //Object implementing 'ILocalizable' interface. ('findLocalizable' is not searched.)
        [SerializeField] private GameObject[] implGameobjects;                  


        //Save PlayerPrefs Settings
        public bool saveSetting = false;                    //Whether to save the settings (Also local value is always overwritten).
        [SerializeField] private string saveKey = "";       //When specifying the PlayerPrefs key for settings.

        //Callbacks
        [Serializable] public class LanguageChangedHandler : UnityEvent<SystemLanguage> { }
        public LanguageChangedHandler OnLanguageChanged;


#if UNITY_EDITOR
        //For debug (Editor play mode only)
        public SystemLanguage debugLanguage = SystemLanguage.Unknown;       //Display language for debug
        public bool debugApplyOnStart = false;                              //Force apply on 'Start()'. Note: Language initialize ('applyOnStart', 'saveSetting') will be overwritten.
        private SystemLanguage oldDebugLanguage = SystemLanguage.Unknown;   //Check for change debugLanguage
#endif


#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string SETTING_PREF = "_setting";
        const string LANGUAGE_KEY = "language";

        private Param setting = new Param();    //Compatibale Dictionary <string, string>

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_setting'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + SETTING_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            setting = Param.GetPlayerPrefs(SaveKey, setting);
            Language = ConvertToSystemLanguage(setting.GetString(LANGUAGE_KEY, Language.ToString()));
        }

        //Save local values manually.
        public void SavePrefs()
        {
            setting.Set(LANGUAGE_KEY, Language);
            Param.SetPlayerPrefs(SaveKey, setting);
            PlayerPrefs.Save();
        }

        //Delete PlayerPrefs key.
        //Note: Local values are not initialized at this time.
        public void DeletePrefs()
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }

        //Returns true if the saved value exists.
        public bool HasPrefs {
            get { return PlayerPrefs.HasKey(SaveKey); }
        }

#endregion

#region Properties and Local values Section

        //Change language setting and apply to 'localizable'.
        public SystemLanguage Language {
            get { return language; }
            set {
                if (language != value)
                {
                    language = value;
                    ApplyLocalize();

                    if (OnLanguageChanged != null)
                        OnLanguageChanged.Invoke(language);

                    if (saveSetting)
                        SavePrefs();
                }
            }
        }

        private List<ILocalizable> iLocalizable = new List<ILocalizable>();    //Cache

        //Create a list of 'ILocalizable'.
        private void MakeILocalizableList()
        {
            iLocalizable.Clear();

            HashSet<ILocalizable> set = new HashSet<ILocalizable>();
            foreach (var go in implGameobjects)
            {
                if (go == null)
                    continue;

                ILocalizable[] impls = go.GetComponents<ILocalizable>();
                if (impls != null)
                {
                    foreach (var obj in impls)
                        set.Add(obj);       //add unique
                }
            }

            if (set.Count > 0)
                iLocalizable.AddRange(set);
        }


        //Apply localization with the current 'Language' property.
        private void ApplyLocalize()
        {
            if (!initialized)
                Initialize();

            foreach (var item in localizableObjects)
                if (item != null)
                    item.ApplyLocalize(language);

            foreach (var item in iLocalizable)
                if (item != null)
                    item.ApplyLocalize(language);
        }

        //Change the current 'Language' property and apply localization.
        public void ApplyLocalize(SystemLanguage language)
        {
            Language = language;
        }

        //Change the current 'Language' (string type) property and apply localization.
        public void ApplyLocalize(string lang)
        {
            Language = ConvertToSystemLanguage(lang);
        }

        //Change the current 'Language' (dropdown selected text) property and apply localization.
        public void ApplyLocalize(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            Language = ConvertToSystemLanguage(dropdown.captionText.text);
        }


        private bool initialized = false;   //Initialized flag

        //Initialization processing such as necessary data
        private void Initialize()
        {
            defaultStrings = defaultStrings.Select(e => e.ToLower()).ToArray();
            Array.Sort(defaultStrings);     //For 'BinarySearch()'

            if (findLocalizable)
                localizableObjects = FindObjectsOfType<LocalizableBehaviour>(); //'ILocalizable' can not be used for 'FindObjectsOfType'. Also note that it is high load.

            if (implGameobjects.Length > 0)
                MakeILocalizableList();

            initialized = true;
        }

        //Is the character string included in 'defaultStrings'?
        public bool IsDefaultString(string lang)
        {
            if (!initialized)
                Initialize();

            return Array.BinarySearch(defaultStrings, lang.ToLower()) >= 0;
        }

        //Convert string (ignore case) to 'SystemLanguage' type. 
        //However, those that match 'defaultStrings' or those that can not be parsed are returned as 'Unknown'.
        //
        //文字列（大小文字を区別しない）を 'SystemLanguage' に変換する。
        //ただし、’defaultStrings’ に一致するもの、またはパースできなかったものは 'Unknown' で返す。
        public SystemLanguage ConvertToSystemLanguage(string lang)
        {
            try
            {
                return (IsDefaultString(lang)) ? 
                    SystemLanguage.Unknown : (SystemLanguage)Enum.Parse(typeof(SystemLanguage), lang, true);
            }
            catch
            {
                return SystemLanguage.Unknown;
            }
        }

#endregion

        // Use this for initialization
        private void Start()
        {
            if (!initialized)
                Initialize();

            if (saveSetting)    //If true, ignore 'applyOnStart'.
                LoadPrefs();
            else if (applyOnStart && language != SystemLanguage.Unknown)
                ApplyLocalize();

#if UNITY_EDITOR
            if (debugApplyOnStart)
                Language = oldDebugLanguage = debugLanguage;
            else
                oldDebugLanguage = debugLanguage = SystemLanguage.Unknown;
#endif
        }

        // Update is called once per frame
        private void Update()
        {
#if UNITY_EDITOR
            if (debugLanguage != oldDebugLanguage && initialized)
            {
                Language = oldDebugLanguage = debugLanguage;
            }
#endif
        }
    }
}
