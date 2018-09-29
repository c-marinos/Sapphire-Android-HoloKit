using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using ZXing;
using ZXing.QrCode;
using System;

public class OpenFileButton : MonoBehaviour
{
#pragma warning disable 0219
#pragma warning disable 0414
#pragma warning disable 0618

    public string filestring, topfilestring, xfilestring;
    public GameObject LoadButton, HologramCollection;
    public Text ConsoleTex;
    UnityWebRequest www;

    private WebCamTexture camTexture;
    private Rect screenRect;

    void Start()
    {

    }

    private IEnumerable ScanQR()
    {
        // drawing the camera on screen
        GUI.DrawTexture(screenRect, camTexture, ScaleMode.ScaleToFit);
        // do the reading — you might want to attempt to read less often than you draw on the screen for performance sake

        IBarcodeReader barcodeReader = new BarcodeReader();
        // decode the current frame
        var result = barcodeReader.Decode(camTexture.GetPixels32(),
          camTexture.width, camTexture.height);
        if (result != null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            yield return new WaitForSeconds(3);
        }

        yield return new WaitForSeconds(1);

    }

    private IEnumerator WaitAndPrint(float waitTime, UnityWebRequest www)
    {
        yield return www.SendWebRequest();
        while (!www.isDone)
        {
            yield return new WaitForSeconds(waitTime);
        }
        if (www.isNetworkError || www.isHttpError)
        {
            //
        }
        if (www.isDone)
        {
            filestring = www.downloadHandler.text;
            GetComponent<Load>().LoadMoleculeAsync();

            www = null;
            yield break;
        }
    }

}
