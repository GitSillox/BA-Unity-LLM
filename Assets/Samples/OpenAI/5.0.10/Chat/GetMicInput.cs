using OpenAI.Audio;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenAI.Samples.Chat
{
    public class GetMicInput : MonoBehaviour
    {
        //These are the elements needed to enable the microphone in your software:
        [SerializeField] private Button startRecordButton;
        [SerializeField] private Button stopRecordButton;
        [SerializeField] private TMP_InputField message;
        [SerializeField] private TMP_Dropdown dropdown;

        private readonly int duration = 10;

        private AudioClip audioClip;
        private bool isRecording;
        private float time;
        private OpenAIClient api;

        //These are additional elements that manage the sound so it doesn't echo:

        // Start is called before the first frame update
        void Start()
        {
            api = new OpenAIClient();
            foreach (var device in Microphone.devices)
            {
                dropdown.AddOptions(new List<string>(Microphone.devices));
            }
            dropdown.RefreshShownValue();
            startRecordButton.onClick.AddListener(StartRecording);
            stopRecordButton.onClick.AddListener(stopRecordingEarly);
        }
        private void StartRecording()
        {
            isRecording = true;
            audioClip = Microphone.Start(dropdown.options[dropdown.value].text, false, duration, 44100);
        }
        private async void EndRecording()
        {
            message.text = "Transcripting...";
            isRecording = false;
            Microphone.End(null);
            var request = new AudioTranscriptionRequest(audioClip, language: "en");
            var result = await api.AudioEndpoint.CreateTranscriptionAsync(request);
            message.text = result;
        }
        private void stopRecordingEarly()
        {
            isRecording = false;
            EndRecording();
        }
        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                if (time >= duration)
                {
                    isRecording = false;
                    EndRecording();

                }
            }
        }
    }
}