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
        [SerializeField] private TMP_Dropdown micDropdown;

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
            micDropdown.ClearOptions();
            micDropdown.AddOptions(new List<string>(Microphone.devices));
            micDropdown.RefreshShownValue();
            startRecordButton.onClick.AddListener(StartRecording);
            stopRecordButton.onClick.AddListener(EndRecording);
        }
        private void StartRecording()
        {
            isRecording = true;
            audioClip = null;
            audioClip = Microphone.Start(micDropdown.options[micDropdown.value].text, false, duration, 44100);
        }
        private async void EndRecording()
        {
            isRecording = false;
            message.text = "Transcripting...";
            Microphone.End(micDropdown.options[micDropdown.value].text);
            var request = new AudioTranscriptionRequest(audioClip, language: "de");
            var result = await api.AudioEndpoint.CreateTranscriptionAsync(request);
            message.text = result;
            time = 0f;
        }
        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                if (time >= duration)
                {
                    EndRecording();
                }
            }
        }
    }
}