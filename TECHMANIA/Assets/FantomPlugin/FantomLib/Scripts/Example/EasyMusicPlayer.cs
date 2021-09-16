using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FantomLib.Example
{
    /// <summary>
    /// Easy Music Player
    /// 
    /// A sample that realizes simple music player function by using information acquisition of audio file and dialogs.
    ///·For 'Next()', 'Prev()', the playback mode is 'one song loop' / 'continuous playback' is list order, and when it is 'shuffle' it is randomly arranged.
    ///·Shuffle table is initialized when there are changes such as playback mode, setting options, song deletion etc.
    ///·Preset songs are not included in saving playlist. Preset songs are added to the playlist at startup.
    /// 
    ///　オーディオファイルの情報取得とダイアログを利用して、簡単な音楽プレイヤー機能を実現するサンプル。
    ///・Next(), Prev() は再生モード（mode）が1曲ループ/連続再生はリスト順、シャッフルのときはランダムな並びになる。
    ///・再生モード、設定オプション、曲の削除など変化があった場合、シャッフルテーブルは初期化される。
    ///・プレイリスト保存にはプリセット曲は含まれない。プリセット曲は起動時にプレイリストに追加される。
    /// </summary>
    public class EasyMusicPlayer : MonoBehaviour, ILocalizable
    {
#region Inspector settings Section

        //Inspector Settings
        [Serializable]
        public enum Mode
        {
            Loop,       //1 song loop                       //1曲ループ
            Next,       //Sequentially in the playlist      //連続再生
            Shuffle,    //Shuffle order in the playlist     //シャッフル
        }

        [SerializeField] private Mode mode = Mode.Loop;     //Playback mode
        
        [SerializeField] private bool exceptPresets = false;        //When Next or Shuffle mode, exclude preset songs.      //連続再生、シャッフルモードのとき、プリセット曲を含める。
        [SerializeField] private bool autoPlaylistOnError = true;   //Automatically remove from playlist when song errors.  //曲のエラー時にリストから自動的に削除する。
        [SerializeField] private bool autoSettingOnError = true;    //Automatically correct setting errors.                 //設定のエラーを自動的に修正する。

        public bool playOnAwake = false;                    //Play song at startup

        public Text displayTitle;                           //Display song title
        public string titlePrefix = "♪ ";                   //Dispalay "Prefix + Title"
        public Text displayArtist;                          //Display song artist
        public string artistUnknown = "<Unknown>";          //Alternate name when empty

        [SerializeField] private string[] mimeTypes;        //MIME types for storage

        //Key operation (Mainly for debug)
        public bool useKey = false;                         //Use key operation
        public KeyCode nextKey = KeyCode.PageUp;            //Play next song in playlist
        public KeyCode prevKey = KeyCode.PageDown;          //Play prev song in playlist


        public bool saveSetting = true;                     //Save setting
        public bool savePlaylist = true;                    //Save playlist
        [SerializeField] private string saveKey = "";       //When specifying the PlayerPrefs key.

        public AudioSource audioSource;

        [Serializable]
        public class PlayItem
        {
            public AudioClip clip;
            public string path;         //also key ("#~" = preset song, "/storage/~" = added song)
            public string title;
            public string artist;

            public PlayItem() { }

            public PlayItem(string path, string title, string artist)
                : this (null, path, title, artist) { }

            public PlayItem(AudioClip clip, string path, string title, string artist)
            {
                this.clip = clip;
                this.path = path;
                this.title = title;
                this.artist = artist;
            }

            public override string ToString()
            {
                return JsonUtility.ToJson(this);    //For debug
            }
        }

        public List<PlayItem> presets;      //Preset songs (internal songs)
        private List<PlayItem> playlist;    //Customizable playlist at runtime (including preset songs at startup)


        //Callbacks
        [Serializable] public class SongChangedHandler : UnityEvent<PlayItem> { }
        public SongChangedHandler OnSongChanged;

        [Serializable] public class SongIntChangedHandler : UnityEvent<int> { }
        public SongIntChangedHandler OnSongIntChanged;

        [Serializable] public class PlaybackModeChangedHandler : UnityEvent<Mode> { }
        public PlaybackModeChangedHandler OnPlaybackModeChanged;

        [Serializable] public class PlaybackModeIntChangedHandler : UnityEvent<int> { } //Converted 'Mode' to int type
        public PlaybackModeIntChangedHandler OnPlaybackModeIntChanged;

        //Callback when error.                  //エラー時のコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }    //error message
        public ErrorHandler OnError;

#endregion Inspector settings Section

#region PlayerPrefs Section

        const string SETTING_PREF = "_setting";
        const string PLAYLIST_PREF = "_playlist";

        public string SaveSettingKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + SETTING_PREF : saveKey + SETTING_PREF; }
        }

        public string SavePlaylistKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + PLAYLIST_PREF : saveKey + PLAYLIST_PREF; }
        }

        //設定のロード
        public void LoadSetting()
        {
            Param param = Param.GetPlayerPrefs(SaveSettingKey);
            if (param != null)
            {
                index = Mathf.Clamp(param.GetInt(KEY_SONG, index), 0, Mathf.Max(0, playlist.Count - 1)); //(*) It needs to be initialized with InitPlaylist() first.  //※先に InitPlaylist() で初期化されている必要がある。
                exceptPresets = param.GetBool(KEY_EXCEPT_PRESETS, exceptPresets);
                autoPlaylistOnError = param.GetBool(KEY_AUTO_PLAYLIST, autoPlaylistOnError);
                autoSettingOnError = param.GetBool(KEY_AUTO_SETTING, autoSettingOnError);
                SetMode(param.GetString(KEY_MODE, mode.ToString()));    //(*) Here the state is updated. //※ここで状態が更新される。
            }
        }

        //設定の保存
        public void SaveSetting()
        {
            Param param = new Param();
            param.Set(KEY_SONG, index);
            param.Set(KEY_MODE, mode);  //to string
            param.Set(KEY_EXCEPT_PRESETS, exceptPresets);
            param.Set(KEY_AUTO_PLAYLIST, autoPlaylistOnError);
            param.Set(KEY_AUTO_SETTING, autoSettingOnError);
            Param.SetPlayerPrefs(SaveSettingKey, param);
            PlayerPrefs.Save();
        }

        //Save the playlist as an array (JSON data are saved).
        //プレイリストを配列として保存する（保存されるのはJSONデータ）
        private void SavePlaylist()
        {
            XPlayerPrefs.SetArray(SavePlaylistKey, playlist.Where((e, i) => i >= presets.Count).ToArray());  //Exclude preset songs
            PlayerPrefs.Save();
        }

        //Loading and initializing playlists
        //･Internal songs (presets) have "#key" in place of path.)
        //･If the original data is empty, the key is prefixed with '#' + index number or '#' at the beginning of the existing character string (keys can not be duplicated).
        //･Internal songs (presets) are always added to the playlist.
        //･Something with a "#" at the beginning of the saved path is always ignored (because it is confused with the preset song, not usually).
        //
        //プレイリストのロードと初期化
        //・内部の楽曲（presets）は path の代わりに "#key" を持たせておく。
        //・キーは元のデータが空の場合、'#'+インデクス番号、または既存の文字列の先頭に '#' が付加される（キーは重複不可）。
        //・常に内部の楽曲（presets）はプレイリストに追加される。
        //・保存されているパスの頭に"#"が付いているものは常に無視される（プリセット曲と混同するため。通常は無い）。
        private void InitPlaylist()
        {
            if (playlist == null)
                playlist = new List<PlayItem>();

            playlist.Clear();

            //Add preset songs
            for (int i = 0; i < presets.Count; i++)
            {
                string key = presets[i].path;
                if (string.IsNullOrEmpty(key))
                    key = "#" + i;      //like "#0", "#1", ...
                if (key[0] != '#')
                    key = "#" + key;    //like "#key"

                playlist.Add(new PlayItem(presets[i].clip, key, presets[i].title, presets[i].artist));
            }

            //Add saved playlist
            PlayItem[] items = XPlayerPrefs.GetArray<PlayItem>(SavePlaylistKey);
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    string path = items[i].path;
                    if (!string.IsNullOrEmpty(path) && path[0] != '#')  //Ignore "#~" songs in the saved playlist (Normaly not)
                    {
                        items[i].clip = null;
                        playlist.Add(items[i]);
                    }
                }
            }
        }

