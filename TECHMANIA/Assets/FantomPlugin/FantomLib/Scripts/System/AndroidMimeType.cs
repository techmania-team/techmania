using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FantomLib
{
    /// <summary>
    /// MIME Type for Android
    /// 
    ///･It is mainly used for specifying mimeType when SAF (Storage Access Framework) is used.
    ///･Android files are managed in a database and are categorized by Files (other than media file), Images (image), Audio (audio), Video (movie).
    ///･Mutiple extensions may be associated with MIME type, and there may be multiple MIME types (including different categories) with one extension.
    ///･Note that MIME type varies depending on provider such as storage and cloud (e.g. MIME type with the extension "csv" is "text/comma-separated-values" for local storage and "text/csv" for Google Drive).
    ///(*) Note that the MIME type does not necessarily work.
    ///(*) When adding, MIME type must not be duplicated within the same category.
    /// 
    /// 
    ///・主に SAF（ストレージアクセス機能）利用時の mimeType 指定に使う。
    ///・Android のファイルはデータベースで管理されており、File（メディアファイル以外）, Image（画像）, Audio（音声）, Video（動画）でカテゴリ分けされている。
    ///・MIME type には複数の拡張子が対応する場合があり、また１つの拡張子で複数の MIME type（別のカテゴリを含む）ことがある。
    ///・MIME type はストレージやクラウドなどプロバイダによって異なるので注意（例：拡張子 "csv" の MIME type はローカルストレージでは "text/comma-separated-values" であり、Google Drive では "text/csv" で認識する）。
    ///※MIME type は必ずしも効くわけではないので注意して下さい。
    ///※追加する場合は、同カテゴリ内で MIME type が重複してはいけません。
    /// </summary>
    public static class AndroidMimeType
    {
        public enum MediaType
        {
            File, Image, Audio, Video
        }

        public static class File
        {
            public const string All = "*/*";            //All file type. (*) Please do not change as much as possible.
            public const string TextAll = "text/*";     //All text type. (*) Please do not change as much as possible.
            public const string txt = "text/plain";
            public const string ApplicationAll = "application/*";   //All application type. (*) Please do not change as much as possible.
            public const string pdf = "application/pdf";

            public static readonly string[][] ConstantValues =
            {
                              //MIME type, ext1, ext2, ... (low index priority)
                new string[] { txt, "txt" },
                new string[] { "text/html", "html", "htm" },
                new string[] { "text/comma-separated-values", "csv" },  //for local storage
                new string[] { "text/csv", "csv" },                     //for etc.
                new string[] { "text/xml", "xml" },                     //for local storage
                new string[] { "application/xml", "xml" },              //for etc.
                new string[] { "application/octet-stream", "json" },    //for local storage. (*) Originally a binary MIME type. Note the duplicate definitions.
                new string[] { "application/json", "json" },            //for etc.

                new string[] { pdf, "pdf" },
                new string[] { "application/vnd.ms-excel", "xls" },
                new string[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx" },
                new string[] { "application/msword", "doc" },
                new string[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx" },
                //new string[] { "application/mspowerpoint", "ppt" },     //not available?
                new string[] { "application/vnd.openxmlformats-officedocument.presentationml.presentation", "pptx" },
            };

            public static void GetMimeToExtList(out List<string> mime, out List<string[]> ext)
            {
                AndroidMimeType.GetMimeToExtList(ConstantValues, out mime, out ext);
            }

            public static void GetExtToMimeList(out List<string> ext, out List<string[]> mime)
            {
                AndroidMimeType.GetExtToMimeList(ConstantValues, out ext, out mime);
            }
        }

        public static class Image
        {
            public const string All = "image/*";        //All image type. (*) Please do not change as much as possible.
            public const string jpg = "image/jpeg";
            public const string png = "image/png";

            public static readonly string[][] ConstantValues =
            {
                              //MIME type, ext1, ext2, ... (low index priority)
                new string[] { jpg, "jpg", "jpeg" },
                new string[] { png, "png" },
                //new string[] { "image/gif", "gif" },  //not available on Unity
            };

            public static void GetMimeToExtList(out List<string> mime, out List<string[]> ext)
            {
                AndroidMimeType.GetMimeToExtList(ConstantValues, out mime, out ext);
            }

            public static void GetExtToMimeList(out List<string> ext, out List<string[]> mime)
            {
                AndroidMimeType.GetExtToMimeList(ConstantValues, out ext, out mime);
            }
        }

        public static class Audio
        {
            public const string All = "audio/*";        //All audio type. (*) Please do not change as much as possible.
            public const string mp3 = "audio/mpeg";
            public const string wav = "audio/x-wav";

            public static readonly string[][] ConstantValues =
            {
                              //MIME type, ext1, ext2, ... (low index priority)
                new string[] { "audio/mpeg", "mp3",/*"m4a"*/ },     //"m4a" is not available on Unity
                new string[] { wav, "wav" },
                new string[] { "application/ogg", "ogg" },          //for local storage
                new string[] { "audio/ogg", "ogg" },                //for etc.
            };

            public static void GetMimeToExtList(out List<string> mime, out List<string[]> ext)
            {
                AndroidMimeType.GetMimeToExtList(ConstantValues, out mime, out ext);
            }

            public static void GetExtToMimeList(out List<string> ext, out List<string[]> mime)
            {
                AndroidMimeType.GetExtToMimeList(ConstantValues, out ext, out mime);
            }
        }

        public static class Video
        {
            public const string All = "video/*";        //All video type. (*) Please do not change as much as possible.
            public const string mp4 = "video/mp4";

            public static readonly string[][] ConstantValues =
            {
                              //MIME type, ext1, ext2, ... (low index priority)
                new string[] { mp4, "mp4" },
                new string[] { "video/3gpp", "3gp" },
                new string[] { "video/3gpp2", "3g2" },
            };

            public static void GetMimeToExtList(out List<string> mime, out List<string[]> ext)
            {
                AndroidMimeType.GetMimeToExtList(ConstantValues, out mime, out ext);
            }

            public static void GetExtToMimeList(out List<string> ext, out List<string[]> mime)
            {
                AndroidMimeType.GetExtToMimeList(ConstantValues, out ext, out mime);
            }
        }


        //==========================================================
        // Extension and MIME Type (list values)

        //Generate a correspondence list of MIME type -> {ext1, ext2, ...} within the same category
        //同カテゴリ内で MIME type -> {ext1, ext2, ...} の対応リストを生成する
        static void GetMimeToExtList(string[][] constantValues, out List<string> mime, out List<string[]> ext)
        {
            mime = new List<string>();
            ext = new List<string[]>();

            foreach (var item in constantValues)
            {
                string[] arr = new string[item.Length - 1];     //extension: always 1 or more
                Array.Copy(item, 1, arr, 0, item.Length - 1);   //item[0]: MIME type, [1][2]...: ext -> arr: extract ext only
                mime.Add(item[0]);                              //MIME type. Always [0] exists.
                ext.Add(arr);                                   //{ext1, ext2, ...}
            }
        }

        //(*) For public acquisition
        //Generate a correspondence list of MIME type -> {ext1, ext2, ...} within the same category
        //同カテゴリ内で MIME type -> {ext1, ext2, ...} の対応リストを生成する
        public static void GetMimeToExtList(MediaType mediaType, out List<string> mime, out List<string[]> ext)
        {
            switch (mediaType)
            {
                default:
                case MediaType.File:
                    GetMimeToExtList(File.ConstantValues, out mime, out ext);
                    break;
                case MediaType.Image:
                    GetMimeToExtList(Image.ConstantValues, out mime, out ext);
                    break;
                case MediaType.Audio:
                    GetMimeToExtList(Audio.ConstantValues, out mime, out ext);
                    break;
                case MediaType.Video:
                    GetMimeToExtList(Video.ConstantValues, out mime, out ext);
                    break;
            }
        }

        //Generate a correspondence list of ext -> {MIME type1, MIME type2, ...} within the same category
        //同カテゴリ内で ext -> {MIME type1, MIME type2, ...} の対応リストを生成する
        static void GetExtToMimeList(string[][] constantValues, out List<string> ext, out List<string[]> mime)
        {
            ext = new List<string>();
            mime = new List<string[]>();

            foreach (var row in constantValues)
            {
                for (int i = 1; i < row.Length; i++)   //extension: always 1 or more
                {
                    int idx = ext.IndexOf(row[i]);     //(*) Inefficient processing    //※非効率な処理
                    if (idx >= 0)
                    {
                        mime[idx] = AddToArray(mime[idx], row[0]);
                    }
                    else
                    {
                        ext.Add(row[i]);
                        mime.Add(new string[] { row[0] });
                    }
                }
            }
        }

        //(*) For public acquisition
        //Generate a correspondence list of ext -> {MIME type1, MIME type2, ...} within the same category
        //同カテゴリ内で ext -> {MIME type1, MIME type2, ...} の対応リストを生成する
        public static void GetExtToMimeList(MediaType mediaType, out List<string> ext, out List<string[]> mime)
        {
            switch (mediaType)
            {
                default:
                case MediaType.File:
                    GetExtToMimeList(File.ConstantValues, out ext, out mime);
                    break;
                case MediaType.Image:
                    GetExtToMimeList(Image.ConstantValues, out ext, out mime);
                    break;
                case MediaType.Audio:
                    GetExtToMimeList(Audio.ConstantValues, out ext, out mime);
                    break;
                case MediaType.Video:
                    GetExtToMimeList(Video.ConstantValues, out ext, out mime);
                    break;
            }
        }


        //==========================================================
        // Extension and MIME Type (each values)

        //MIME Type <-> Extension convert table
        static readonly Dictionary<string, string[]> MimeToExt = new Dictionary<string, string[]>();
        static readonly Dictionary<string, string[]> ExtToMime = new Dictionary<string, string[]>();

        //Generate MIME Type <-> Extension convert table
        static void MakeTable()
        {
            MimeToExt.Clear();
            ExtToMime.Clear();

            List<string> mime; List<string[]> ext;
            File.GetMimeToExtList(out mime, out ext);
            for (int i = 0; i < mime.Count; i++)
            {
                foreach (var item in ext[i])
                {
                    AddUniqValueAsArray(MimeToExt, mime[i], item);
                    AddUniqValueAsArray(ExtToMime, item, mime[i]);
                }
            }

            Image.GetMimeToExtList(out mime, out ext);
            for (int i = 0; i < mime.Count; i++)
            {
                foreach (var item in ext[i])
                {
                    AddUniqValueAsArray(MimeToExt, mime[i], item);
                    AddUniqValueAsArray(ExtToMime, item, mime[i]);
                }
            }

            Audio.GetMimeToExtList(out mime, out ext);
            for (int i = 0; i < mime.Count; i++)
            {
                foreach (var item in ext[i])
                {
                    AddUniqValueAsArray(MimeToExt, mime[i], item);
                    AddUniqValueAsArray(ExtToMime, item, mime[i]);
                }
            }

            Video.GetMimeToExtList(out mime, out ext);
            for (int i = 0; i < mime.Count; i++)
            {
                foreach (var item in ext[i])
                {
                    AddUniqValueAsArray(MimeToExt, mime[i], item);
                    AddUniqValueAsArray(ExtToMime, item, mime[i]);
                }
            }
        }

        //(*) For public acquisition
        //MIME type -> Extension (array)
        public static string[] GetExtension(string mimeType)
        {
            if (MimeToExt.Count == 0)
                MakeTable();

            if (MimeToExt.ContainsKey(mimeType))
                return MimeToExt[mimeType];

            return null;  //Not found
        }

        //(*) For public acquisition
        //Extension -> MIME type (array)
        public static string[] GetMimeType(string ext)
        {
            if (ExtToMime.Count == 0)
                MakeTable();

            if (ExtToMime.ContainsKey(ext))
                return ExtToMime[ext];

            return null;  //Not found
        }


        //==========================================================
        // other method

        //Add a value (character string) uniquely as an array.
        //(*) This additional processing is high in load, but there is not much problem because the target data (duplicated value) is small.
        //
        //値（文字列）を配列としてユニーク追加する。
        //※この追加の処理は負荷が高いが、対象となるデータ（重複してるもの）は少量であるため、あまり問題ないとしている。
        //※Dictionary<string, List<string>> にした方が追加処理は軽いが、表記が煩雑になる（また読み取りは List<string> より string[] の方が速い）。
        static void AddUniqValueAsArray(Dictionary<string, string[]> dic, string key, string val)
        {
            if (dic.ContainsKey(key))
            {
                string[] src = dic[key];

                //(*) The following processing is noticeable when the data is large, because the load is high.
                //※以下の処理はデータが大量にあるとき、負荷が高いので注意
                if (!src.Contains(val))
                    dic[key] = AddToArray(src, val);
            }
            else
            {
                dic[key] = new string[] { val };
            }
        }
        
        //Add an element to the end of the array (Always new array is generated)
        //配列の最後に要素を追加する（常に新しい配列が生成される）
        static string[] AddToArray(string[] src, string val)
        {
            if (src != null)
            {
                string[] dst = new string[src.Length + 1];
                Array.Copy(src, 0, dst, 0, src.Length);
                dst[src.Length] = val;
                return dst;
            }
            else
            {
                return new string[] { val };
            }
        }
    }
}