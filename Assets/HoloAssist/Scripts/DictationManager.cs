using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;

[RequireComponent(typeof(AudioSource))]
public class DictationManager : MonoBehaviour,IInputClickHandler
{

    public Text dictationResult;
    public int DefaultFontSize = 14;

    private DictationRecognizer dictationRecognizer;
    private StringBuilder textSoFar;
    private bool hasRecordingStarted;

    // Using an empty string specifies the default microphone. 
    private static string deviceName = string.Empty;

    private int samplingRate;
    private const int messageLength = 300;

    private bool isRecording;

    float sumOfValues = 0;
    float countOfValues = 0;

    public int AmplFactor = 1000;

    /// <summary>
    /// Which type of microphone/quality to access
    /// </summary>
    public MicStream.StreamCategory StreamType = MicStream.StreamCategory.ROOM_CAPTURE;

    /// <summary>
    /// can boost volume here as desired. 1 is default but probably too quiet. can change during operation. 
    /// </summary>
    public float InputGain = 1;


    private bool KeepAllData = false;

    /// <summary>
    /// Should the mic stream start automatically when this component is enabled?
    /// </summary>
    private bool AutomaticallyStartStream = true;

    /// <summary>
    /// Records estimation of volume from the microphone to affect other elements of the game object
    /// </summary>
    private float averageAmplitude = 0;


    private void OnAudioFilterRead(float[] buffer, int numChannels)
    {
        // this is where we call into the DLL and let it fill our audio buffer for us
        CheckForErrorOnCall(MicStream.MicGetFrame(buffer, buffer.Length, numChannels));

        // figure out the average amplitude from this new data
        for (int i = 0; i < buffer.Length; i++)
        {
            sumOfValues += Mathf.Abs(buffer[i]);
            countOfValues++;
        }
    }

    private void Awake()
    {
        

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


        dictationResult.text = "Air tap to See what they Say";

        //Always be recording
        Microphone.Start(deviceName, true, messageLength, samplingRate);

        CheckForErrorOnCall(MicStream.MicInitializeCustomRate((int)StreamType, AudioSettings.outputSampleRate));
        CheckForErrorOnCall(MicStream.MicSetGain(InputGain));

        if (AutomaticallyStartStream)
        {
            CheckForErrorOnCall(MicStream.MicStartStream(KeepAllData, false));
        }

    }


    private void OnDestroy()
    {
        CheckForErrorOnCall(MicStream.MicDestroy());
    }

    private void CheckForErrorOnCall(int returnCode)
    {
        MicStream.CheckForErrorOnCall(returnCode);
    }
    // Use this for initialization
    void Start()
    {

        //this.gameObject.GetComponent<AudioSource>().volume = 0; // can set to zero to mute mic monitoring


        StartRecording();

        InputManager.Instance.AddGlobalListener(this.gameObject);


    }

    // Update is called once per frame
    void Update()
    {

    }
    /// <summary>
    /// Turns on the dictation recognizer and begins recording audio from the default microphone.
    /// </summary>
    /// <returns>The audio clip recorded from the microphone.</returns>
    public void StartRecording()
    {

        // Shutdown the PhraseRecognitionSystem. This controls the KeywordRecognizers
        PhraseRecognitionSystem.Shutdown();

        dictationRecognizer.Start();

        dictationResult.text = "Welcome to <size=20> See </size>what they Say";

        // Set the flag that we've started recording.
        hasRecordingStarted = true;

        
    }

    /// <summary>
    /// Ends the recording session.
    /// </summary>
    public void StopRecording()
    {
        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        //Microphone.End(deviceName);
    }

    /// <summary>
    /// This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
    /// </summary>
    /// <param name="text">The currently hypothesized recognition.</param>
    private void DictationRecognizer_DictationHypothesis(string text)
    {
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

        if (countOfValues != 0)
            averageAmplitude = sumOfValues / countOfValues;

        sumOfValues = 0;
        countOfValues = 0;

        //int scaleAmplitude = (int) (averageAmplitude * (float) AmplFactor);
        //scaleAmplitude = (5 / 1000) * scaleAmplitude;
        //if (scaleAmplitude > 5) scaleAmplitude = 5;
        //int fontSize = DefaultFontSize;
        //fontSize = fontSize + (int)scaleAmplitude;
        int fontSize = (int)averageAmplitude;

        if (textSoFar.Length > 200) textSoFar.Length = 0;
        if (averageAmplitude < 14) fontSize = 14;
        if (averageAmplitude > 20) DefaultFontSize = 20;
        textSoFar.Append("<size=" + fontSize + ">" + text + "</size>. ");
        
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
            //Microphone.End(deviceName);
            dictationRecognizer.Start();

            dictationResult.text = "";

            textSoFar.Length = 0;

            sumOfValues = 0;
            countOfValues = 0;
            //SendMessage("ResetAfterTimeout");
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

        sumOfValues = 0;
        countOfValues = 0;
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
        dictationResult.text = "Welcome to <size=20> See </size>what they Say";
        sumOfValues = 0;
        countOfValues = 0;
        textSoFar.Length = 0;
        //Always be recording
        //if (!hasRecordingStarted)

        //    StartRecording();

        //else
        //    StopRecording();

    }

#if DOTNET_FX
        // on device, deal with all the ways that we could suspend our program in as few lines as possible
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                CheckForErrorOnCall(MicStream.MicPause());
            }
            else
            {
                CheckForErrorOnCall(MicStream.MicResume());
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            this.OnApplicationPause(!focused);
        }

        private void OnDisable()
        {
            this.OnApplicationPause(true);
        }

        private void OnEnable()
        {
            this.OnApplicationPause(false);
        }
#endif
}