#endregion PlayerPrefs Section

#region Properties and Local values Section

        private string DefaultMimeType {
            get { return AndroidMimeType.Audio.All; }
        }

        //Multiple MIME type specifications.
        //(*) Note that valid MIME type by provider (storage) is different and may not apply.
        //
        //複数の MIME type 指定。
        //※プロバイダ（ストレージ）によって有効な MIME type は異なり、適用されない場合もあるので注意。
        public string[] MimeTypes {
            get {
                if (mimeTypes == null || mimeTypes.Length == 0)
                    mimeTypes = new string[] { DefaultMimeType };

                return mimeTypes;
            }
            set {
                if (value != null && value.Length > 0)
                    mimeTypes = value;
                else
                    mimeTypes = new string[] { DefaultMimeType };
            }
        }

        //Set up as a single MIME type. However, it is internally managed by array.
        //(*) Acquisition returns [0]. When multiple MIME types are set, it is desirable that [0] is the main one.
        //(*) Note that valid MIME type by provider (storage) is different and may not apply.
        //
        //単一で MIME type 設定。ただし、内部では配列で管理される。
        //※取得は [0] が返される。複数の MIME type がセットされてる場合、[0] が主要なものであることが望ましい。
        //※プロバイダ（ストレージ）によって有効な MIME type は異なり、適用されない場合もあるので注意。
        public string MimeType {
            get {
                return MimeTypes[0];
            }
            set {
                string mimeType = value;
                if (string.IsNullOrEmpty(mimeType))
                    mimeType = DefaultMimeType;

                if (mimeTypes != null && mimeTypes.Length == 1)
                    mimeTypes[0] = mimeType;
                else
                    mimeTypes = new string[]{ mimeType };
            }
        }


        //Current index in playlist (When illegal = -1)
        private int index = 0;

        //Current index in playlist (When illegal = -1)
        //現在のプレイリスト内でのインデクス（不正のとき = -1）
        public int CurrentIndex {
            get { return index; }
            set {
                Play(value);
            }
        }

        //Is it playing?
        //再生中かどうか？
        public bool IsPlaying {
            get { return (audioSource != null && audioSource.isPlaying); }
        }

        //Whether the index is in a valid range for the playlist.
        //インデクスがプレイリストの有効な範囲にあるか？
        private bool IsValidIndex(int idx) {
            return (playlist != null && 0 <= idx && idx < playlist.Count);
        }

        //Whether the index is in a valid range for the presets.
        //インデクスがプリセットの有効な範囲にあるか？
        private bool IsPresetIndex(int idx) {
            return (presets != null && 0 <= idx && idx < presets.Count);
        }

        //Whether the AudioClip of the index is valid (Not null? Mainly used for preset song judgment)
        //インデクスの AudioClip が有効（null 以外）か？（主にプリセット曲判別に使う）
        private bool IsValidClip(int idx)
        {
            return (IsValidIndex(idx) && playlist[idx].clip != null);
        }

        //Get index by the path (key) from 'playlist'. (Forward sequential search)
        //･"/storage/~" (Preset songs have "#~" in place of path.)
        //
        //プレイリストから、パス(キー)でインデクス取得（前方逐次検索）
        //・プリセット曲は path の代わりに "#~" を持たせてある。
        private int IndexOfPlaylist(string path)
        {
            if (playlist == null)
                return -1;  //nothing

            for (int i = 0; i < playlist.Count; i++)    //(*) Because it is a sequential search, when there are a large number, efficiency is not good, attention. //※逐次検索のため、大量にある場合は効率が良くないので注意。
            {
                if (path == playlist[i].path)
                    return i;
            }
            return -1;  //not found
        }

        //Get index by the path (key) from 'playlist'. (Backward sequential search)
        //･"/storage/~" (Preset songs have "#~" in place of path.)
        //
        //プレイリストから、パス(キー)でインデクス取得（後方逐次検索）
        //・プリセット曲は path の代わりに "#~" を持たせてある。
        private int LastIndexOfRangePlaylist(string path, int from = -1, int to = -1)   //It must be 'from >= to'.
        {
            if (playlist == null)
                return -1;  //nothing

            int sidx = (0 <= from && from < playlist.Count) ? from : playlist.Count - 1;
            int eidx = (0 <= to && to < playlist.Count) ? to : 0;
            for (int i = sidx; i >= eidx; i--)    //(*) Because it is a sequential search, when there are a large number, efficiency is not good, attention. //※逐次検索のため、大量にある場合は効率が良くないので注意。
            {
                if (path == playlist[i].path)
                    return i;
            }
            return -1;  //not found
        }

        //Generate arrays of keys and titles from 'playlist'.
        //プレイリストからキーとタイトルの配列を生成する
        private void GetPlaylistKeyAndTitleArrays(out string[] keys, out string[] titles, bool addPresets = true)
        {
            int len = addPresets ? playlist.Count : (playlist.Count - presets.Count);
            keys = new string[len];     //path
            titles = new string[len];

            int sidx = addPresets ? 0 : presets.Count;
            for (int i = sidx, p = 0; i < playlist.Count; i++, p++)
            {
                keys[p] = playlist[i].path;
                titles[p] = playlist[i].title;
            }
        }

        //'Next' or 'Shuffle' mode also includes preset songs [* Update only when there is change and save setting]
        //連続再生, シャッフルモードのときプリセット曲も含む [※変化があったときのみ更新と設定保存]
        public bool IsExceptPresets {
            get { return exceptPresets; }
            set {
                if (exceptPresets != value)
                {
                    exceptPresets = value;
                    InitShuffleTable();

                    if (saveSetting)
                        SaveSetting();
                }
            }
        }

        //First index to be played back (Mainly used in 'Next' or 'Shuffle' mode)
        //再生対象の最初のインデクス（主に 'Next' または 'Shuffle' モードで使う）
        private int TargetSongFirstIndex
        {
            get { return exceptPresets ? presets.Count : 0; }
        }

        //Number of songs to be played back (Mainly used in 'Next' or 'Shuffle' mode)
        //再生対象の曲数（主に 'Next' または 'Shuffle' モードで使う）
        private int TargetSongCount
        {
            get { return exceptPresets ? Mathf.Max(0, playlist.Count - presets.Count) : playlist.Count; }
        }


        //Playback mode setting (Property) [* Update only when there is a change]
        //再生モードの設定（プロパティ）[※変化があったときのみ更新]
        public Mode PlaybackMode {
            get { return mode; }
            set {
                if (mode != value)
                    SetMode(value);
            }
        }

        //Playback mode setting (Specify by int type [index]) [* Update only when there is a change]
        //再生モードの設定（int 型 [インデクス] で指定する）[※変化があったときだけ更新]
        public void SetPlaybackMode(int idx)
        {
            PlaybackMode = (Mode)Enum.ToObject(typeof(Mode), idx);
        }

        //Playback mode setting (Specify by string type) [* Update only when there is a change]
        //再生モードの設定（文字列型で指定する）[※変化があったときだけ更新]
        private void SetPlaybackMode(string str)
        {
            PlaybackMode = (Mode)Enum.Parse(typeof(Mode), str);
        }

        //Playback mode setting [* Always update]
        //再生モードの設定 [※常に更新]
        private void SetMode(Mode md)
        {
            if (mode != md)
            {
                mode = md;

                if (OnPlaybackModeChanged != null)
                    OnPlaybackModeChanged.Invoke(mode);
                if (OnPlaybackModeIntChanged != null)
                    OnPlaybackModeIntChanged.Invoke((int)mode);

                if (saveSetting)
                    SaveSetting();
            }

            if (mode == Mode.Loop)
            {
                StopSongFinishCheck();

                if (audioSource != null)
                    audioSource.loop = true;
            }
            else
            {
                if (audioSource != null)
                    audioSource.loop = false;

                StartSongFinishCheck();
            }

            InitShuffleTable();
        }

        //Playback mode setting (string character overload)
        //再生モードの設定（文字列のオーバーロード）
        private void SetMode(string str)
        {
            SetMode((Mode)Enum.Parse(typeof(Mode), str));
        }


        //Reset settings and delete saved data (*'index' remains as it is)
        //設定のリセットと保存データの削除（※ただし index はそのまま）
        private void ResetSetting()
        {
            exceptPresets = initExceptPresets;
            autoPlaylistOnError = initAutoPlaylistOnError;
            autoSettingOnError = initAutoSettingOnError;
            SetMode(initMode);      //Update here
            PlayerPrefs.DeleteKey(SaveSettingKey);
        }

        //Reset playlist and delete saved data
        //プレイリストのリセットと保存データの削除
        private void ResetPlaylist()
        {
            if (!IsPresetIndex(index))
                Stop();

            PlayerPrefs.DeleteKey(SavePlaylistKey);
            InitPlaylist();
            InitShuffleTable();

            Play(IsPresetIndex(index) ? index : 0);
        }

        //Reset playlist and settings (use this when both resetting)
        //プレイリストと設定のリセット（両方リセットする時にはこれを使う）
        private void ResetPlaylistAndSetting()
        {
            if (!IsPresetIndex(index))
                Stop();

            PlayerPrefs.DeleteKey(SavePlaylistKey);
            InitPlaylist();
            ResetSetting();     //Update here

            Play(IsPresetIndex(index) ? index : 0);
        }

        //The values for reset.
        private Mode initMode;
        private bool initExceptPresets;
        private bool initAutoPlaylistOnError;
        private bool initAutoSettingOnError;

        //Store the values of the inspector.
        private void StoreInitValue()
        {
            initMode = mode;
            initExceptPresets = exceptPresets;
            initAutoPlaylistOnError = autoPlaylistOnError;
            initAutoSettingOnError = autoSettingOnError;
        }


        //Check empty or other errors.
        private void CheckForErrors()
        {
            if (!exceptPresets && presets.Count == 0)
            {
                //Since it is reset to preset songs, such as file reading failed / setting error etc.
                //ファイルの読み込み失敗/設定エラーのときなどプリセット曲に再設定されることがあるため
                Debug.LogWarning("Preset songs is empty (It is better to have one song as possible).");
            }
            else
            {
                HashSet<string> set = new HashSet<string>();  //duplicate check
                for (int i = 0; i < playlist.Count; i++)
                    set.Add(playlist[i].path);

                if (set.Count != playlist.Count)
                    Debug.LogError("There are duplicate keys (path) in the 'presets' or 'playlist'.");
            }
        }

