using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Switch Dialog Controller
    ///
    ///(*) The message character string (message) and the buttons are not displayed when the item does not fit in the dialog (exclusive to the scrolling display).
    ///    In that case, you will only be able to display the button if you lose the message (to empty("")): Android dialog specification(?).
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) 'SaveValue' is better not to use it when dynamically changing items (SetItem(), Show(string[])). It becomes incompatible with saved value.
    ///(*) When using value save (saveValue), it is better to give a specific save name (saveKey) individually.
    ///    (By default it is saved as GameObject.name [*using PlayerPrefs], so the same name across the scene, it will be overwritten).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）。
    ///  その場合、メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※動的に items を変更（SetItem(), Show(strnig[])）するときは、値の保存（saveValue）は使用しない方が良い（保存された値との整合性が無くなるため）。
    ///※値の保存（saveValue）をするときは、なるべく固有の保存名（saveKey）を設定した方が良い
    ///（デフォルトではGameObject名で保存されるため[※PlayerPrefs を利用]、シーンをまたがって同じ名前があると上書きされてしまう）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class SwitchDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                  //Dialog title
        [Multiline] public string message = "Message";  //Dialog message (It should be empty when overflowing)

        [Serializable]
        public class Item
        {
            public string text = "";                    //Text for each item
            public string key = "";                     //Identification key for each item
            public bool isOn = false;                   //Switch is On

            public Item() { }
            public Item(string text, string key, bool isOn = false)
            {
                this.text = text;
                this.key = key;
                this.isOn = isOn;
            }

            public Item Clone()
            {
                return (Item)MemberwiseClone();
            }
        }
        [SerializeField] private Item[] items;          //All items

        public Color itemsTextColor = Color.black;      //Text color of all items

        public string okButton = "OK";                  //Text of 'OK' button.
        public string cancelButton = "Cancel";          //Text of 'Cancel' button.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Save PlayerPrefs Settings
        public bool saveValue = false;                  //Whether to save the seleted (Also local value is always overwritten). It is better not to use it when dynamically changing items (It becomes incompatible with saved value).
        [SerializeField] private string saveKey = "";   //When specifying the PlayerPrefs key.

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";
            public string messageID = "message";
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
        [Serializable] public class ResultHandler : UnityEvent<Dictionary<string, bool>> { }    //key, isOn
        public ResultHandler OnResult;

        [Serializable] public class ValueChangedHandler : UnityEvent<string, bool> { }  //key, isOn
        public ValueChangedHandler OnValueChanged;

        [Serializable] public class CancelHandler : UnityEvent<Dictionary<string, bool>> { }  //key, isOn
        public CancelHandler OnCancel;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string SWITCH_PREF = "_switches";     //For all switches on/off

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_switches'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + SWITCH_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            Param param = Param.GetPlayerPrefs(SaveKey);
            SetOn(param);
        }

        //Save local values manually.
        //･To be saved value is only the 'key & value' pairs (Dictionary <string, string>).
        public void SavePrefs()
        {
            Dictionary<string, string> dic = items.ToDictionary(e => e.key, e => e.isOn.ToString());    //Duplicate key is not allowed.
            XPlayerPrefs.SetDictionary(SaveKey, dic);   //compatible 'Param' class
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

        //Convert the local on/off state to Dictionary <item.key, item.isOn>
        //･If saveValue is true, it will be automatically saved.
        public Dictionary<string, bool> CurrentValue {
            get { return items.ToDictionary(e => e.key, e => e.isOn); }
            set {
                SetOn(value);
                if (saveValue)
                    SavePrefs();
            }
        }

        //Propeties
        public string[] Texts {
            get { return items.Select(e => e.text).ToArray(); }
        }

        public string[] Keys {
            get { return items.Select(e => e.key).ToArray(); }
        }

        public bool[] IsOn {
            get { return items.Select(e => e.isOn).ToArray(); }
        }

        //Create arrays to be arguments of dialogs at once.
        private void GetItemArrays(out string[] texts, out string[] keys, out bool[] checkedItems)
        {
            texts = new string[items.Length];
            keys = new string[items.Length];
            checkedItems = new bool[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                texts[i] = items[i].text;
                keys[i] = items[i].key;
                checkedItems[i] = items[i].isOn;
            }
        }

        //Param <items.key, (string)items.isOn> -> local values
        private void SetOn(Param param)
        {
            if (param == null)
                return;

            foreach (var item in param)
            {
                if (keyToIndex.ContainsKey(item.Key))
                {
                    bool value;
                    if (bool.TryParse(item.Value, out value))
                        items[keyToIndex[item.Key]].isOn = value;
                }
            }
        }

        //Dictionary <items.key, items.isOn> -> local values
        private void SetOn(Dictionary<string, bool> dic)
        {
            if (dic == null)
                return;

            foreach (var item in dic)
            {
                if (keyToIndex.ContainsKey(item.Key))
                    items[keyToIndex[item.Key]].isOn = item.Value;
            }
        }


        //Set items dynamically (current items will be overwritten)
        //(*) When changed dynamically, it is inconsistent with the value saved in Playerprefs (better to use saveValue is false).
        //(*) The key becomes the index of string type.
        //(*) Empty and duplication are not checked.
        //(*) localization will be incompatible.
        //
        //動的にアイテムを設定する（現在のアイテムは上書き）
        //※動的にアイテム変更した場合、保存データ（PlayerPrefs）と互換性が無くなるので注意（saveValue オプションはオフで使う方が良い）。
        //※キー（key）は文字列型のインデクスになる。
        //※空や重複データはチェックされないので注意。
        //※ローカライズデータは互換性が無くなるので注意。
        public void SetItem(string[] texts, bool isOn)
        {
            if (texts == null)
                return;

            items = new Item[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                items[i] = new Item(texts[i], i.ToString(), isOn);  //key is empty -> index (string type)

            MakeKeyToIndexTable();
        }

        //･overload (For callback registration in the inspector)
        //･All values are off.
        public void SetItem(string[] texts)
        {
            SetItem(texts, false);  //all off
        }

        //Set items dynamically (current items will be overwritten)
        //(*) When changed dynamically, it is inconsistent with the value saved in PlayerPrefs (better to use saveValue is false).
        //(*) Empty and duplication are not checked.
        //(*) localization will be incompatible.
        //
        //動的にアイテムを設定する（現在のアイテムは上書き）
        //※動的にアイテム変更した場合、保存データ（PlayerPrefs）と互換性が無くなるので注意（saveValue オプションはオフで使う方が良い）。
        //※空や重複データはチェックされないので注意。
        //※ローカライズデータは互換性が無くなるので注意。
        public void SetItem(Item[] items)
        {
            if (items == null)
                return;

            this.items = items;
            MakeKeyToIndexTable();
        }


        //The values for reset.
        private Item[] initValue;

        //Store the values of the inspector.
        private void StoreInitValue()
        {
            initValue = (Item[])items.Clone();
            for (int i = 0; i < items.Length; i++)
                initValue[i] = items[i].Clone();
        }

        //Restore the values of the inspector and delete PlayerPrefs key.
        public void ResetValue()
        {
            items = (Item[])initValue.Clone();
            for (int i = 0; i < initValue.Length; i++)
                items[i] = initValue[i].Clone();

            MakeKeyToIndexTable();
            DeletePrefs();
        }


        //'key -> index' convert table
        private Dictionary<string, int> keyToIndex = new Dictionary<string, int>();

        //Make 'key -> index' table
        private void MakeKeyToIndexTable()
        {
            keyToIndex.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                if (!string.IsNullOrEmpty(items[i].key))
                    keyToIndex[items[i].key] = i;
            }
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
                if (keyToIndex.Count != items.Length)
                    Debug.LogError("[" + gameObject.name + "] There is empty or duplicate 'Key'.");

                HashSet<string> set = new HashSet<string>();
                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item.text))
                        set.Add(item.text);
                }
                if (set.Count != items.Length)
                    Debug.LogWarning("[" + gameObject.name + "] There is empty or duplicate 'Text'.");
            }

            //Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy.
            //Note: Search only within the same type.
            SwitchDialogController[] objs = FindObjectsOfType<SwitchDialogController>();
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
                message = localize.localizeResource.Text(localize.messageID, message);
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
                message = localize.localizeResource.Text(localize.messageID, language, message);
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
            MakeKeyToIndexTable();
            ApplyLocalize();
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
            Debug.Log("SwitchDialogController.Show called");
