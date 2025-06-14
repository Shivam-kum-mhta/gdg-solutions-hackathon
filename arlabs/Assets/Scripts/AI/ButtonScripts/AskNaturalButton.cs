using UnityEngine;
using ARLabs.Core;
using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.UI;
namespace ARLabs.AI
{
    // Handles the ask natural button
    public class AskNaturalButton : MonoBehaviour
    {

        [SerializeField] private GameObject suggestionsPanel;
        public CanvasGroup canvasGroup;
        public bool alsoSaveScreenshot = false;
        public UnityEngine.UI.Button recordButton;
        public Lean.Gui.LeanToggle voiceInputWindowToggle;
        public Lean.Gui.LeanToggle isRequestHelpToggle;
        public GameObject loadingIcon, mainText;
        public TMPro.TMP_Text transcriptDisplay, recordTimeText;

        private string _latestImage = "";
        private ExperimentContext _latestExperimentContext;


        // When the button on the AI tray is clicked, 
        // which opens the menu for voice and image capture
        public void OnClick()
        {
            ExperimentContext experiment = ExperimentContext.GetExperimentContext();
            _latestExperimentContext = experiment;
            Debug.Log("Setting experiment context");
        }

        // When the record button is held down  
        public void OnBeginRecord()
        {
            ExperimentManager.Instance.StartScreenCapture(canvasGroup, (base64Image) =>
            {
                _latestImage = base64Image;
            });
            suggestionsPanel.SetActive(false);
            GoogleSTT.Instance.StartRecording();
        }

        // When the record button is released   
        public void OnEndRecord()
        {
            if (GoogleSTT.Instance.IsRecording)
            {
                GoogleSTT.Instance.StopRecording();
                loadingIcon.SetActive(true);
                RecordButtonSetActive(false);

                FinalizeChatMessage();
            }
        }

        // Finalizes the chat message and send to backend
        public async void FinalizeChatMessage()
        {
            // Transcribe audio
            string transcript = await GoogleSTT.Instance.TranscribeSavedClip();
            if (transcript == "")
            {
                ResetUI();
                return;
            }

            // Replace instructions UI with the transcript
            mainText.SetActive(false);
            transcriptDisplay.text = "\"" + transcript + "\"";

            // Create chat message object
            AIChatMessage aiChatMessage = new AIChatMessage();
            aiChatMessage.sessionID = ExperimentManager.Instance.SessionID;
            aiChatMessage.prompt = transcript;
            aiChatMessage.experimentContext = _latestExperimentContext;
            aiChatMessage.base64Image = _latestImage;

            // Send only the JSON object
            string aiChatMessageJson = JsonUtility.ToJson(aiChatMessage);
            Debug.Log("AI Chat Message JSON: " + aiChatMessageJson);
            try
            {
                string response = "";
                if (isRequestHelpToggle.On)
                {
                    // Send the request to the backend
                    aiChatMessage.base64Image = ""; // Clear the image for help requests
                    response = await APIHandler.Instance.AskBackendHelp(aiChatMessageJson);
                    Debug.Log("Response: " + response);
                    ResponseManager.Instance.ProcessResponse(response);
                }
                else
                {
                    // Send the request to the backend
                    response = await APIHandler.Instance.AskBackend(aiChatMessageJson);
                    Debug.Log("Response: " + response);
                    ResponseManager.Instance.ProcessResponse(response);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error: " + e.Message);
            }

            ResetUI();
        }

        public void OnCancel()
        {
            GoogleSTT.Instance.CancelRecording();
            ResetUI();
        }

        public void ResetUI()
        {
            if (GoogleSTT.Instance.IsRecording)
            {
                GoogleSTT.Instance.CancelRecording();
            }
            suggestionsPanel.SetActive(true);
            loadingIcon.SetActive(false);
            RecordButtonSetActive(true);
            transcriptDisplay.text = "";
            mainText.SetActive(true);
            voiceInputWindowToggle.TurnOff();
            _latestImage = "";
        }

        private void RecordButtonSetActive(bool state)
        {
            recordButton.interactable = state;
            recordButton.GetComponent<Image>().raycastTarget = state;
        }
    }
}