#endregion Properties and Local values Section

#region Unity life cycle section

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                SaveSetting();
        }

        private void OnApplicationQuit()
        {
            SaveSetting();
        }


        // Use this for initialization
        private void Awake()
        {
            if (localize == null)
                localize = new LocalizeData();

            StoreInitValue();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;

            if (mimeTypes == null || mimeTypes.Length == 0)
                MimeType = DefaultMimeType;
        }

        private void Start()
        {
            InitPlaylist();

            if (saveSetting)
                LoadSetting();

            if (playOnAwake)
                Play();

#if UNITY_EDITOR
            CheckForErrors();   //Check empty or other errors (Editor only)
#endif
        }

        // Update is called once per frame
        private void Update()
        {
            if (useKey)
            {
                if (Input.GetKeyUp(nextKey))
                {
                    Next();
                    return;
                }
                if (Input.GetKeyUp(prevKey))
                {
                    Prev();
                    return;
                }
            }

            switch (mode)
            {
                case Mode.Loop:
                    break;

                case Mode.Next:
                case Mode.Shuffle:
                    if (IsSongFinished())
                    {
                        StopSongFinishCheck();
                        Next();
                    }
                    break;
            }
        }

#endregion Unity life cycle section

#region Music Player Section

        bool isFinishChecking = false;    //Checking end of song

        //Note that it can not be detected unless audioSource.loop = false.
        //※audioSource.loop = false でないと検出できないことに注意。
        private bool IsSongFinished()
        {
            return (isFinishChecking && audioSource.clip != null
                && !audioSource.isPlaying && audioSource.timeSamples == 0);
        }

        //Start of music end detection (* music must already be played)
        //楽曲終了検出開始（※曲が既に再生されている必要がある）
        private void StartSongFinishCheck()
        {
            if (IsPlaying)
                isFinishChecking = true;
        }

        //Stop detection of song completion
        //楽曲終了検出停止
        private void StopSongFinishCheck()
        {
            isFinishChecking = false;
        }

        //Index of shuffled songs (Index only for playback target range).
        //シャッフルされた曲のインデクス（再生対象のみのインデクス）
        List<int> shuffleTable;
        
        //Position in the current shuffle table (Shuffle again when you go to the end).
        //現在のシャッフルテーブルでの位置（最後まで行くと再度シャッフルされる）
        int shufflePtr = 0;

        //Initialize the music sequence table (index number) for shuffling
        //(*) When adding / deleting a song, changing play mode, etc., it is necessary to regenerate.
        //シャッフル用の曲順テーブル（インデクス番号）を初期化する
        //※曲の追加・削除、再生モードの変更されたときなどは、必ず再生成する必要がある。
        private void InitShuffleTable(int idx = -1)     //idx: Search inside the shuffleTable and set shufflePtr / -1: Search the current index / int.MinValue: Not search (to be shufflePtr = -1)
        {
            if (mode != Mode.Shuffle)
            {
                shuffleTable = null;
                return;
            }

            if (shuffleTable == null)
                shuffleTable = new List<int>();

            shuffleTable.Clear();

            for (int i = TargetSongFirstIndex; i < playlist.Count; i++)
                shuffleTable.Add(i);

            Shuffle(shuffleTable);
            if (idx == int.MinValue)    //Do not search the current index
                shufflePtr = -1;        //next to be always 0
            else
                shufflePtr = shuffleTable.IndexOf(idx >= 0 ? idx : index);   //not found = -1 (next to be always 0)
        }

        //Move the 'shufflePtr' forward in the 'shuffleTable' (shuffle the table again when it goes to the end)
        //shuffleTable 内で、shufflePtr を次へ進ませる（最後まで行ったら、再度テーブルをシャッフルする）
        private void ShuffleNext()
        {
            if (AutoSettingCheckAndExecute())
                return;

            if (shuffleTable == null || shuffleTable.Count == 0)
                return;

            shufflePtr = Mathf.Max(0, ++shufflePtr);
            if (shufflePtr >= shuffleTable.Count)
            {
                int last = shuffleTable.Last();
                Shuffle(shuffleTable);

                //If the end of the previous table and the beginning of the table shuffled become the same song, change it randomly (so that it does not repeat).
                //以前のテーブルの最後とシャッフルしたテーブルの先頭が同じ曲になった場合、ランダムで入れ替える（繰り返しに聴こえないように）
                if (shuffleTable.Count > 2 && last == shuffleTable[0])  //Only when there are 3 or more songs   //３曲以上あるときのみ
                {
                    int r = Random.Range(1, shuffleTable.Count);    //[1]~[n-1]
                    Swap(shuffleTable, 0, r);
                }

                shufflePtr = 0;
            }

            Play(shuffleTable[shufflePtr], 1);
        }

        //Move the 'shufflePtr' backward in the 'shuffleTable'
        //shuffleTable 内で、shufflePtr を前へ進ませる
        private void ShufflePrev()
        {
            if (AutoSettingCheckAndExecute())
                return;

            if (shuffleTable == null || shuffleTable.Count == 0)
                return;

            shufflePtr = Repeat(--shufflePtr, 0, shuffleTable.Count);
            Play(shuffleTable[shufflePtr], -1);
        }


        //Mainly from the outside "Forward" operation
        //主に外部からの「次へ」操作
        public void Next()
        {
            if (AutoSettingCheckAndExecute())
                return;

            if (mode == Mode.Shuffle)
                ShuffleNext();
            else
                Next(index);
        }

        //Specify index + 1 in the playlist range (Beyond the last, go to the first).
        //(*) It does not come here when shuffling.
        //
        //プレイリスト範囲内で指定インデクス +1 する（最後の次は最初に戻る）
        //※シャッフル時はここには来ない
        private void Next(int idx)      //idx: current index
        {
            Play(Repeat(idx + 1, mode == Mode.Loop ? 0 : TargetSongFirstIndex, playlist.Count), 1);
        }

        //Mainly from the outside "Backward" operation
        //主に外部からの「前へ」操作
        public void Prev()
        {
            if (AutoSettingCheckAndExecute())
                return;

            if (mode == Mode.Shuffle)
                ShufflePrev();
            else
                Prev(index);
        }

        //Specify index - 1 in the playlist range (Below the first, go to the last).
        //(*) It does not come here when shuffling.
        //
        //プレイリスト範囲内で指定インデクス -1 する（最初の次は最後になる）
        //※シャッフル時はここには来ない
        private void Prev(int idx)      //idx: current index
        {
            Play(Repeat(idx - 1, mode == Mode.Loop ? 0 : TargetSongFirstIndex, playlist.Count), -1);
        }


        //Mainly from the outside "Playback" operation
        //主に外部からの「再生」操作
        public void Play()
        {
            Play(index);
        }

        private void Play(string key, int dir = 0)   //key is same as path
        {
            Play(IndexOfPlaylist(key), dir);
        }

        public void Play(int idx, int dir = 0)  //dir: 1=next, 0=stay, -1:prev (Mainly next direction at error occurrence)
        {
            if (!IsValidIndex(idx))
                return;

            if (playlist[idx].clip == null)
                StartCoroutine(LoadAudio(playlist[idx].path, idx, LoadAudioComplete, dir));
            else
                PlayClip(idx);
        }

        int retry = 0;      //load fail count

        //When Load is successful, play AudioClip
        //ロードが成功したとき、AudioClipを再生する
        private void PlayClip(int idx)
        {
            if (!IsValidIndex(idx))
                return;

            if (playlist[idx].clip == null || audioSource == null)
                return;

            if (!audioSource.isPlaying || audioSource.clip != playlist[idx].clip)
            {
                Stop();  //Here 'StopSongFinishCheck()' is executed    //ここで StopSongFinishCheck() が実行される
                audioSource.clip = playlist[idx].clip;
                audioSource.Play();

                if (mode != Mode.Loop)
                    StartSongFinishCheck();

                index = idx;    //For callback sync
                if (OnSongChanged != null)
                    OnSongChanged.Invoke(playlist[index]);
                if (OnSongIntChanged != null)
                    OnSongIntChanged.Invoke(index);
            }

            index = idx;
            retry = 0;
            DisplayInfo();

            if (saveSetting)
                SaveSetting();  //save index
#if UNITY_EDITOR
            Debug.Log("PlayClip : index = " + index + ", title = " + playlist[index].title + ", artist = " + playlist[index].artist);
#endif
        }

        //Stop playback
        public void Stop()
        {
            if (IsPlaying)
            {
                StopSongFinishCheck();
                audioSource.Stop();

                if (IsValidIndex(index))
                {
                    PlayItem data = playlist[index];
                    if (data != null && data.path[0] != '#')  //Added song ("#~" is preset song)
                        data.clip = null;
                }
            }
        }

        //Check current status and settings and correct them
        //現在の状態と設定のチェックと自動修正
        private bool AutoSettingCheckAndExecute()
        {
            //When the preset song is not included and there are no additional songs etc.
            //プリセット曲を含まず、追加曲がないとき等
            if (TargetSongCount == 0)
            {
                string mes;
                if (autoSettingOnError)
                {
                    IsExceptPresets = false;   //Ignored when there is no change   //変化がないときには無視される
                    mes = localize.Text(localize.errorSongNotFound)
                        + "\n" + localize.Text(localize.optionalAutoExceptPresets);
                }
                else
                {
                    mes = localize.Text(localize.errorSongNotFound)
                        + "\n" + localize.Text(localize.confirmSetting);
                }
                ShowToast(mes);

                Play(IsPresetIndex(index) ? index : 0);

                return true;
            }
            return false;
        }