#elif UNITY_ANDROID
            string[] texts; string[] keys; bool[] checkedItems;
            GetItemArrays(out texts, out keys, out checkedItems);

            AndroidPlugin.ShowSwitchDialog(
                title,
                message,
                texts,
                keys,
                checkedItems,
                itemsTextColor,
                gameObject.name, "ReceiveResult", "ReceiveChanged", "ReceiveCancel",
                okButton, cancelButton,
                style);
#endif
        }

        //Set items dynamically and show dialog (current items will be overwritten).
        //Note: When changed dynamically, it is inconsistent with the value saved in Playerprefs (better to use saveValue is false).
        //Note: Empty and duplication are not checked.
        public void Show(string[] texts)
        {
            SetItem(texts, false);  //all off
            Show();
        }


        //Returns value when 'OK' pressed.
        private void ReceiveResult(string result)
        {
            Param param = Param.Parse(result);  //Parse: "key1=value1\nkey2=value2" -> same as Dictionary<key, value> (Param)
            if (param == null)
                return;

            if (saveValue)
            {
                SetOn(param);
                Param.SetPlayerPrefs(SaveKey, param);
                PlayerPrefs.Save();
            }

            if (OnResult != null)
            {
                try {
                    OnResult.Invoke(param.Select(e => new { k = e.Key, v = bool.Parse(e.Value) }).ToDictionary(a => a.k, a => a.v));
                }
                catch (Exception) { }
            }
        }

        //Returns value when switch pressed.
        public void ReceiveChanged(string result)
        {
            if (OnValueChanged != null)
            {
                string[] arr = result.Split('=');
                bool isOn;
                if (bool.TryParse(arr[1], out isOn))
                    OnValueChanged.Invoke(arr[0], isOn);
            }
        }
    
        //Returns value when 'Cancel' pressed or closed. (= initial value)
        private void ReceiveCancel(string result)
        {
            if (result != "CANCEL_DIALOG" && result != "CLOSE_DIALOG")
                return;

            if (OnCancel != null)
                OnCancel.Invoke(CurrentValue);
        }
    }
}
