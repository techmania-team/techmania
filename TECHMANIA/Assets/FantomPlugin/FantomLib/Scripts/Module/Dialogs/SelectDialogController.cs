using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Select Dialog Controller
    /// 
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class SelectDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";              //Dialog title

        [Serializable]
        public class Item
        {
            public string text = "";                //Text for each item
            public string value = "";               //Value for each item

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
        [SerializeField] private Item[] items;      //Choices

        //Callback value type
        [Serializable] public enum ResultType { Index, Text, Value }
        public ResultType resultType = ResultType.Value;

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";

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

#region Properties and Local values Section

        //Propeties
        public string[] Texts {
            get { return items.Select(e => e.text).ToArray(); }
        }

        public string[] Values {
            get { return items.Select(e => e.value).ToArray(); }
        }


        //Set items dynamically (current items will be overwritten)
        //(*) When the resultType is 'Value', the value becomes the index of string type.
        //(*) Empty and duplication are not checked.
        //(*) localization will be incompatible.
        //
        //動的にアイテムを設定する（現在のアイテムは上書き）
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
        }

        //Set items dynamically (current items will be overwritten)
        //(*) Empty and duplication are not checked.
        //(*) localization will be incompatible.
        //
        //動的にアイテムを設定する（現在のアイテムは上書き）
        //※空や重複データはチェックされないので注意。
        //※ローカライズデータは互換性が無くなるので注意。
        public void SetItem(Item[] items)
        {
            if (items == null)
                return;

            this.items = items;
        }


        //The value for reset.
        private Item[] initValue;

        //Store the values of the inspector.
        private void StoreInitValue()
        {
            initValue = (Item[])items.Clone();
            for (int i = 0; i < items.Length; i++)
                initValue[i] = items[i].Clone();
        }

        //Restore the value of inspector.
        public void ResetValue()
        {
            items = (Item[])initValue.Clone();
            for (int i = 0; i < initValue.Length; i++)
                items[i] = initValue[i].Clone();
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
            SelectDialogController[] objs = FindObjectsOfType<SelectDialogController>();
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
            StoreInitValue();
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

        
        //Show dialog with local values
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("SelectDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowSelectDialog(
                title,
                Texts,
                gameObject.name,
                "ReceiveResult",
                true,   //For internal processing, only uses Index type.
                style);
#endif
        }

        //Set items dynamically and show dialog (current items will be overwritten)
        //Note: When the resultType is 'Value', the value becomes the index of string type.
        //Note: Empty and duplication are not checked.
        public void Show(string[] texts)
        {
            SetItem(texts);
            Show();
        }


        //Returns value when choiced item.
        private void ReceiveResult(string result)
        {
            int index;      //For internal processing, only uses Index type.
            if (!int.TryParse(result, out index))
                return;

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
    }
}