#endregion Music Player Section

#region Load AudioClip Section

        enum LoadedState
        {
            None,
            Success,        //Load success      //ロード成功
            NotExist,       //File not found    //ファイルが存在しない
            LoadFailure,    //Read failed       //読み込み失敗
        }

        LoadedState loadedState = LoadedState.None; //Loaded status     //読み込み状態ステータス
        AudioClip loadedClip;                       //Loaded AudioClip  //読み込まれた AudioClip

        //Load song from file into AudioClip
        //ファイルから楽曲を AudioClip にロードする
        IEnumerator LoadAudio(string path, int idx, Action<int, int> completeCallback, int dir = 0)
        {
            loadedClip = null;
            loadedState = LoadedState.None;

            if (!File.Exists(path)) {
                string mes = localize.Text(localize.errorNotExist)
                    + "\n" + path;
                if (autoPlaylistOnError)
                {
                    RemoveSongOnError(idx);
                    mes += "\n" + localize.Text(localize.optionalAutoPlaylist);
                }
                ShowToast(mes, true);

                loadedState = LoadedState.NotExist;
                completeCallback(idx, dir);
                yield break;
            }

            yield return StartCoroutine(LoadToAudioClip(path));

            if (loadedClip == null || loadedClip.loadState != AudioDataLoadState.Loaded)
            {
                string mes = localize.Text(localize.errorLoadFailure)
                    + "\n" + path;
                if (autoPlaylistOnError)
                {
                    RemoveSongOnError(idx);
                    mes += "\n" + localize.Text(localize.optionalAutoPlaylist);
                }
                ShowToast(mes, true);

                loadedState = LoadedState.LoadFailure;
                loadedClip = null;
                completeCallback(idx, dir);
                yield break;
            }

            loadedState = LoadedState.Success;
            completeCallback(idx, dir);
        }

        //Download the file as AudioClip
        //ファイルを AudioClip としてダウンロードする
        IEnumerator LoadToAudioClip(string path)
        {
            using(WWW www = new WWW("file://" + path))
            {
                while (!www.isDone)
                    yield return null;

                loadedClip = www.GetAudioClip(false, true);
            }
        }

        //Load processing complete callback handler
        //ロード処理完了コールバックハンドラ
        private void LoadAudioComplete(int idx, int dir = 1)
        {
            if (loadedState == LoadedState.Success)
            {
                if (IsValidIndex(idx))
                {
                    playlist[idx].clip = loadedClip;
                    PlayClip(idx);
                }
            }
            else
            {
                //NotExist, LoadFailure  //パス取得失敗, ロード失敗
                bool overLimit = autoPlaylistOnError ? (TargetSongCount == 0) : (++retry >= TargetSongCount);
                if (overLimit)
                {
                    //If the number of tracks to be played fails, forcibly set to preset or stop.
                    //再生対象曲数の回数失敗したら、強制的にプリセット曲または停止する
                    if (IsPresetIndex(index) || IsValidClip(0))   //preset song exists
                    {
                        if (autoSettingOnError) //Automatic correction of setting error     //設定エラーの自動修正
                        {
                            IsExceptPresets = false;   //Ignored when there is no change    //変化がないときには無視される
                        }

                        Play(IsPresetIndex(index) ? index : 0);
                    }
                    else
                    {
                        Stop();
                    }
                }
                else
                {
                    switch (mode)
                    {
                        case Mode.Loop:
                        case Mode.Next:
                            if (dir != 0)
                                Stop();     //Release current

                            //When 'autoPlaylistOnError = true', 'idx' is automatically deleted with an error.
                            //autoPlaylistOnError = true のとき、'idx'はエラーで自動削除されている。
                            if (autoPlaylistOnError)
                            {
                                if (dir > 0)
                                    Next(index);
                                else if (dir < 0)
                                    Prev(index);
                            }
                            else
                            {
                                if (dir > 0)
                                    Next(idx);
                                else if (dir < 0)
                                    Prev(idx);
                            }
                            break;

                        case Mode.Shuffle:
                            if (dir != 0)
                                Stop();     //Release current

                            //If deleted due to an error, a new shuffle table is generated.
                            //エラーで削除された場合、新たなシャッフルテーブルが生成されている。
                            if (dir > 0)
                                ShuffleNext();
                            else if (dir < 0)
                                ShufflePrev();
                            break;
                    }
                }
            }
        }

        //Delete songs from playlist on file error
        //ファイルエラー時にプレイリストから曲を削除
        private void RemoveSongOnError(int idx)
        {
            if (!IsValidIndex(idx))
                return;

            if (idx == index)   //Current song
                Stop();

            string key = IsValidIndex(index) ? playlist[index].path : "";   //same as path

            playlist.RemoveAt(idx);

            index = string.IsNullOrEmpty(key) ? -1 : IndexOfPlaylist(key);  //Not found = -1
            InitShuffleTable(IsValidIndex(index) ? index : int.MinValue);   //-1 -> Do not search the current index

            if (savePlaylist)
                SavePlaylist();
        }

