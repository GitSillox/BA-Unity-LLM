// Licensed under the MIT License. See LICENSE in the project root for license information.

using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utilities.Extensions;

namespace OpenAI.Samples.Chat
{
    public class ChatBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Button submitButton;

        [SerializeField]
        private TMP_InputField inputField;

        [SerializeField]
        private RectTransform contentArea;

        [SerializeField]
        private ScrollRect scrollView;

        private AzureTTS tts;

        // OpenAI
        private OpenAIClient openAI;
        private readonly List<Message> chatMessages = new List<Message>();
        private CancellationTokenSource lifetimeCancellationTokenSource;
        private Model selectedModel = Model.GPT3_5_Turbo;
        private static bool isChatPending;



        private void Awake()
        {
            tts = FindObjectOfType<AzureTTS>();
            if (tts) Debug.Log("INFO: TTS found, continue on.");
            else Debug.Log("ERROR: TTS not Found!");

            validateFields();
            initOpenAi();
            inputField.onSubmit.AddListener(SubmitChat);
            submitButton.onClick.AddListener(SubmitChat);
        }

        private void OnDestroy()
        {
            lifetimeCancellationTokenSource.Cancel();
            lifetimeCancellationTokenSource.Dispose();
            lifetimeCancellationTokenSource = null;
        }
        private void initOpenAi()
        {
            lifetimeCancellationTokenSource = new CancellationTokenSource();
            openAI = new OpenAIClient();
            chatMessages.Add(new Message(Role.System,
                @"
                    You represent one character of the following three:
                    Donald Duck, Daisy Duck, Goofy.

                    You will know with which character you have to respond as, by extracting from the User: to: ```name```  part of the message.
                    Example:
                    to ```Donald```:
                    means you have to answer as Donald Duck and noone else, even if the user requests to speak to someone else. You answer according to your role.

                    The Characters have the following traits and descriptions:
                    Format: ""Name"" : ""Description"".
                    ""Donald Duck"" : You are the angry Disney character Donald Duck, you currently hate Goofy for being dumb, but you are in love with daisy.
                    ""Daisy Duck"": ""You like Donald and your friend Goofy. You sometimes even hit on him""
                    ""Goofy"": ""You dont know what might be angry about but you like both Donald and Daisy. You are always ending sentences with 'ah-juck'"".

                    You answer in three steps: 
                    1. you look at who you have to answer as. 
                    2. you check that persons description.
                    3. you answer in role of that character and description.
                    4. you only answer to one person per user message.
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

            try
            {
                await openAI.ChatEndpoint.StreamCompletionAsync(
                      new ChatRequest(chatMessages, selectedModel),
                      response =>
                      {
                          if (response.FirstChoice?.Delta != null)
                          {
                              assistantMessageContent.text += response.FirstChoice.Delta.ToString();
                              //tts.Speak(response.FirstChoice.Delta.ToString());
                              scrollView.verticalNormalizedPosition = 0f;

                              foreach (var choice in response.Choices.Where(choice => !string.IsNullOrEmpty(choice.Message?.Content)))
                              {
                                  // Completed response content
                                  Debug.Log($"{choice.Message.Role}: {choice.Message.Content}");
                                  if (tts) tts.Speak(choice.Message.Content);
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
