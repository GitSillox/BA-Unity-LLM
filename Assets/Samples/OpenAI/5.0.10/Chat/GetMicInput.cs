using OpenAI.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace OpenAI.Samples.Chat
{
    public class GetMicInput : MonoBehaviour
    {
        //These are the elements needed to enable the microphone in your software:
        [SerializeField] private Button startRecordButton;
        [SerializeField] private InputField message;
        [SerializeField] private Dropdown dropdown;

        private readonly int duration = 5;

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
                dropdown.options.Add(new Dropdown.OptionData(device));
            }
            startRecordButton.onClick.AddListener(StartRecording);
        }
        private void StartRecording()
        {
            isRecording = true;
            startRecordButton.enabled = false;

            audioClip = Microphone.Start(dropdown.options[dropdown.value].text, false, duration, 44100);
        }
        private async void EndRecording()
        {
            message.text = "Transcripting...";
            isRecording = false;
            Microphone.End(null);
            var request = new AudioTranscriptionRequest(audioClip, language: "en");
            var result = await api.AudioEndpoint.CreateTranscriptionAsync(request);
            Debug.Log(result);
            message.text = result;
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