#endregion Load AudioClip Section

#region Android Dialog Section

        const string KEY_SONG = "song";
        const string KEY_MODE = "mode";
        const string KEY_EXCEPT_PRESETS = "exceptPresets";
        const string KEY_AUTO_PLAYLIST = "autoPlaylist";
        const string KEY_AUTO_SETTING = "autoSetting";
        const string KEY_RESET_SETTING = "resetSetting";
        const string KEY_RESET_PLAYLIST = "resetPlaylist";

#pragma warning disable 0219    //'items' is never used. But it is used at runtime.

        //Open playlist and setting dialog
        public void OpenSongDialog()
        {
#if UNITY_ANDROID
            string[] songKeys; string[] songTitles;
            GetPlaylistKeyAndTitleArrays(out songKeys, out songTitles);
            ToggleItem toggleItem = new ToggleItem(
                    songTitles,
                    KEY_SONG,
                    songKeys,
                    index,
                    0,
                    "PreviewSong");

            DivisorItem divisorItem = new DivisorItem(1);

            ToggleItem toggleMode = new ToggleItem(
                localize.GetPlaybackCaptionArray(),
                KEY_MODE,
                Enum.GetNames(typeof(Mode)),
                mode.ToString(),
                0);


            CheckItem checkExceptPresets = new CheckItem(localize.Text(localize.exceptPresetsCaption), KEY_EXCEPT_PRESETS, exceptPresets, 0);
            TextItem textExceptPresets = new TextItem(localize.Text(localize.exceptPresetsExplanation), Color.red);

            CheckItem checkAutoPlaylist = new CheckItem(localize.Text(localize.autoPlaylistCaption), KEY_AUTO_PLAYLIST, autoPlaylistOnError, 0);
            CheckItem checkAutoSetting = new CheckItem(localize.Text(localize.autoSettingCaption), KEY_AUTO_SETTING, autoSettingOnError, 0);

            SwitchItem switchResetPlaylist = new SwitchItem(localize.Text(localize.resetPlaylistCaption), KEY_RESET_PLAYLIST, false, Color.blue);
            SwitchItem switchResetSetting = new SwitchItem(localize.Text(localize.resetSettingCaption), KEY_RESET_SETTING, false, Color.blue);

            TextItem textReset = new TextItem(localize.Text(localize.resetExplanation), Color.red);

            DialogItem[] items = new DialogItem[] {
                            toggleItem,
                            divisorItem,
                            toggleMode,
                            checkExceptPresets,
                            textExceptPresets,
                            divisorItem,
                            checkAutoPlaylist,
                            checkAutoSetting,
                            divisorItem,
                            switchResetPlaylist,
                            switchResetSetting,
                            textReset,
                        };
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidPlugin.ShowCustomDialog(
                localize.Text(localize.songDialogTitle), "",
                items, gameObject.name, "OnReceiveResult", false, 
                localize.Text(localize.songDialogOK), 
                localize.Text(localize.songDialogCancel));
#endif
        }

