using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Launch QR Code (Bar Code) Scanner to acquire text.
    /// Using ZXing ("zebra crossing") open source project (google). [ver.3.3.2]
    /// https://github.com/zxing/zxing
    /// (Apache License Version 2.0)
    /// http://www.apache.org/licenses/LICENSE-2.0
    ///·Launch the QR Code Scanner application of ZXing and obtain the result text.
    ///·When cancellation or acquisition fails, it returns a empty character ("").
    ///·If ZXing's QR Code Scanner application is not in the device, a dialog prompting installation will be displayed.
    /// https://play.google.com/store/apps/details?id=com.google.zxing.client.android
    /// 
    /// 
    /// QRコード(バーコード)スキャナを起動してテキストを取得する。
    /// ZXing オープンソースプロジェクト（google）を利用。[ver.3.3.2]
    /// https://github.com/zxing/zxing
    /// (Apache License Version 2.0)
    /// http://www.apache.org/licenses/LICENSE-2.0
    ///・ZXing の QRコードスキャナアプリを起動し、結果のテキストを取得する。
    ///・キャンセルまたは取得失敗したときなどは、空文字（""）を返す。
    ///・端末に ZXing の QRコードスキャナアプリが入ってない場合は、インストールを促すダイアログが表示される。
    /// https://play.google.com/store/apps/details?id=com.google.zxing.client.android
    /// </summary>
    public class QRCodeScannerController : MonoBehaviour
    {
        //Callbacks
        [Serializable] public class ResultHandler : UnityEvent<string> { }  //text string
        public ResultHandler OnResult;


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Launch QR Code Scanner
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("QRCodeScannerController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowQRCodeScanner(gameObject.name, "ReceiveResult");
#endif
        }

        //Returns value when Barcode scan succeed
        private void ReceiveResult(string text)
        {
            if (OnResult != null)
                OnResult.Invoke(text);
        }
    }
}
