using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;

public class DictationManager : MonoBehaviour,IInputClickHandler
{

    public Text dictationResult;


    private DictationRecognizer dictationRecognizer;
    private StringBuilder textSoFar;

    // Use this to reset the UI once the Microphone is done recording after it was started.
    private bool hasRecordingStarted;

    // Using an empty string specifies the default microphone. 
    private static string deviceName = string.Empty;
    private int samplingRate;
    private const int messageLength = 20;

    private bool isRecording;

    private void Awake()
    {
        InputManager.Instance.AddGlobalListener(this.gameObject);

        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;

        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;

        dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;

        dictationRecognizer.DictationError += DictationRecognizer_DictationError;

        // Query the maximum frequency of the default microphone. Use 'unused' to ignore the minimum frequency.
        int unused;
        Microphone.GetDeviceCaps(deviceName, out unused, out samplingRate);

        // Use this string to cache the text currently displayed in the text box.
        textSoFar = new StringBuilder();

        // Use this to reset the UI once the Microphone is done recording after it was started.
        hasRecordingStarted = false;
    }
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    /// <summary>
    /// Turns on the dictation recognizer and begins recording audio from the default microphone.
    /// </summary>
    /// <returns>The audio clip recorded from the microphone.</returns>
    public AudioClip StartRecording()
    {
        // 3.a Shutdown the PhraseRecognitionSystem. This controls the KeywordRecognizers
        PhraseRecognitionSystem.Shutdown();

        // 3.a: Start dictationRecognizer
        dictationRecognizer.Start();

        // 3.a Uncomment this line
        dictationResult.text = "Dictation is starting. It may take time to display your text the first time, but begin speaking now...";

        // Set the flag that we've started recording.
        hasRecordingStarted = true;

        // Start recording from the microphone for 10 seconds.
        return Microphone.Start(deviceName, false, messageLength, samplingRate);
        
    }

    /// <summary>
    /// Ends the recording session.
    /// </summary>
    public void StopRecording()
    {
        // 3.a: Check if dictationRecognizer.Status is Running and stop it if so
        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        Microphone.End(deviceName);
    }

    /// <summary>
    /// This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
    /// </summary>
    /// <param name="text">The currently hypothesized recognition.</param>
    private void DictationRecognizer_DictationHypothesis(string text)
    {
        // 3.a: Set DictationDisplay text to be textSoFar and new hypothesized text
        // We don't want to append to textSoFar yet, because the hypothesis may have changed on the next event
        dictationResult.text = textSoFar.ToString() + " " + text + "...";
    }

    /// <summary>
    /// This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
    /// </summary>
    /// <param name="text">The text that was heard by the recognizer.</param>
    /// <param name="confidence">A representation of how confident (rejected, low, medium, high) the recognizer is of this recognition.</param>
    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        // 3.a: Append textSoFar with latest text
        textSoFar.Append(text + ". ");

        // 3.a: Set DictationDisplay text to be textSoFar
        dictationResult.text = textSoFar.ToString();
    }

    /// <summary>   
    /// This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
    /// Typically, this will simply return "Complete". In this case, we check to see if the recognizer timed out.
    /// </summary>
    /// <param name="cause">An enumerated reason for the session completing.</param>
    private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    {
        // If Timeout occurs, the user has been silent for too long.
        // With dictation, the default timeout after a recognition is 20 seconds.
        // The default timeout with initial silence is 5 seconds.
        if (cause == DictationCompletionCause.TimeoutExceeded)
        {
            Microphone.End(deviceName);

            dictationResult.text = "Timeout";
            SendMessage("ResetAfterTimeout");
        }
    }

    /// <summary>
    /// This event is fired when an error occurs.
    /// </summary>
    /// <param name="error">The string representation of the error reason.</param>
    /// <param name="hresult">The int representation of the hresult.</param>
    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        // 3.a: Set DictationDisplay text to be the error string
        dictationResult.text = error + "\nHRESULT: " + hresult;
    }

    private IEnumerator RestartSpeechSystem(KeywordManager keywordToStart)
    {
        while (dictationRecognizer != null && dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            yield return null;
        }

        keywordToStart.StartKeywordRecognizer();
    }

    void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
    {
        if (!hasRecordingStarted)
            StartRecording();

        else
            StopRecording();
            
    }
}
