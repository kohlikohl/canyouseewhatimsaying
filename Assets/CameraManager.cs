
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.WSA.WebCam;

public class CameraManager : MonoBehaviour
{
    public const int FetchEmotionsDataIntervalMs = 3000;
    public const string EmotionsApiKey = "ab06c57a4a504abeb597cdc96ad67e38";
    public const string EmotionsApiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

    //public Text dictationResult;

    public static List<Face> CurrentFrameFaces;

    PhotoCapture photoCaptureObject = null;
    bool photoModeStarted = false;
    DateTime lastEmotionsDataFetch;
    
    void Start()
    {
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }
    
    void Update()
    {
        // No timers - images from the camera can be fetched only from the main thread.
        if (photoModeStarted && (lastEmotionsDataFetch == null ||
            (DateTime.Now - lastEmotionsDataFetch > TimeSpan.FromMilliseconds(FetchEmotionsDataIntervalMs))))
        {
            lastEmotionsDataFetch = DateTime.Now;

            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
    }

    void Awake()
    {
    }

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoModeStarted = true;
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);

            byte[] bytes = targetTexture.EncodeToJPG();

            var headers = new Dictionary<string, string>() {
                { "Ocp-Apim-Subscription-Key", EmotionsApiKey },
                { "Content-Type", "application/octet-stream" }
            };
            WWW www = new WWW(EmotionsApiUrl, bytes, headers);
            StartCoroutine(ParseResponse(www));
        }

        //photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    private IEnumerator ParseResponse(WWW www)
    {
        yield return www;

        if (www.text != "[]")
        {
            //dictationResult.text = www.text;

            try
            {
                CameraManager.CurrentFrameFaces =
                    JsonConvert.DeserializeObject<List<Face>>(www.text);
            }
            catch (Exception) {}
        }
    }
}

[DataContract]
public class Face
{
    [DataMember(Name = "faceRectangle")]
    public Rectangle Rectangle { get; set; }

    [DataMember(Name = "scores")]
    public EmotionsScores Scores { get; set; }
}

[DataContract]
public class Rectangle
{
    [DataMember(Name = "left")]
    public int Left { get; set; }

    [DataMember(Name = "top")]
    public int Top { get; set; }

    [DataMember(Name = "width")]
    public int Width { get; set; }

    [DataMember(Name = "height")]
    public int Height { get; set; }
}

[DataContract]
public class EmotionsScores
{
    [DataMember(Name = "anger")]
    public float Anger { get; set; }

    [DataMember(Name = "contempt")]
    public float Contempt { get; set; }

    [DataMember(Name = "disgust")]
    public float Disgust { get; set; }

    [DataMember(Name = "fear")]
    public float Fear { get; set; }

    [DataMember(Name = "happiness")]
    public float Happiness { get; set; }

    [DataMember(Name = "neutral")]
    public float Neutral { get; set; }

    [DataMember(Name = "sadness")]
    public float Sadness { get; set; }

    [DataMember(Name = "surprise")]
    public float Surprise { get; set; }
}