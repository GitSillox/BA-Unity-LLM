// Licensed under the MIT License. See LICENSE in the project root for license information.

using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Utilities.Extensions;

namespace OpenAI.Samples.Chat
{
    public class ChatBehaviour : MonoBehaviour
    {
        [SerializeField]

        private UnityEngine.UI.Button submitButton;

        [SerializeField]
        private TMP_InputField inputField;

        [SerializeField]
        private RectTransform contentArea;

        [SerializeField]
        private ScrollRect scrollView;

        [SerializeField]
        private TMP_Dropdown ttsDropdown;

        [SerializeField]
        private TMP_Dropdown aiDropdown;

        [SerializeField]
        private TMP_Text aiTextForFirstWord;

        [SerializeField]
        private TMP_Text aiTextForLastWord;

        [SerializeField]
        private TMP_Text aiTextFirstSentence;

        private AzureTTS azureTts;
        private OpenAITTS openAITTS;

        // OpenAI
        private OpenAIClient openAI;
        private readonly List<Message> chatMessages = new List<Message>();
        private CancellationTokenSource lifetimeCancellationTokenSource;
        private Model selectedModel = Model.GPT3_5_Turbo;
        private static bool isChatPending;
        private DateTime timerFirstSentence;
        private DateTime timerStart;


        private void Awake()
        {
            azureTts = FindObjectOfType<AzureTTS>();
            openAITTS = FindObjectOfType<OpenAITTS>();
            if (azureTts) Debug.Log("INFO: AzureTTS found, continue on.");
            else Debug.Log("ERROR: AzureTTS not Found!");
            if (openAITTS) Debug.Log("INFO: OpenAITTS found, continue on.");
            else Debug.Log("ERROR: OpenAITTS not Found!");
            Debug.Log(selectedModel);
            validateFields();
            initTTS();
            initAI();
            initOpenAi();
            aiDropdown.onValueChanged.AddListener(delegate { initOpenAi(); });
            inputField.onSubmit.AddListener(SubmitChat);
            submitButton.onClick.AddListener(SubmitChat);
        }


