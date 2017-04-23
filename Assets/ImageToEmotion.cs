using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;

public class ImageToEmotionAPI
{

    string EMOTIONKEY = "ab06c57a4a504abeb597cdc96ad67e38"; // replace with your Emotion API Key

    string emotionURL = "https://api.projectoxford.ai/emotion/v1.0/recognize";

    public string fileName { get; private set; }
    string responseData;

    public void getEmotion()
    {
        fileName = Path.Combine(Application.streamingAssetsPath, "myphoto.jpeg"); // Replace with your file
        MakeRequest(fileName);
    }

    byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        return binaryReader.ReadBytes((int)fileStream.Length);
    }

    void MakeRequest(string imageFilePath)
    {
        var client = new HttpClient();

        // Request headers
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", EMOTIONKEY);

        string uri = "https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize?";
        HttpResponseMessage response;
        string responseContent;

        // Request body. Try this sample with a locally stored JPEG image.
        byte[] byteData = GetImageAsByteArray(imageFilePath);

        using (var content = new ByteArrayContent(byteData))
        {
            // This example uses content type "application/octet-stream".
            // The other content types you can use are "application/json" and "multipart/form-data".
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response = client.PostAsync(uri, content).getResult();
            responseContent = response.Content.ReadAsStringAsync().Result;
        }

        //A peak at the JSON response.
        Debug.Log(responseContent);
    }
}