#pragma warning restore 0219    //'items' is never used. But it is used at runtime.

        //「決定」時のコールバックハンドラ（主に設定を反映する）
        private void OnReceiveResult(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Param param = Param.Parse(message);
                if (param != null)
                {
                    bool resetSetting = param.GetBool(KEY_RESET_SETTING, false);    //Reset settings    //設定のリセット
                    bool resetPlaylist = param.GetBool(KEY_RESET_PLAYLIST, false);  //Reset playlist    //プレイリストのリセット

                    if (resetSetting && resetPlaylist)
                    {
                        ResetPlaylistAndSetting();
                        ShowToast(localize.Text(localize.messageResetBoth));
                    }
                    else if (resetSetting)
                    {
                        ResetSetting();
                        ShowToast(localize.Text(localize.messageResetSetting));
                    }
                    else
                    {
                        if (resetPlaylist)
                        {
                            ResetPlaylist();
                            ShowToast(localize.Text(localize.messageResetPlaylist));
                        }

                        IsExceptPresets = param.GetBool(KEY_EXCEPT_PRESETS, exceptPresets);
                        autoPlaylistOnError = param.GetBool(KEY_AUTO_PLAYLIST, autoPlaylistOnError);
                        autoSettingOnError = param.GetBool(KEY_AUTO_SETTING, autoSettingOnError);
                        SetPlaybackMode(param.GetString(KEY_MODE, mode.ToString()));
                        
                        if (saveSetting)
                            SaveSetting();
                    }
                }
            }
        }

        //Song preview playback callback handler (Mainly apply songs to be played).
        //楽曲プレビュー再生コールバックハンドラ（主に再生する曲を反映する）
        private void PreviewSong(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Param param = Param.Parse(message);
                if (param != null)
                {
                    string path = param.GetString(KEY_SONG);    //"song=path"
                    if (!string.IsNullOrEmpty(path))
                    {
                        int idx = IndexOfPlaylist(path);
                        if (idx >= 0)
                        {
                            Play(path);

                            if (mode == Mode.Shuffle)
                                shufflePtr = shuffleTable.IndexOf(idx);
                        }
                        else
                            ShowToast(localize.Text(localize.errorNotExist));
                    }
                }
            }
        }


        //(*) Call the Storage Access Framework (API 19 [Android 4.4] or higher).
        //Open storage to add song
        public void OpenStorageToAdd()
        {
#if UNITY_EDITOR
            Debug.Log("OpenStorageToAdd called.");
#elif UNITY_ANDROID
            AndroidPlugin.OpenStorageAudio(MimeTypes, gameObject.name, "ReceiveAddResult", "ReceiveAddError");    //Always json
#endif
        }

        const string ErrorNotGetPathMessage = "Failed to get path.";

        //Callback handler when receive result
        private void ReceiveAddResult(string result)
        {
            if (result[0] == '{')   //Json
            {
                AudioInfo info = JsonUtility.FromJson<AudioInfo>(result);
                if (!string.IsNullOrEmpty(info.path))
                    AddSong(info);
                else
                    ReceiveAddError(ErrorNotGetPathMessage);
            }
            else
                ReceiveAddError(result);
        }

        //Callback handler when receive error
        private void ReceiveAddError(string message)
        {
            if (OnError != null)
                OnError.Invoke(message);
        }


        //Extension that can be played
        HashSet<string> availableExtType = new HashSet<string>()
        {
            ".mp3", ".ogg", ".wav"
        };

        //==========================================================
        //(*) Callback handler from 'StorageOpenAudioController.OnResultInfo'
        //==========================================================
        //Add a new song file
        //新しく楽曲ファイルを追加する
        public void AddSong(AudioInfo info)
        {
            //Check if the path is not empty
            //パスが空でないかをチェック
            if (string.IsNullOrEmpty(info.path))
            {
                ShowToast(localize.Text(localize.errorNotGetPath));
                return;
            }

            //Available extension check
            //利用できる拡張子チェック
            string ext = Path.GetExtension(info.path).ToLower();
            if (!availableExtType.Contains(ext))
            {
                ShowToast(localize.Text(localize.errorNotAvailableType));
                return;
            }

            //Registered file (path) check
            //登録済みファイル（パス）チェック
            int idx = IndexOfPlaylist(info.path);
            if (idx >= 0)
            {
                ShowToast(localize.Text(localize.errorDuplicateSong));
                return;
            }

            AddToPlaylist(info);    //Add at the end
            SavePlaylist();
            InitShuffleTable(playlist.Count - 1);

            StartCoroutine(LoadAudio(info.path, playlist.Count - 1, LoadAudioComplete));
        }

        //Add to playlist as data (Add at the end)
        //プレイリストにデータとして追加する（最後に追加）
        private void AddToPlaylist(AudioInfo info)
        {
            PlayItem data = new PlayItem(
                info.path, string.IsNullOrEmpty(info.title) ? info.name : info.title, info.artist);
            playlist.Add(data);
        }

        //Open a dialog for deletion
        //削除用のダイアログを開く
        public void OpenRemoveDialog()
        {
            string[] songKeys; string[] songTitles;
            GetPlaylistKeyAndTitleArrays(out songKeys, out songTitles, false);

            if (songKeys.Length > 0)
            {
#if UNITY_EDITOR
                Debug.Log("OpenRemoveDialog called.");
#elif UNITY_ANDROID
                AndroidPlugin.ShowMultiChoiceDialog(
                    localize.Text(localize.removeDialogTitle),
                    songTitles, null,
                    gameObject.name, "ReceiveRemoveResult", songKeys,
                    localize.Text(localize.removeDialogOK),
                    localize.Text(localize.removeDialogCancel));
#endif
            }
            else
            {
                ShowToast(localize.Text(localize.errorRemoveNotFound));
            }
        }

        //Callback handler for deletion dialog
        //削除用のダイアログのコールバックハンドラ
        private void ReceiveRemoveResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            string[] paths = result.Split('\n');
            if (paths.Length > 0)
                RemoveSong(paths);
        }

        //Remove songs from playlist (* Preset songs are ignored)
        //プレイリストから複数曲を削除（※プリセット曲は無視される）
        public void RemoveSong(string[] paths)
        {
            string key = IsValidIndex(index) ? playlist[index].path : "";
            int lastIdx = -1;
            int moveIdx = index;    //Destination index when the number smaller than the original number is deleted
            int deleted = 0;        //Deleted count

            for (int i = paths.Length - 1; i >= 0; i--)
            {
                string path = paths[i];
                if (!string.IsNullOrEmpty(path) && path[0] != '#')
                {
                    int idx = LastIndexOfRangePlaylist(path, lastIdx, presets.Count);   //Range of added songs
                    if (idx >= 0)
                    {
                        if (idx == index)       //Current song
                            Stop();
                        else if (idx < index)   //'presets.Count <= idx' is known.
                            moveIdx--;

                        playlist.RemoveAt(idx);
                        lastIdx = idx - 1;
                        deleted++;
                    }
                }
            }

            int toIndex = string.IsNullOrEmpty(key) ? -1 : IndexOfPlaylist(key);    //not found = -1 (Normally not)
            InitShuffleTable(IsValidIndex(toIndex) ? toIndex : int.MinValue);       //Do not search the current index

            if (savePlaylist)
                SavePlaylist();

            if (deleted > 0)
                ShowToast(string.Format(localize.Text(localize.messageRemoved), deleted));

            switch (mode)
            {
                case Mode.Loop:
                case Mode.Next:
                    if (IsValidIndex(toIndex))
                        Play(toIndex);  //for update index and SaveSetting
                    else
                        Play(Mathf.Clamp(moveIdx, TargetSongCount > 0 ? TargetSongFirstIndex : 0, playlist.Count - 1));
                    break;

                case Mode.Shuffle:
                    if (IsValidIndex(toIndex))
                        Play(toIndex);  //for update index and SaveSetting
                    else
                        ShuffleNext();
                    break;
            }
        }

