using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Single Choice Dialog Controller
    /// 
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) 'SaveValue' is better not to use it when dynamically changing items (SetItem(), Show(strnig[])). It becomes incompatible with saved value.
    ///(*) When using value save (saveValue), it is better to give a specific save name (saveKey) individually.
    ///    (By default it is saved as GameObject.name [*using PlayerPrefs], so the same name across the scene, it will be overwritten).
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※動的に items を変更（SetItem(), Show(strnig[])）するときは、値の保存（saveValue）は使用しない方が良い（保存された値との整合性が無くなるため）。
    ///※値の保存（saveValue）をするときは、なるべく固有の保存名（saveKey）を設定した方が良い
    ///（デフォルトではGameObject名で保存されるため[※PlayerPrefs を利用]、シーンをまたがって同じ名前があると上書きされてしまう）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class SingleChoiceDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                  //Dialog title

        [Serializable]
        public class Item
        {
            public string text = "";                    //Text for each item
            public string value = "";                   //Value for each item

            public Item() { }
            public Item(string text, string value)
            {
                this.text = text;
                this.value = value;
            }

            public Item Clone()
            {
                return (Item)MemberwiseClone();
            }
        }
        [SerializeField] private Item[] items;          //Choices

        [SerializeField] private int selectedIndex = 0; //Default selected index

        //Callback value type
        [Serializable] public enum ResultType { Index, Text, Value }
        public ResultType resultType = ResultType.Value;

        public string okButton = "OK";                  //Text of 'OK' button.
        public string cancelButton = "Cancel";          //Text of 'Cancel' button.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Save PlayerPrefs Settings
        public bool saveValue = false;                  //Whether to save the seleted index (Also local value is always overwritten). It is better not to use it when dynamically changing items (It becomes incompatible with saved value).
        [SerializeField] private string saveKey = "";   //When specifying the PlayerPrefs key.

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";
            public string okButtonID = "okButton";
            public string cancelButtonID = "cancelButton";

            [Serializable]
            public class LocalizeItem
            {
                public LocalizeStringResource localizeResource;
                public string[] textID;
            }
            public LocalizeItem items;
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class ResultHandler : UnityEvent<string> { }      //text or value
        public ResultHandler OnResult;

        [Serializable] public class ResultIndexHandler : UnityEvent<int> { }    //index
        public ResultIndexHandler OnResultIndex;

        [Serializable] public class ValueChangedHandler : UnityEvent<string> { }  //text or value
        public ValueChangedHandler OnValueChanged;

        [Serializable] public class ValueIndexChangedHandler : UnityEvent<int> { }  //index
        public ValueIndexChangedHandler OnValueIndexChanged;

        [Serializable] public class CancelHandler : UnityEvent<string> { }      //text or value
        public CancelHandler OnCancel;

        [Serializable] public class CancelIndexHandler : UnityEvent<int> { }    //index
        public CancelIndexHandler OnCancelIndex;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string SELECTED_PREF = "_selected";   //For selected index

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_selected'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + SELECTED_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            selectedIndex = Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, selectedIndex), 0, items.Length -1);
        }

        //Save local values manually.
        //･To be saved value is only selected index.
        public void SavePrefs()
        {
            PlayerPrefs.SetInt(SaveKey, selectedIndex);
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

        //The currently (default) selected index.
        //･If saveValue is true, it will be automatically saved.
        public int CurrentValue {
            get { return selectedIndex; }
            set {
                selectedIndex = Mathf.Clamp(value, 0, items.Length - 1);
                if (saveValue)
                    SavePrefs();
            }
        }

        //Propeties
        public string[] Texts {
            get { return items.Select(e => e.text).ToArray(); }
        }

        public string[] Values {
            get { return items.Select(e => e.value).ToArray(); }
        }


        //Set items dynamically (current items will be overwritten)
        //･The selected index is 0. It can be changed by 'CurrentValue' property.
        //(*) When changed dynamically, it is inconsistent with the value saved in Playerprefs (better to use saveValue is false).
        //(*) When the resultType is 'Value', the value becomes the index of string type.
        //(*) Empty and duplication are not checked.
        //(*) localization will be incompatible.
        //
        //動的にアイテムを設定する（現在のアイテムは上書き）
        //・選択済みインデクスは 0 になる。変更したい場合は「CurrentValue」で設定する。
        //※動的にアイテム変更した場合、保存データ（PlayerPrefs）と互換性が無くなるので注意（saveValue オプションはオフで使う方が良い）。
        //※「resultType」が「Value」の場合、値は文字列型のインデクスになる。
        //※空や重複データはチェックされないので注意。
        //※ローカライズデータは互換性が無くなるので注意。
        public void SetItem(string[] texts)
        {
            if (texts == null)
                return;

            items = new Item[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                items[i] = new Item(texts[i], i.ToString());  //value is empty -> index (string type)

            selectedIndex = 0;
        }

        //Set items dynamically (current items will be overwritten)
        //･The selected index is 0. It can be changed by 'CurrentValue' property.
        //(*) When changed dynamically, it is inconsistent with the value saved in PlayerPrefs (better to use saveValue is false).
        //(*) Empty and duplication are not checked.
        //(*) localization will be incompatible.
        //
        //動的にアイテムを設定する（現在のアイテムは上書き）
        //・選択済みインデクスは 0 になる。変更したい場合は「CurrentValue」で設定する。
        //※動的にアイテム変更した場合、保存データ（PlayerPrefs）と互換性が無くなるので注意（saveValue オプションはオフで使う方が良い）。
        //※空や重複データはチェックされないので注意。
        //※ローカライズデータは互換性が無くなるので注意。
        public void SetItem(Item[] items)
        {
            if (items == null)
                return;

            this.items = items;
            selectedIndex = 0;
        }


        //The value for reset.
        private Item[] initValue;
        private int initSelectIndex;

        //Store the values of the inspector.
        private void StoreInitValue()
        {
            initValue = (Item[])items.Clone();
            for (int i = 0; i < items.Length; i++)
                initValue[i] = items[i].Clone();

            initSelectIndex = selectedIndex;
        }

        //Restore the value of inspector and delete PlayerPrefs key.
        public void ResetValue()
        {
            items = (Item[])initValue.Clone();
            for (int i = 0; i < initValue.Length; i++)
                items[i] = initValue[i].Clone();

            selectedIndex = initSelectIndex;
            DeletePrefs();
        }


        //Check empty or duplication from item elements.
        private void CheckForErrors()
        {
            if (items.Length == 0)
            {
                Debug.LogWarning("[" + gameObject.name + "] 'Items' is empty.");
            }
            else
            {
                if (resultType == ResultType.Value)
                {
                    HashSet<string> set = new HashSet<string>();
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.value))
                            set.Add(item.value);
                    }
                    if (set.Count != items.Length)
                        Debug.LogWarning("[" + gameObject.name + "] There is empty or duplicate 'Value'.");
                }
                else if (resultType == ResultType.Text)
                {
                    HashSet<string> set = new HashSet<string>();
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.text))
                            set.Add(item.text);
                    }
                    if (set.Count != items.Length)
                        Debug.LogWarning("[" + gameObject.name + "] There is empty or duplicate 'Text'.");
                }
            }

            //Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy.
            //Note: Search only within the same type.
            SingleChoiceDialogController[] objs = FindObjectsOfType<SingleChoiceDialogController>();
            if (objs.Length > 1)
            {
                HashSet<string> set = new HashSet<string>(objs.Select(e => e.gameObject.name).ToArray());
                if (set.Count != objs.Length)
                    Debug.LogError("[" + gameObject.name + "] There is duplicate 'gameObject.name'.");
            }
        }


        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, title);
                okButton = localize.localizeResource.Text(localize.okButtonID, okButton);
                cancelButton = localize.localizeResource.Text(localize.cancelButtonID, cancelButton);
            }

            if (localize.items.localizeResource != null)
            {
                int len = Mathf.Min(items.Length, localize.items.textID.Length);
                for (int i = 0; i < len; i++)
                {
                    items[i].text = localize.items.localizeResource.Text(localize.items.textID[i], items[i].text);
                    if (initValue != null && i < initValue.Length)
                        initValue[i].text = items[i].text;
                }
            }
        }

        //Specify language and apply (update) localized string
        //(*) When dynamically changing Items, it is better not to use it because it is incompatible.
        //※動的にItemsを変更する場合は、互換性がないので使わない方が良い。
        public override void ApplyLocalize(SystemLanguage language)
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, language, title);
                okButton = localize.localizeResource.Text(localize.okButtonID, language, okButton);
                cancelButton = localize.localizeResource.Text(localize.cancelButtonID, language, cancelButton);
            }

            if (localize.items.localizeResource != null)
            {
                int len = Mathf.Min(items.Length, localize.items.textID.Length);
                for (int i = 0; i < len; i++)
                {
                    items[i].text = localize.items.localizeResource.Text(localize.items.textID[i], language, items[i].text);
                    if (initValue != null && i < initValue.Length)
                        initValue[i].text = items[i].text;
                }
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            ApplyLocalize();
            selectedIndex = Mathf.Clamp(selectedIndex, 0, items.Length - 1);
            StoreInitValue();

            if (saveValue)
                LoadPrefs();
        }

        private void Start()
        {
#if UNITY_EDITOR
            CheckForErrors();    //Check for items (Editor only).
#endif
        }


        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        //Show dialog
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("SingleChoiceDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowSingleChoiceDialog(
                title,
                Texts,
                Mathf.Clamp(selectedIndex, 0, items.Length - 1),
                gameObject.name, "ReceiveResult", "ReceiveChanged", "ReceiveCancel",
                true,   //For internal processing, only uses Index type.
                okButton, cancelButton,
                style);
