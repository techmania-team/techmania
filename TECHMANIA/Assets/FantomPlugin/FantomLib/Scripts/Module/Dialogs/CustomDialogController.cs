using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Custom Dialog Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-290.html
    /// 
    /// (Usage)
    ///･When 'Find Items In Child' is on, put Prefab of 'DialogItemDivisor', 'DialogItemText', 'DialogItemSlider', 'DialogItemSwitch', 'DialogItemToggles'
    /// in transform.child (child GameObject on hierarchy) of GameObject to which CustomDialogController is attached.
    ///･When 'Find Items In Child' is off, register Prefab of 'DialogItemXXX'(XXX:widget type) to 'Items' in inspector.
    /// (Notes)
    ///(*) The message character string (message) and the buttons are not displayed when the item does not fit in the dialog (exclusive to the scrolling display).
    ///    In that case, you will only be able to display the button if you lose the message (to empty("")): Android dialog specification(?).
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) 'SaveValue' is better not to use it when dynamically changing items (SetItem()). It becomes incompatible with saved value.
    ///(*) When using value save (saveValue), it is better to give a specific save name (saveKey) individually.
    ///    (By default it is saved as GameObject.name [*using PlayerPrefs], so the same name across the scene, it will be overwritten).
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    /// (使い方)
    ///・「Find Items In Child」がオンのときは、CustomDialogController がアタッチされてる GameObject の transform.child（ヒエラルキー上での子GameObject）に
    /// 「DialogItemDivisor」「DialogItemText」「DialogItemSlider」「DialogItemSwitch」「DialogItemToggles」のプレファブを置く。
    ///・「Find Items In Child」がオフのときは、インスペクタで「Items」にプレファブ「DialogItemXXX」(XXX：ウィジェット形状)を登録する。
    /// (注意点)
    ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）。
    ///  その場合、メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※動的に items を変更（SetItem()）するときは、値の保存（saveValue）は使用しない方が良い（保存された値との整合性が無くなるため）。
    ///※値の保存（saveValue）をするときは、なるべく固有の保存名（saveKey）を設定した方が良い
    ///（デフォルトではGameObject名で保存されるため[※PlayerPrefs を利用]、シーンをまたがって同じ名前があると上書きされてしまう）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class CustomDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                          //Dialog title
        [Multiline] public string message = "";                 //Dialog message (It should be empty when overflowing)

        public bool findItemsInChild = true;                    //true: Find items in 'transform.GetChild()' / false: Inspector settings
        [SerializeField] private DialogItemParameter[] items;   //Use only in the inspector (When internal processing, It is converted to dialogItems and used).

        public string okButton = "OK";                          //Text of 'OK' button.
        public string cancelButton = "Cancel";                  //Text of 'Cancel' button.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Save PlayerPrefs Settings
        public bool saveValue = false;                  //Whether to save the 'key=value' pair (Also local value is always overwritten). It is better not to use it when dynamically changing items (It becomes incompatible with saved value).
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
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class ResultHandler : UnityEvent<Dictionary<string, string>> { }  //key, value
        public ResultHandler OnResult;

        [Serializable] public class ValueChangedHandler : UnityEvent<string, string> { }    //key, value
        public ValueChangedHandler OnValueChanged;

        [Serializable] public class CancelHandler : UnityEvent<Dictionary<string, string>> { }  //key, value
        public CancelHandler OnCancel;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string VALUE_PREF = "_values";

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_values'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + VALUE_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            Param param = Param.GetPlayerPrefs(SaveKey);
            SetValue(param);
        }

        //Save local values manually.
        //･To be saved value is only the 'key & value' pairs (Dictionary <string, string>).
        public void SavePrefs()
        {
            XPlayerPrefs.SetDictionary(SaveKey, GetValue());    //compatible 'Param' class
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

        //Convert the local value to Dictionary <item.key, value>
        //･If saveValue is true, it will be automatically saved.
        public Dictionary<string, string> CurrentValue {
            get { return GetValue(); }
            set {
                SetValue(value);
                if (saveValue)
                    SavePrefs();
            }
        }

        //Local value (dialogItems) -> Dictionary <string, string> (compatible 'Param' class)
        private Dictionary<string, string> GetValue()
        {
            Param param = new Param();
            if (dialogItems != null)
            {
                foreach (var elem in keyToIndex)
                {
                    int i = elem.Value;
                    string type = dialogItems[i].type;
                    if (type == "Switch")
                        param[elem.Key] = ((SwitchItem)dialogItems[i]).defChecked.ToString();
                    else if (type == "Slider")
                        param[elem.Key] = ((SliderItem)dialogItems[i]).value.ToString();
                    else if (type == "Toggle")
                        param[elem.Key] = ((ToggleItem)dialogItems[i]).defValue;
                    else if (type == "Check")
                        param[elem.Key] = ((CheckItem)dialogItems[i]).defChecked.ToString();
                }
            }
            return param;
        }

        //Dictionary <string, string> -> Local value (dialogItems) (compatible 'Param' class)
        //･Note: Nonexistent keys and type mismatch are ignored.
        private void SetValue(Dictionary<string, string> dic)
        {
            if (dic == null || dialogItems == null)
                return;

            foreach (var item in dic)
            {
                if (keyToIndex.ContainsKey(item.Key))
                {
                    int i = keyToIndex[item.Key];
                    try
                    {
                        string type = dialogItems[i].type;
                        if (type == "Switch")
                            ((SwitchItem)dialogItems[i]).defChecked = bool.Parse(item.Value);
                        else if (type == "Slider")
                            ((SliderItem)dialogItems[i]).value = float.Parse(item.Value);
                        else if (type == "Toggle")
                            ((ToggleItem)dialogItems[i]).defValue = item.Value;
                        else if (type == "Check")
                            ((CheckItem)dialogItems[i]).defChecked = bool.Parse(item.Value);
                    }
                    catch (Exception) { }
                }
            }
        }


        //Set items(dialogItems) dynamically (current items will be overwritten)
        //(*) When changed dynamically, it is inconsistent with the value saved in PlayerPrefs (better to use saveValue is false).
        //(*) Empty and duplication are not checked.
        //(*) localization will be incompatible.
        //
        //動的にアイテム（dialogItems）を設定する（現在のアイテムは上書き）
        //※動的にアイテム変更した場合、保存データ（PlayerPrefs）と互換性が無くなるので注意（saveValue オプションはオフで使う方が良い）。
        //※空や重複データはチェックされないので注意。
        //※ローカライズデータは互換性が無くなるので注意。
        public void SetItem(DialogItem[] dialogItems)
        {
            if (dialogItems == null)
                return;

            this.dialogItems = dialogItems;

            keyToIndex.Clear();
            for (int i = 0; i < dialogItems.Length; i++)
            {
                var item = dialogItems[i];
                if (item == null)
                    continue;

                switch (item.type)
                {
                    case "Switch":
                        ((SwitchItem)item).changeCallbackMethod = "ReceiveChanged";
                        break;
                    case "Slider":
                        ((SliderItem)item).changeCallbackMethod = "ReceiveChanged";
                        break;
                    case "Toggle":
                        ((ToggleItem)item).changeCallbackMethod = "ReceiveChanged";
                        break;
                    case "Check":
                        ((CheckItem)item).changeCallbackMethod = "ReceiveChanged";
                        break;
                    default:
                        //Divisor, Text
                        continue;
                }

                if (!string.IsNullOrEmpty(item.key))
                    keyToIndex[item.key] = i;         //same as MakeKeyToIndexTable()
            }
        }


        //Convert items -> dialogItems (Local Values)
        private DialogItem[] dialogItems;

        //The values for reset.
        private DialogItem[] initValue;

        //Inspecter value (DialogItemParameter) -> Local value (DialogItem).
        //(*) When internal processing, it is used dialogItems.
        //･Make 'key -> index' table
        //･Store the values of the inspector (initValue).
        //･Check for key exist.
        private void ConvertToDialogItem()
        {
            if (items == null || items.Length == 0)     //from inspector
                return;

            dialogItems = new DialogItem[items.Length];
            initValue = new DialogItem[items.Length];
            keyToIndex.Clear();
            int keyCount = 0;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;

                switch (item.type)
                {
                    case DialogItemType.Divisor:
                        dialogItems[i] = new DivisorItem(item.lineHeight, item.lineColor);
                        initValue[i] = ((DivisorItem)dialogItems[i]).Clone();
                        break;
                    case DialogItemType.Text:
                        string align = item.align.ToString().ToLower();
                        dialogItems[i] = new TextItem(item.Text, item.textColor, item.backgroundColor, align == "none" ? "" : align);
                        initValue[i] = ((TextItem)dialogItems[i]).Clone();
                        break;
                    case DialogItemType.Switch:
                        dialogItems[i] = new SwitchItem(item.Text, item.key, item.defaultChecked, item.textColor, "ReceiveChanged");
                        initValue[i] = ((SwitchItem)dialogItems[i]).Clone();
                        if (!string.IsNullOrEmpty(item.key))
                            keyToIndex[item.key] = i;
                        keyCount++;
                        break;
                    case DialogItemType.Slider:
                        dialogItems[i] = new SliderItem(item.Text, item.key, item.value, item.minValue, item.maxValue, item.digit, item.textColor, "ReceiveChanged");
                        initValue[i] = ((SliderItem)dialogItems[i]).Clone();
                        if (!string.IsNullOrEmpty(item.key))
                            keyToIndex[item.key] = i;
                        keyCount++;
                        break;
                    case DialogItemType.Toggle:
                        dialogItems[i] = new ToggleItem(item.TogglesTexts, item.key, item.TogglesValues, item.toggleItems[item.checkedIndex].value, item.textColor, "ReceiveChanged");
                        initValue[i] = ((ToggleItem)dialogItems[i]).Clone();
                        if (!string.IsNullOrEmpty(item.key))
                            keyToIndex[item.key] = i;
                        keyCount++;
                        break;
                    case DialogItemType.Check:
                        dialogItems[i] = new CheckItem(item.Text, item.key, item.defaultChecked, item.textColor, "ReceiveChanged");
                        initValue[i] = ((CheckItem)dialogItems[i]).Clone();
                        if (!string.IsNullOrEmpty(item.key))
                            keyToIndex[item.key] = i;
                        keyCount++;
                        break;
                }
            }
#if UNITY_EDITOR
            //Check for errors (Editor only).
            if (keyCount != keyToIndex.Count)
                Debug.LogError("[" + gameObject.name + "] There is empty or duplicate 'Key'.");
#endif
        }

        //Restore the values of the inspector and delete PlayerPrefs key.
        public void ResetValue()
        {
            dialogItems = (DialogItem[])initValue.Clone();

            for (int i = 0; i < initValue.Length; i++)
            {
                if (initValue[i] == null)
                    continue;

                string type = initValue[i].type;
                if (type == "Divisor")
                    dialogItems[i] = ((DivisorItem)initValue[i]).Clone();
                else if (type == "Text")
                    dialogItems[i] = ((TextItem)initValue[i]).Clone();
                else if (type == "Switch")
                    dialogItems[i] = ((SwitchItem)initValue[i]).Clone();
                else if (type == "Slider")
                    dialogItems[i] = ((SliderItem)initValue[i]).Clone();
                else if (type == "Toggle")
                    dialogItems[i] = ((ToggleItem)initValue[i]).Clone();
                else if (type == "Check")
                    dialogItems[i] = ((CheckItem)initValue[i]).Clone();
            }

            MakeKeyToIndexTable();
            DeletePrefs();
        }


        //Detects 'DialogItemParameter' from its 'transform.GetChild()' element (not search for grandchildren.).
        private void FindItemsInChild()
        {
            int len = transform.childCount;
            if (len == 0)
                return;

            List<DialogItemParameter> list = new List<DialogItemParameter>(len);
            for (int i = 0; i < len; i++)
            {
                DialogItemParameter dlgParam = transform.GetChild(i).GetComponent<DialogItemParameter>();
                if (dlgParam != null)
                    list.Add(dlgParam);
            }

            if (list.Count > 0)
                items = list.ToArray();     //overwrite inspector value
        }


        //'key -> index' convert table (Divisor and Text do not have a key)
        private Dictionary<string, int> keyToIndex = new Dictionary<string, int>();

        //Make 'key -> index' table
        private void MakeKeyToIndexTable()
        {
            keyToIndex.Clear();
            for (int i = 0; i < dialogItems.Length; i++)
            {
                if (dialogItems[i] != null && !string.IsNullOrEmpty(dialogItems[i].key))
                    keyToIndex[dialogItems[i].key] = i;
            }
        }


        //Check empty or duplication from item elements.
        private void CheckForErrors()
        {
            if (dialogItems == null || dialogItems.Length == 0)
            {
                Debug.LogWarning("[" + gameObject.name + "] 'Items' is empty.");
            }

            //Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy.
            //Note: Search only within the same type.
            CustomDialogController[] objs = FindObjectsOfType<CustomDialogController>();
            if (objs.Length > 1)
            {
                HashSet<string> set = new HashSet<string>(objs.Select(e => e.gameObject.name).ToArray());
                if (set.Count != objs.Length)
                    Debug.LogError("[" + gameObject.name + "] There is duplicate 'gameObject.name'.");
            }
        }

        //Apply localization of Items.
        //(*) When dynamically changing Items, it is better not to use it because it is incompatible.
        //※動的にItemsを変更する場合は、互換性がないので使わない方が良い。
        private void ApplyLocalizeItems(SystemLanguage language)
        {
            if (dialogItems == null || dialogItems.Length == 0 || items.Length == 0)
                return;

            for (int i = 0; i < items.Length && i < dialogItems.Length; i++)
            {
                var item = items[i];
                if (item == null || dialogItems[i] == null)
                    continue;

                switch (item.type)
                {
                    case DialogItemType.Divisor:
                        continue;
                    case DialogItemType.Text:
                        if (item.localize.localizeResource != null)
                        {
                            var obj = (TextItem)dialogItems[i];
                            obj.text = item.localize.localizeResource.Text(item.localize.textID, language, obj.text);
                            if (initValue != null && i < initValue.Length)
                                ((TextItem)initValue[i]).text = obj.text;
                        }
                        break;
                    case DialogItemType.Switch:
                        if (item.localize.localizeResource != null)
                        {
                            var obj = (SwitchItem)dialogItems[i];
                            obj.text = item.localize.localizeResource.Text(item.localize.textID, language, obj.text);
                            if (initValue != null && i < initValue.Length)
                                ((SwitchItem)initValue[i]).text = obj.text;
                        }
                        break;
                    case DialogItemType.Slider:
                        if (item.localize.localizeResource != null)
                        {
                            var obj = (SliderItem)dialogItems[i];
                            obj.text = item.localize.localizeResource.Text(item.localize.textID, language, obj.text);
                            if (initValue != null && i < initValue.Length)
                                ((SliderItem)initValue[i]).text = obj.text;
                        }
                        break;
                    case DialogItemType.Toggle:
                        if (item.localizeItems.localizeResource != null)
                        {
                            var obj = (ToggleItem)dialogItems[i];
                            var ini = (initValue != null && i < initValue.Length) ? (ToggleItem)initValue[i] : null;
                            var itm = (ini != null && ini.items != null) ? ini.items : null;
                            if (obj.items != null && item.localizeItems.textID != null)
                            {
                                int len = Mathf.Min(obj.items.Length, item.localizeItems.textID.Length);
                                for (int j = 0; j < len; j++)
                                {
                                    obj.items[j] = item.localizeItems.localizeResource.Text(item.localizeItems.textID[j], language, obj.items[j]);
                                    if (itm != null && j < itm.Length)
                                        itm[j] = obj.items[j];
                                }
                            }
                        }
                        break;
                    case DialogItemType.Check:
                        if (item.localize.localizeResource != null)
                        {
                            var obj = (CheckItem)dialogItems[i];
                            obj.text = item.localize.localizeResource.Text(item.localize.textID, language, obj.text);
                            if (initValue != null && i < initValue.Length)
                                ((CheckItem)initValue[i]).text = obj.text;
                        }
                        break;
                }
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

            ApplyLocalizeItems(language);
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            if (findItemsInChild)
                FindItemsInChild();

            ConvertToDialogItem();
            ApplyLocalize();

            if (saveValue)
                LoadPrefs();
        }

        private void Start()
        {
#if UNITY_EDITOR
            CheckForErrors();   //Check for items(dialogItems) (Editor only).
#endif
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        //Show dialog
        public void Show()
        {
            if (dialogItems == null || dialogItems.Length == 0)
                return;
#if UNITY_EDITOR
            Debug.Log("CustomDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowCustomDialog(
                title,
                message,
                dialogItems,
                gameObject.name, "ReceiveResult", "ReceiveCancel",
                false,          //"key=value" format
                okButton, cancelButton,
                style);
#endif
        }


        //Returns value when 'OK' pressed.
        private void ReceiveResult(string result)
        {
            Param param = Param.Parse(result);  //Parse: "key1=value1\nkey2=value2" -> same as Dictionary<key, value> (Param)
            if (param == null)
                return;

            if (saveValue)
            {
                SetValue(param);
                Param.SetPlayerPrefs(SaveKey, param);
                PlayerPrefs.Save();
            }

            if (OnResult != null)
                OnResult.Invoke(param);     //Param and Dictionary are compatible.
        }

        //Returns value when slider, switch, toggle changed
        public void ReceiveChanged(string result)
        {
            if (OnValueChanged != null)
            {
                string[] arr = result.Split('=');
                if (arr.Length >= 2)
                    OnValueChanged.Invoke(arr[0], arr[1]);
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