#endregion Android Dialog Section

#region UI etc. Section

        //Display message on Toast
        private void ShowToast(string message, bool longDuration = false)
        {
            if (string.IsNullOrEmpty(message))
                return;
#if UNITY_EDITOR
            Debug.Log("ShowToast called : " + message);
#elif UNITY_ANDROID
            AndroidPlugin.ShowToast(message, longDuration);
#endif
        }

        //Display song title etc.
        private void DisplayInfo()
        {
            if (IsValidIndex(index) && playlist[index] != null)
            {
                if (displayTitle != null)
                {
                    string title = playlist[index].title;
                    if (string.IsNullOrEmpty(title) && playlist[index].clip != null)
                        title = playlist[index].clip.name;
                    displayTitle.text = titlePrefix + title;
                }

                if (displayArtist != null)
                {
                    string artist = string.IsNullOrEmpty(playlist[index].artist) ? artistUnknown : playlist[index].artist;
                    displayArtist.text = artist;
                }
            }
        }

#endregion UI etc. Section

#region Other static method etc. Section

        //Shuffle list elements (Fisher-Yates shuffle)
        //リスト要素のシャッフル
        static void Shuffle<T>(List<T> list)
        {
		    for (int i = list.Count - 1; i > 0; i--) {
                int j = Random.Range(0, i + 1); //[0]～[i]
                Swap(list, i, j);
		    }
        }

        //Element swap
        //要素のスワップ
        static void Swap<T>(List<T> list, int i, int j)
        {
            if (i != j)
            {
		        T tmp = list[i];
		        list[i] = list[j];
		        list[j] = tmp;
            }
        }

        //Apply lower limit (inclusive) to upper limit (exclusive) to Mathf.Repeat().
        //Mathf.Repeat() に下限（含む）～上限（除く）を付ける
        static int Repeat(int value, int min, int max)   //min: inclusive, max: exclusive
        {
            return (int)Mathf.Repeat(value - min, max - min) + min;
        }

#endregion Other static method etc. Section

