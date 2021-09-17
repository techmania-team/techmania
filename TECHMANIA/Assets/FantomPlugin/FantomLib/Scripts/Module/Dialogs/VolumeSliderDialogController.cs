using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace FantomLib
{
    /// <summary>
    /// Volume Slider Dialog Controller
    /// 
    /// Software Volume operation of the AudioMixer in the Slider Dialog
    /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#VolumeSliderDialogController
    ///･Register the exporse parameter name of the AudioMixer and the AudioSource for preview playback as items in the inspector.
    ///･On the Silder Dialog, express the software volume with 0~100 -> convert to AudioMixer: -80~0db
    ///(*) The message character string (message) and the buttons are not displayed when the item does not fit in the dialog (exclusive to the scrolling display).
    ///    In that case, you will only be able to display the button if you lose the message (to empty("")): Android dialog specification(?).
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) When using value save (saveVolume), it is better to give a specific save name (saveKey) individually.
    ///    (By default it is saved as GameObject.name [*using PlayerPrefs], so the same name across the scene, it will be overwritten).
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    /// スライダーダイアログでミキサーの音量操作をするスクリプト
    /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#VolumeSliderDialogController
    ///・インスペクタにミキサーのエクスポーズパラメーター名とプレビュー再生するソースをアイテムとして登録する。
    ///・ダイアログでは音量を 0～100 で表現する（→ ミキサー：-80～0db に変換する）。
    ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）。
    ///  その場合、メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※値の保存（saveVolume）をするときは、なるべく固有の保存名（saveKey）を設定した方が良い
    ///（デフォルトではGameObject名で保存されるため[※PlayerPrefs を利用]、シーンをまたがって同じ名前があると上書きされてしまう）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class VolumeSliderDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public AudioMixer mixer;                        //AudioMixer to control the volume  //ボリュームをコントロールするミキサー

        public string title = "Sound Volume Setting";   //Dialog title    //音量の設定
        [Multiline] public string message = "You can preview play by moving the slider."; //Dialog message (It should be empty when overflowing)   //スライダーを動かすとプレビュー再生できます。
        public Color itemTextColor = Color.black;       //Text color of all items
        public string okButton = "OK";                  //Text of 'OK' button.
        public string cancelButton = "Cancel";          //Text of 'Cancel' button.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert";

        public bool showToast = true;                   //Display on toast when receiving results.  //結果受信時にトーストで表示する。

        public bool saveVolume = true;                  //Save the software volume (PlayerPrefs)    //音量を保存する
        [SerializeField] private string saveKey = "";   //When specifying the PlayerPrefs key.      //固有の保存名（PlayerPrefs のキー）


        //Item parameters for Slider Dialog
        //スライダーのアイテムの情報クラス
        [Serializable]
        public class SliderItem
        {
            public string key;          //Returns key and Expose parameter name used in Slider Dialog   //ダイアログで使用するキー＆エクスポーズパラメーター名（※スライダーで音量コールバックするときには必要）
            public string text;         //Display text in dialog                                        //ダイアログでの表示テキスト
            public AudioSource source;  //AudioSource to preview playback (Set in the inspector)        //プレビュー再生するソース（インスペクタで設定する）
            [Range(0, 100)] public int volume = 50;  //Software volume: 0~100                           //音量：0～100 で表現

            public SliderItem() { }

            public SliderItem(string key, string text, int volume)
            {
                this.key = key;
                this.text = text;
                this.volume = volume;
            }
        }

        //Slider Dialog Items
        //スライダーのアイテムたち
        [SerializeField]
        private SliderItem[] items = new SliderItem[] {
            new SliderItem("master", "Master", 100),    //マスター  
            new SliderItem("bgm", "Music", 50),         //音楽
            new SliderItem("se", "Effect", 50),         //効果音
            new SliderItem("voice", "Voice", 50),       //ボイス
        };


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

#region PlayerPrefs Section

        //PlayerPrefs Key (It is used only when saveKey is empty)
        const string VOLUME_PREF = "_volume";       //add name (PlayerPrefs)

        //Saved key in PlayerPrefs
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + VOLUME_PREF : saveKey; }
        }

        //Set a software volume to PlayerPrefs (*) It does not affect the current software volume
        //外部からの設定保存（保存された PlayerPrefs のみ更新：現在の音量には影響しない）
        public void SetPrefs(Dictionary<string, int> pref)
        {
            if (pref != null && pref.Count > 0)
                XPlayerPrefs.SetDictionary(SaveKey, pref);
        }

#endregion

