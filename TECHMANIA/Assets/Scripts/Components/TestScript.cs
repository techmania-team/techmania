using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    private bool done;

    // Start is called before the first frame update
    void Start()
    {
        string workingFolder = Paths.GetTrackRootFolder();
        string zipFilename = Path.Combine(workingFolder, "test.zip");

        FileStream fileStream = File.OpenRead(zipFilename);
        ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(fileStream);

        byte[] buffer = new byte[4096];  // Recommended length

        foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry entry in
            zipFile)
        {
            if (string.IsNullOrEmpty(Path.GetDirectoryName(entry.Name)))
            {
                Debug.Log($"Ignoring due to not being in a folder: {entry.Name} in {zipFilename}");
                continue;
            }

            if (entry.IsDirectory)
            {
                Debug.Log($"Ignoring empty folder: {entry.Name} in {zipFilename}");
                continue;
            }

            string unpackedFilename = Path.Combine(
                workingFolder, entry.Name);
            Debug.Log($"Unpacking {entry.Name} in {zipFilename} to: {unpackedFilename}");

            var inputStream = zipFile.GetInputStream(entry);
            Directory.CreateDirectory(Path.GetDirectoryName(
                unpackedFilename));
            FileStream outputStream = File.Create(unpackedFilename);
            ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(
                inputStream, outputStream, buffer);
        }

        Debug.Log($"Unpack successful. Deleting: {zipFilename}");
        fileStream.Dispose();
        File.Delete(zipFilename);
    }
}