#endif
        }


        //Set items dynamically and show dialog (current items will be overwritten)
        //Note: When the resultType is 'Value', the value becomes the index of string type.
        //Note: When changed dynamically, it is inconsistent with the value saved in Playerprefs (better to use saveValue is false).
        //Note: Empty and duplication are not checked.
        public void Show(string[] texts)
        {
            SetItem(texts);
            Show();
        }


        //Returns value when 'OK' pressed.
        private void ReceiveResult(string result)
        {
            int index;      //For internal processing, only uses Index type.
            if (!int.TryParse(result, out index))
                return;

            if (saveValue)
            {
                selectedIndex = index;
                SavePrefs();
            }

            switch (resultType)
            {
                case ResultType.Index:
                    if (OnResultIndex != null)
                        OnResultIndex.Invoke(index);
                    break;
                case ResultType.Text:
                    if (OnResult != null)
                        OnResult.Invoke(items[index].text);
                    break;
                case ResultType.Value:
                    if (OnResult != null)
                        OnResult.Invoke(items[index].value);
                    break;
            }
        }

        //Returns value when choice.
        public void ReceiveChanged(string result)
        {
            int index;      //For internal processing, only uses Index type.
            if (!int.TryParse(result, out index))
                return;

            switch (resultType)
            {
                case ResultType.Index:
                    if (OnValueIndexChanged != null)
                        OnValueIndexChanged.Invoke(index);
                    break;
                case ResultType.Text:
                    if (OnValueChanged != null)
                        OnValueChanged.Invoke(items[index].text);
                    break;
                case ResultType.Value:
                    if (OnValueChanged != null)
                        OnValueChanged.Invoke(items[index].value);
                    break;
            }
        }

        //Returns value when 'Cancel' pressed or closed. (= initial value)
        private void ReceiveCancel(string result)
        {
            if (result != "CANCEL_DIALOG" && result != "CLOSE_DIALOG")
                return;

            switch (resultType)
            {
                case ResultType.Index:
                    if (OnCancelIndex != null)
                        OnCancelIndex.Invoke(selectedIndex);
                    break;
                case ResultType.Text:
                    if (OnCancel != null)
                        OnCancel.Invoke(items[selectedIndex].text);
                    break;
                case ResultType.Value:
                    if (OnCancel != null)
                        OnCancel.Invoke(items[selectedIndex].value);
                    break;
            }
        }
    }
}
