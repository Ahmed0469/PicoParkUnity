using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Visyde
{
    /// <summary>
    /// Sample Main Menu
    /// - A sample script that handles the main menu UI.
    /// </summary>

    public class SampleMainMenu : MonoBehaviour
    {
        [Header("Main:")]
        public GameObject splashScreenObj;
        public Text connectionStatusText;
        public Button findMatchBTN;
        public Button customMatchBTN;
        public Button friendsBtn;
        public GameObject findMatchCancelButtonObj;
        public GameObject findingMatchPanel;
        public GameObject customGameRoomPanel;
        public Text matchmakingPlayerCountText;
        public InputField playerNameInput;
        public GameObject messagePopupObj;
        public Text messagePopupText;
        public GameObject characterSelectionPanel;
        public Image characterIconPresenter;
        public GameObject loadingPanel;
        public Toggle frameRateSetting;

        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            // Load or create a username:
            //if (PlayerPrefs.HasKey("name"))
            //{
            //    playerNameInput.text = PlayerPrefs.GetString("name");
            //}
            //else
            //{
            playerNameInput.text = PlayerPrefs.GetString("USERNAME");
            //}
            SetPlayerName();
            StartCoroutine(SplashScreen());
            // Others:
            //frameRateSetting.isOn = Application.targetFrameRate == 60;
        }
        public IEnumerator SplashScreen()
        {
            splashScreenObj.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            splashScreenObj.SetActive(false);
        }
        // Update is called once per frame
        void Update()
        {
            bool connecting = !Connector.instance.networkRunner.IsCloudReady;

            // Handling texts:
            connectionStatusText.text = connecting ? /*Connector.instance.networkRunner.State == Fusion.NetworkRunner.States.Starting ? "Connecting..." :*/ "Finding network..."
                : $"Connected! {Connector.instance.networkRunner.LobbyInfo.Region}";// | Ping: " + Connector.instance.networkRunner.GetPlayerRtt(Connector.instance.networkRunner.LocalPlayer) 100.0f;
            connectionStatusText.color = !connecting ? Color.green : Color.yellow;
            matchmakingPlayerCountText.text = Connector.instance.networkRunner.IsInSession ? Connector.instance.totalPlayerCount + "/" + Connector.instance.networkRunner.SessionInfo.MaxPlayers : "Matchmaking...";

            // Handling buttons:
            customMatchBTN.interactable = !connecting;
            friendsBtn.interactable = !connecting;
            findMatchBTN.interactable = !connecting;
            findMatchCancelButtonObj.SetActive(Connector.instance.networkRunner.IsInSession);

            // Handling panels:
            customGameRoomPanel.SetActive(Connector.instance.isInCustomGame);
            loadingPanel.SetActive(connecting);
            // Messages popup system (used for checking if we we're kicked or we quit the match ourself from the last game etc):
            if (DataCarrier.message.Length > 0)
            {
                messagePopupObj.SetActive(true);
                messagePopupText.text = DataCarrier.message;
                DataCarrier.message = "";
            }
        }

        // Profile:
        public void SetPlayerName()
        {
            //PlayerPrefs.SetString("name", playerNameInput.text);
            //PhotonNetwork.NickName = playerNameInput.text;
        }

        // Main:
        public void FindMatch(){
            // Enable the "finding match" panel:
            findingMatchPanel.SetActive(true);
            // ...then finally, find a match:
            Connector.instance.FindMatch();
        }

        // Others:
        // *called by the toggle itself in the "On Value Changed" event:
        public void ToggleTargetFps(){
            //Application.targetFrameRate = frameRateSetting.isOn? 60 : 30;

            // Display a notif message:
            if (frameRateSetting.isOn){
                DataCarrier.message = "Target frame rate has been set to 60.";
            }
        }
    }
}