#region Properties and Local values Section

        //Generate arrays to be arguments of Slider Dialog
        //ダイアログの引数にする配列たちを生成する
        private void GetItemArrays(out string[] keys, out string[] texts, out float[] volumes, out int[] revert)
        {
            keys = new string[items.Length];
            texts = new string[items.Length];
            volumes = new float[items.Length];
            revert = new int[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                keys[i] = items[i].key;
                texts[i] = items[i].text;
                volumes[i] = revert[i] = items[i].volume;
            }
        }

        //key -> SliderItem
        //key → オブジェクトを引く辞書
        private Dictionary<string, SliderItem> dic = new Dictionary<string, SliderItem>();

        //For reset (Save the value of inspector at startup)
        //リセット用（起動時のインスペクタの値を保存しておく）
        private Dictionary<string, int> initVolumes = new Dictionary<string, int>(); //key, volume

        //Set a software volume (Convert to db)
        //音量の設定（デシベル変換）
        //volume: 0~100 -> -80~0db (AudioMixer)
        public void SetVolume(string key, float volume)
        {
            //Convert to db   //デシベル変換
            float val = Mathf.Clamp(volume / 100f, 0.0001f, 1.0f);
            float db = 20 * Mathf.Log10(val);
            mixer.SetFloat(key, Mathf.Clamp(db, -80.0f, 0.0f));

            //Store to local  //内部にも保持
            if (dic.ContainsKey(key))
                dic[key].volume = (int)Mathf.Clamp(volume, 0, 100);
        }

        //Get software volumes
        //音量の取得
        public Dictionary<string, int> GetVolumes()
        {
            return items.ToDictionary(e => e.key, e => e.volume);
        }

        //Reset software volume -> Restore the value (defVolumes) and delete PlayerPrefs
        //音量を初期状態（インスペクタで設定した値）に戻す。保存データを消去
        public void ResetVolumes()
        {
            foreach (var item in initVolumes)
                SetVolume(item.Key, item.Value);

            PlayerPrefs.DeleteKey(SaveKey);
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
                }
            }
        }

        //Specify language and apply (update) localized string
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
                }
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            ApplyLocalize();    //Localize text

            //Load the software volume
            //保存された音量の読み込み
            Dictionary<string, int> pref = null;
            if (saveVolume)
                pref = XPlayerPrefs.GetDictionary<string, int>(SaveKey);  //nothing -> null  //無いとき=null;

            foreach (var item in items)
            {
                dic[item.key] = item;   //Register in dictionary with key   //キーで辞書に登録
                initVolumes[item.key] = item.volume;  //Save the value of inspector at startup  //起動時のインスペクタをデフォとする

                //Update stored saved volume    //保存された音量があったら更新
                if (pref != null && pref.ContainsKey(item.key))
                    item.volume = pref[item.key];

                SetVolume(item.key, item.volume);
            }
        }

        private void Start()
        {
            
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Preview playback (Ignored if the key is not found or already played)
        //プレビュー再生（キーが見つからない/既に再生されている場合は無視される）
        public void Play(string key)
        {
            if (dic.ContainsKey(key))
            {
                AudioSource src = dic[key].source;
                if (src != null && !src.isPlaying)
                    src.Play();
            }
        }

        //Stop preview (Ignored if the key is not found or already stopped)
        //プレビュー停止（キーが見つからない/既に停止している場合は無視される）
        public void Stop(string key)
        {
            if (dic.ContainsKey(key))
            {
                AudioSource src = dic[key].source;
                if (src != null && src.isPlaying)
                    src.Stop();
            }
        }

#pragma warning disable 0649 //'revertVolumes' is always null, but actually it is used on the Android platform. 
        //When cancel/closed to revert
        private int[] revertVolumes;

        //Call Adroid Slider Dialog for software volume
        //音量用のダイアログを開く
        public void OpenVolumeDialog()
        {
#if UNITY_EDITOR
            Debug.Log("VolumeSliderDialogController.OpenVolumeDialog called");
#elif UNITY_ANDROID
            string[] keys; string[] texts; float[] volumes;
            GetItemArrays(out keys, out texts, out volumes, out revertVolumes);
            AndroidPlugin.ShowSliderDialog(
                title, message, 
                texts, keys, volumes, null, null, null, itemTextColor, 
                gameObject.name, "ReceiveVolume", "PreviewVolume", "ReceiveCancel",
                okButton, cancelButton, style);
#endif
        }

        //Alias (overload)
        public void Show()
        {
            OpenVolumeDialog();
        }


        //When "OK", the setting completion callback handler
        //設定完了（「OK」時）のコールバックハンドラ
        private void ReceiveVolume(string message)
        {
#if UNITY_EDITOR
            Debug.Log("ReceiveVolume : " + message);
#endif
            if (!string.IsNullOrEmpty(message))
            {
                Dictionary<string, int> pref = new Dictionary<string, int>();   //For save   //音量の保存用（アイテムキーと音量のペアで保存）
                string[] arr = message.Split('\n');
                string str = "";    //for toast message
                for (int i = 0; i < arr.Length && i < items.Length; i++)
                {
                    string[] param = arr[i].Split('=');
                    items[i].volume = (param.Length > 1) ? int.Parse(param[1]) : int.Parse(param[0]);
                    pref[items[i].key] = items[i].volume;   //item key and software volume pair     //アイテムキーと音量のペア
                    str += (i > 0 ? "\n" : "") + items[i].text + " : " + items[i].volume;
                }

                if (showToast)
                {
#if !UNITY_EDITOR && UNITY_ANDROID
                    AndroidPlugin.ShowToast(str);
#endif
                }

                if (saveVolume && pref.Count > 0)
                {
                    SetPrefs(pref);     //Save the volume with item key and volume pair   //アイテムのキーと音量のペアで音量を保存
                    PlayerPrefs.Save();
                }
            }
        }

        //Preview playback callback handler ('key' required)
        //プレビュー再生コールバックハンドラ（キーが必要）
        private void PreviewVolume(string message)
        {
#if UNITY_EDITOR
            Debug.Log("PreviewVolume : " + message);
#endif
            if (!string.IsNullOrEmpty(message))
            {
                string[] param = message.Split('=');    //"key=value" format only   //key=value の形式
                if (param.Length > 1)
                {
                    //Select AudioSource from the key
                    //スライダーのキーから AudioSource を選択
                    string key = param[0];
                    Play(key);

                    //Set a software volume
                    //音量設定
                    float vol = float.Parse(param[1]);
                    SetVolume(key, vol);
                }
            }
        }

        //When cancel or closed. Revert volumes.
        private void ReceiveCancel(string message)
        {
            if (message != "CANCEL_DIALOG" && message != "CLOSE_DIALOG")
                return;

            if (revertVolumes != null)
            {
                for (int i = 0; i < revertVolumes.Length; i++)
                    SetVolume(items[i].key, revertVolumes[i]);
            }
        }
    }

}