#region Localize Section

        [Serializable]
        public class LocalizeData
        {
            public SystemLanguage language = SystemLanguage.Unknown;   //Current localize language

            //==========================================================
            //Dialog captions

            public LocalizeString songDialogTitle = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Song selection and setting options"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "曲の選択と設定オプション"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "乐曲选择和设置选项"),
                new LocalizeString.Data(SystemLanguage.Korean, "곡 선택 및 설정 옵션"),
            });

            public LocalizeString songDialogOK = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Apply"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "決定"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "应用"),
                new LocalizeString.Data(SystemLanguage.Korean, "대다"),
            });

            public LocalizeString songDialogCancel = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Cancel"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "キャンセル"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "取消"),
                new LocalizeString.Data(SystemLanguage.Korean, "취소"),
            });

            public LocalizeString exceptPresetsCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "All song play / shuffle, except for preset songs"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "連続再生/シャッフルのとき、プリセット曲を除く"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "所有歌曲播放/随机播放，除了预设歌曲"),
                new LocalizeString.Data(SystemLanguage.Korean, "전곡 재생 / 셔플 때 프리셋 곡을 제외"),
            });

            public LocalizeString exceptPresetsExplanation = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "* If there are no additional songs, it will be invalid."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "※追加曲がない場合は無効になります。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "*如果没有其他歌曲，它将无效。"),
                new LocalizeString.Data(SystemLanguage.Korean, "※ 추가 곡이없는 경우는 무효가됩니다."),
            });

            public LocalizeString autoPlaylistCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "If not find a song, delete it from the playlist automatically"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "曲が見つからない場合、自動でプレイリストから削除する"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "如果您找不到歌曲，请自动将其从播放列表中删除"),
                new LocalizeString.Data(SystemLanguage.Korean, "곡이 없으면 자동으로 재생 목록에서 삭제"),
            });

            public LocalizeString autoSettingCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Automatically modify options on error"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "エラー時にオプションを自動で修正する"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "自动修改错误选项"),
                new LocalizeString.Data(SystemLanguage.Korean, "오류시 옵션을 자동으로 수정"),
            });

            public LocalizeString resetPlaylistCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Reset playlist"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "プレイリストのリセット"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "重置播放列表"),
                new LocalizeString.Data(SystemLanguage.Korean, "재생 목록 재설정"),
            });

            public LocalizeString resetSettingCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Reset settings"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "設定のリセット"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "重置设置"),
                new LocalizeString.Data(SystemLanguage.Korean, "설정 재설정"),
            });

            public LocalizeString resetExplanation = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "* All saved settings will be erased when reset."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "※リセットすると保存された設定が全て消去されます。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "*所有保存的设置将在复位时被删除。"),
                new LocalizeString.Data(SystemLanguage.Korean, "※ 재설정하면 저장된 설정이 모두 삭제됩니다."),
            });


            public LocalizeString oneSongCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "1 song"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "１曲再生"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "1首歌曲播放"),
                new LocalizeString.Data(SystemLanguage.Korean, "1 곡 재생"),
            });

            public LocalizeString allSongCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "All songs"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "連続再生"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "所有歌曲播放"),
                new LocalizeString.Data(SystemLanguage.Korean, "전곡 재생"),
            });

            public LocalizeString shuffleCaption = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Shuffle"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "シャッフル"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "拖曳"),
                new LocalizeString.Data(SystemLanguage.Korean, "셔플"),
            });

            public string[] GetPlaybackCaptionArray()
            {
                return new string[] {
                    oneSongCaption.TextByLanguage(language),
                    allSongCaption.TextByLanguage(language),
                    shuffleCaption.TextByLanguage(language),
                };
            }


            public LocalizeString removeDialogTitle = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Select songs to delete from the playlist"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "プレイリストから削除する曲を選択"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "选择要从播放列表中删除的歌曲"),
                new LocalizeString.Data(SystemLanguage.Korean, "재생 목록에서 삭제하는 곡을 선택"),
            });

            public LocalizeString removeDialogOK = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Delete"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "削除"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "删除"),
                new LocalizeString.Data(SystemLanguage.Korean, "삭제"),
            });

            public LocalizeString removeDialogCancel = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Cancel"),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "キャンセル"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "取消"),
                new LocalizeString.Data(SystemLanguage.Korean, "취소"),
            });


            //==========================================================
            //Toast messages

            public LocalizeString messageResetSetting = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "The setting was reset."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "設定がリセットされました。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "该设置被重置。"),
                new LocalizeString.Data(SystemLanguage.Korean, "설정이 초기화되었습니다."),
            });

            public LocalizeString messageResetPlaylist = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "The playlist has been reset."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "プレイリストがリセットされました。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "该列表已被重置。"),
                new LocalizeString.Data(SystemLanguage.Korean, "목록이 재설정되었습니다."),
            });

            public LocalizeString messageResetBoth = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Playlist and settings reset."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "プレイリストと設定がリセットされました。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "播放列表和设置重置。"),
                new LocalizeString.Data(SystemLanguage.Korean, "재생 목록 설정이 재설정되었습니다."),
            });

            public LocalizeString messageRemoved = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "The file of {0} song(s) has been deleted from the playlist."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "{0}曲のファイルがプレイリストから削除されました。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "已从播放列表中删除{0}首歌曲的文件。"),
                new LocalizeString.Data(SystemLanguage.Korean, "{0} 음악 파일이 재생 목록에서 삭제되었습니다."),
            });

            public LocalizeString errorNotGetPath = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Path acquisition failed.\nFor cloud storage, please download it."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "パスの取得に失敗しました。\nクラウドストレージの場合、ダウンロードして下さい。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "路径获取失败。\n对于云存储，请下载。"),
                new LocalizeString.Data(SystemLanguage.Korean, "경로의 취득에 실패했습니다.\n클라우드 스토리지의 경우 다운로드하십시오."),
            });

            public LocalizeString errorDuplicateSong = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "It is already registered in the playlist."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "既にプレイリストに登録されています。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "它已经在播放列表中注册。"),
                new LocalizeString.Data(SystemLanguage.Korean, "이미 재생 목록에 등록되어 있습니다."),
            });

            public LocalizeString errorNotAvailableType = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Audio format is not available."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "利用できないオーディオ形式です。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "音频格式不可用。"),
                new LocalizeString.Data(SystemLanguage.Korean, "사용할 수없는 오디오 형식입니다."),
            });

            public LocalizeString errorNotExist = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "File not found.\nIt may have been moved or deleted."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "ファイルが見つかりません。\n移動または削除された可能性があります。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "未找到文件。\n它可能已被移动或删除。"),
                new LocalizeString.Data(SystemLanguage.Korean, "파일을 찾을 수 없습니다.\n이동 또는 삭제 된 수 있습니다."),
            });

            public LocalizeString errorLoadFailure = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Failed to read the file.\nAudio data is invalid."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "ファイルの読み込みに失敗しました。\nオーディオデータが不正です。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "无法读取文件。\n音频数据无效。"),
                new LocalizeString.Data(SystemLanguage.Korean, "파일의 읽기에 실패했습니다.\n오디오 데이터가 잘못되었습니다."),
            });

            public LocalizeString errorRemoveNotFound = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "There are no songs that can be deleted."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "削除可能な楽曲がありません。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "没有可以删除的歌曲。"),
                new LocalizeString.Data(SystemLanguage.Korean, "제거 가능한 악곡이 없습니다."),
            });

            public LocalizeString errorSongNotFound = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "There is no song that can be played."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "再生可能な楽曲がありません。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "没有音乐可以播放。"),
                new LocalizeString.Data(SystemLanguage.Korean, "재생 가능한 악곡이 없습니다."),
            });

            public LocalizeString confirmSetting = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "Please check the current setting."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "現在の設定を確認して下さい。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "请检查当前设置。"),
                new LocalizeString.Data(SystemLanguage.Korean, "현재 설정을 확인하십시오."),
            });

            public LocalizeString optionalAutoPlaylist = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "It was deleted automatically from the playlist."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "自動でプレイリストから削除されました。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "它已从播放列表中自动删除。"),
                new LocalizeString.Data(SystemLanguage.Korean, "자동으로 재생 목록에서 삭제되었습니다."),
            });

            public LocalizeString optionalAutoExceptPresets = new LocalizeString(SystemLanguage.English,
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.English, "The exclusion setting of the preset songs was turned off."),    //default language
                new LocalizeString.Data(SystemLanguage.Japanese, "プリセット曲の除外設定をオフに修正しました。"),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "预设歌曲的排除设置已关闭。"),
                new LocalizeString.Data(SystemLanguage.Korean, "프리셋 곡의 제외 설정을 해제 수정했습니다."),
            });

            //Text according to 'language' field
            public string Text(LocalizeString localizeString)
            {
                return localizeString.TextByLanguage(language);
            }
        }
        public LocalizeData localize;

        //ILocalizable implementation
        public void ApplyLocalize(SystemLanguage language)
        {
            if (localize == null)
                localize = new LocalizeData();

            localize.language = language;
        }

#endregion Localize Section
    }
}