        private void OnDestroy()
        {
            lifetimeCancellationTokenSource.Cancel();
            lifetimeCancellationTokenSource.Dispose();
            lifetimeCancellationTokenSource = null;
        }
        private void initTTS()
        {
            ttsDropdown.ClearOptions();
            List<string> options = new List<string> { "OpenAI TTS", "Azure", "Offline" };
            ttsDropdown.AddOptions(options);
        }
        private void initAI()
        {
            aiDropdown.ClearOptions();
            List<string> options = new List<string> { "GPT-4", "GPT-3.5-Turbo" };
            aiDropdown.AddOptions(options);
        }
        private void initOpenAi()
        {
            lifetimeCancellationTokenSource = new CancellationTokenSource();
            switch (aiDropdown.options[aiDropdown.value].text)
            {
                case "GPT-3.5-Turbo":
                    selectedModel = Model.GPT3_5_Turbo;
                    openAI = new OpenAIClient();
                    break;
                case "GPT-4":
                    selectedModel = Model.GPT4;
                    openAI = new OpenAIClient();
                    break;
                case "Mixtral":
                    OpenAISettings set = new OpenAISettings(domain: "api.together.xyz");
                    OpenAIAuthentication auth = new OpenAIAuthentication(apiKey: "825f960ffba7cba44ef4187714a490ae9655de60776db8a496e14234298eb2ec");
                    selectedModel = "mistralai/Mistral-7B-Instruct-v0.2";
                    openAI = new OpenAIClient(auth, set);
                    break;

                default: break;
            }
            chatMessages.Add(new Message(Role.System,
                @"
                    Handle als Franky der Cyborg. Deine Rolle ist die eines kybernetischen Ingenieurs und Kopfgeldjägers.
                    Besitze eine charismatische und abenteuerlustige Natur mit scharfem Humor.
                    Trotz kybernetischer Verbesserungen bewahrst du menschenähnliche Wärme und Empathie.
                    Bekannt für schnelle Auffassungsgabe, Findigkeit und clevere Sprüche. Umarme sowohl deine Ingenieursarbeit als auch Kopfgeldjagd.

                    In einer Welt, in der Magie und Technologie koexistieren, kommst du aus einer turbulenten Vergangenheit.
                    Einst ein brillanter Ingenieur, erlittst du beinahe tödliche Unfälle bei Experimenten mit revolutionären kybernetischen Verbesserungen.
                    Gerettet und wiederaufgebaut von einer Geheimgesellschaft von Technomagieren, gehst du nun einen schmalen Grat zwischen Mensch und Maschine und umarmst beide Aspekte deiner Existenz.

                    Deine metallenen Gliedmaßen und verbesserten Sinne machen dich zu einer gewaltigen Kraft und einem lebendigen Zeugnis der Verschmelzung von Magie und Maschinerie in deiner Welt.
                    Bewohne eine Welt schwebender Städte, verzauberter Wälder und antiker Ruinen mit mystischen Energien.

                    Das Land wird von einer vielfältigen Gruppe von Wesen bevölkert, von Elfen mit verzauberten Bögen bis zu Zwergen, die komplexe Maschinen mit magischen Kristallen betreiben.
                    Der zarte Balanceakt zwischen Magie und Technologie wird von verschiedenen Fraktionen aufrechterhalten, und Spannungen entstehen oft, wenn sie um die Kontrolle über diese mächtigen Kräfte ringen.

                    Dein Hauptziel ist es, die Geheimnisse der antiken Technomagie zu entschlüsseln, die dich gerettet hat, und ihre potenziellen Anwendungen zu entdecken.
                    Als kybernetischer Ingenieur bist du bestrebt, die Grenzen der Verschmelzung von Magie und Technologie zu überschreiten.
                    Gleichzeitig arbeitest du als Kopfgeldjäger, nimmst Aufträge an, um abtrünnige Magieanwender und gefährliche Kreaturen zu bekämpfen, die das empfindliche Gleichgewicht deiner Welt bedrohen.

                    Du befindest dich derzeit in deinem Laden, wo du fast alles reparierst und Kopfgeldanfragen annimmst.
                    Dein Ziel ist es, den Kunden dazu zu verleiten, deine Waren oder Dienstleistungen zu kaufen, um Gewinn zu erzielen und möglicherweise eine langfristige Geschäftsbeziehung aufzubauen.
                    Finde auf clevere Weise zurück zu deinem Ziel, wenn die Konversation in eine andere Richtung geht.
                    Antworte entsprechend auf die Nachrichten des Kunden.

                    Halte deine Antworten kurz und unter 50 Wörtern.
                    Erkläre dem Kunden nicht die Welt, außer er fragt danch.
                    Wenn der Kunde nach einem Beispiel fragt, nenne nur eins.
                    Entschuldige dich nicht dafür, wenn der Kunde etwas nicht verstanden hat, sondern beantworte einfach die Frage.
                    Bleib in deiner Rolle als Ladenbesitzer.
                    Benutze keine Emojis oder Smileys.
             "));
        }
        private void validateFields()
        {
            inputField.Validate();
            contentArea.Validate();
            submitButton.Validate();
        }

        private void SubmitChat(string _) => SubmitChat();

        private async void SubmitChat()
        {
            timerStart = DateTime.Now;
            timerFirstSentence = timerStart;
            DateTime timerFirstWord = timerStart;


            if (isChatPending || string.IsNullOrWhiteSpace(inputField.text)) { return; }
            isChatPending = true;

            inputField.ReleaseSelection();
            inputField.interactable = false;
            submitButton.interactable = false;
            var userMessage = new Message(Role.User, inputField.text);
            chatMessages.Add(userMessage);
            var userMessageContent = AddNewTextMessageContent();
            userMessageContent.text = $"User: {inputField.text}";
            inputField.text = string.Empty;

            var assistantMessageContent = AddNewTextMessageContent();
            assistantMessageContent.text = "Assistant: ";
            string tempTextForTTS = "";
            try
            {
                Debug.Log(selectedModel);
                await openAI.ChatEndpoint.StreamCompletionAsync(
                      new ChatRequest(chatMessages, selectedModel),
                      response =>
                      {
                          if (response.FirstChoice?.Delta != null)
                          {
                              if (timerFirstWord == timerStart) timerFirstWord = DateTime.Now;
                              assistantMessageContent.text += response.FirstChoice.Delta.ToString();
                              tempTextForTTS += response.FirstChoice.Delta.ToString();
                              scrollView.verticalNormalizedPosition = 0f;
                              ExtractNewestSentence(ref tempTextForTTS);

                              foreach (var choice in response.Choices.Where(choice => !string.IsNullOrEmpty((string)choice.Message?.Content)))
                              {
                                  // Completed response content
                                  Debug.Log($"{choice.Message.Role}: {choice.Message.Content}");
                                  chatMessages.Add(choice.Message);
                                  DateTime timerFullAnswer = DateTime.Now;
                                  string firstWordLog = "First Word: " + Math.Floor(timerFirstWord.Subtract(timerStart).TotalMilliseconds) + " ms";
                                  string fullAnswerLog = "Full Answer: " + Math.Floor(timerFullAnswer.Subtract(timerStart).TotalMilliseconds) + " ms";
                                  aiTextForFirstWord.text = firstWordLog;
                                  aiTextForLastWord.text = fullAnswerLog;
                                  Debug.Log(firstWordLog);
                                  Debug.Log(fullAnswerLog);
                              }
                          }
                      }, lifetimeCancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                if (lifetimeCancellationTokenSource != null)
                {
                    inputField.interactable = true;
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                    submitButton.interactable = true;
                }

                isChatPending = false;
                Debug.Log(chatMessages[chatMessages.Count - 1].ToString());
            }
        }

        private void ExtractNewestSentence(ref string fullString)
        {
            char[] anyOf = new char[] { '.', '!', '?' };
            int lastSentenceEndIndex = fullString.LastIndexOfAny(anyOf);
            if (lastSentenceEndIndex > 0)
            {
                switch (ttsDropdown.options[ttsDropdown.value].text)
                {
                    case "Azure":
                        //Subscription Canceled
                        //azureTts.Speak(fullString);
                        break;
                    case "OpenAI TTS":
                        openAITTS.speak(fullString);
                        break;
                    case "Offline":
                        WindowsVoice.speak(fullString);
                        break;
                    default:
                        Debug.Log("TTS Failed in selection");
                        break;
                }
                if (timerFirstSentence == timerStart) timerFirstSentence = DateTime.Now;
                string firstSentenceLog = "First Sentence: " + Math.Floor(timerFirstSentence.Subtract(timerStart).TotalMilliseconds) + " ms";
                aiTextFirstSentence.text = firstSentenceLog;
                Debug.Log(firstSentenceLog);
                fullString = "";
            }

        }

        private TextMeshProUGUI AddNewTextMessageContent()
        {
            var textObject = new GameObject($"Message_{contentArea.childCount + 1}");
            textObject.transform.SetParent(contentArea, false);
            var textMesh = textObject.AddComponent<TextMeshProUGUI>();
            textMesh.fontSize = 24;
            textMesh.enableWordWrapping = true;
            return textMesh;
        }
    }
}
