using Microsoft.CognitiveServices.Speech;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class OpenAITTS : MonoBehaviour

{
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private TMP_Text ttsDurationLog;

    private OpenAIClient openAI;
    private CancellationTokenSource lifetimeCancellationTokenSource;
    private Queue<AudioClip> queue = new Queue<AudioClip>();
    private int sentenceRecievedCounter;
    private int sentencePlayedCounter;


    void Start()
    {
        lifetimeCancellationTokenSource = new CancellationTokenSource();
        openAI = new OpenAIClient();
        sentenceRecievedCounter = 0;
        sentencePlayedCounter = 0;
    }
    public void speak(string text)
    {
        var request = new SpeechRequest(text, voice: SpeechVoice.Onyx, responseFormat: SpeechResponseFormat.MP3);
        var speakTask = openAI.AudioEndpoint.CreateSpeechAsync(request);
        StartCoroutine(SpeakRoutine(speakTask));
    }
    IEnumerator SpeakRoutine(Task<Tuple<string, AudioClip>> speakTask)
    {
        var startTime = DateTime.Now;
        int currentCount = sentenceRecievedCounter;
        sentenceRecievedCounter++;
        while (!speakTask.IsCompleted)
        {
            yield return null;
        }
        var (path, clip) = speakTask.Result;
        var endTime = DateTime.Now;
        if (currentCount == 0)
        {
            string ttsStartLog = "TTS: " + Math.Floor(endTime.Subtract(startTime).TotalMilliseconds) + " ms";
            ttsDurationLog.text = ttsStartLog;
            Debug.Log(ttsStartLog);
        }
        yield return new WaitUntil(() => currentCount == sentencePlayedCounter);
        queue.Enqueue(clip);
        yield return new WaitUntil(() => audioSource.isPlaying == false);
        if (queue.Count != 0)
        {
            audioSource.clip = queue.Dequeue();
            audioSource.Play();
            sentencePlayedCounter++;
        }

    }
    private void Update()
    {
        if (queue.Count == 0 && !audioSource.isPlaying)
        {
            if (sentencePlayedCounter == sentenceRecievedCounter)
            {
                sentencePlayedCounter = 0;
                sentenceRecievedCounter = 0;
            }
        }
    }
    private void OnDestroy()
    {
        lifetimeCancellationTokenSource.Cancel();
        lifetimeCancellationTokenSource.Dispose();
        lifetimeCancellationTokenSource = null;
    }
}
