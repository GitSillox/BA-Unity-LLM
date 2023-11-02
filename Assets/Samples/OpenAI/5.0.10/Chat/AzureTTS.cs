//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
// <code>
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;

public class AzureTTS : MonoBehaviour
{
    public AudioSource audioSource;

    // Replace with your own subscription key and service region (e.g., "westus").
    private const string SubscriptionKey = "9e20cd54c9e14724997ab6644510ba39";
    private const string Region = "germanywestcentral";

    private const int SampleRate = 24000;

    private bool audioSourceNeedStop;
    private SpeechConfig speechConfig;
    private SpeechSynthesizer synthesizer;

    public void Speak(string text)
    {
        // We can't await the task without blocking the main Unity thread, so we'll call a coroutine to
        // monitor completion and play audio when it's ready.
        var speakTask = synthesizer.StartSpeakingTextAsync(text);
        StartCoroutine(SpeakRoutine(speakTask));
    }

    IEnumerator SpeakRoutine(Task<SpeechSynthesisResult> speakTask)
    {
        var startTime = DateTime.Now;

        while (!speakTask.IsCompleted)
        {
            yield return null;
        }

        var result = speakTask.Result;
        {
            if (result.Reason == ResultReason.SynthesizingAudioStarted)
            {
                // Native playback is not supported on Unity yet (currently only supported on Windows/Linux Desktop).
                // Use the Unity API to play audio here as a short term solution.
                // Native playback support will be added in the future release.
                var audioDataStream = AudioDataStream.FromResult(result);
                while (!audioDataStream.CanReadData(4092 * 2)) // audio clip requires 4096 samples before it's ready to play
                {
                    yield return null;
                }

                var isFirstAudioChunk = true;
                var audioClip = AudioClip.Create(
                    "Speech",
                    SampleRate * 600, // Can speak 10mins audio as maximum
                    1,
                    SampleRate,
                    true,
                    (float[] audioChunk) =>
                    {
                        var chunkSize = audioChunk.Length;
                        var audioChunkBytes = new byte[chunkSize * 2];
                        var readBytes = audioDataStream.ReadData(audioChunkBytes);
                        if (isFirstAudioChunk && readBytes > 0)
                        {
                            var endTime = DateTime.Now;
                            var latency = endTime.Subtract(startTime).TotalMilliseconds;
                            Debug.Log($"Speech synthesis succeeded!\nLatency: {latency} ms.");
                            isFirstAudioChunk = false;
                        }

                        for (int i = 0; i < chunkSize; ++i)
                        {
                            if (i < readBytes / 2)
                            {
                                audioChunk[i] = (short)(audioChunkBytes[i * 2 + 1] << 8 | audioChunkBytes[i * 2]) / 32768.0F;
                            }
                            else
                            {
                                audioChunk[i] = 0.0f;
                            }
                        }

                        if (readBytes == 0)
                        {
                            Thread.Sleep(200); // Leave some time for the audioSource to finish playback
                            audioSourceNeedStop = true;
                        }
                    });

                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }
    }

    void Start()
    {
        // Creates an instance of a speech config with specified subscription key and service region.
        speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, Region);

        // The default format is RIFF, which has a riff header.
        // We are playing the audio in memory as audio clip, which doesn't require riff header.
        // So we need to set the format to raw (24KHz for better quality).
        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);

        // Creates a speech synthesizer.
        // Make sure to dispose the synthesizer after use!
        synthesizer = new SpeechSynthesizer(speechConfig, null);

        synthesizer.SynthesisCanceled += (s, e) =>
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(e.Result);
            Debug.Log($"CANCELED:\nReason=[{cancellation.Reason}]\nErrorDetails=[{cancellation.ErrorDetails}]\nDid you update the subscription info?");
        };
    }

    void Update()
    {
        if (audioSourceNeedStop)
        {
            audioSource.Stop();
            audioSourceNeedStop = false;
        }
    }

    void OnDestroy()
    {
        if (synthesizer != null)
        {
            synthesizer.Dispose();
        }
    }
}
// </code>
