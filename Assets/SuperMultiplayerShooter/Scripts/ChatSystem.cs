using UnityEngine;
using UnityEngine.UI;
using Photon.Chat;
//using Photon.Pun;
//using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Fusion;

namespace Visyde
{
    /// <summary>
    /// Chat System (Lobby in main menu only)
    /// - manages the chat system itself as well as the chat's UI in one script
    /// </summary>

    public class ChatSystem : MonoBehaviour, IChatClientListener
    {
        [Header("Settings:")]
        public Color ourColor;
        public Color othersColor;
        public Color ourChatColor;
        public Color othersChatColor;
        public Color positiveNotifColor;
        public Color negativeNotifColor;
        [Header("References:")]
        public Button startBTN;
        public Button startBTN2;
        public Text messageDisplay;
        public InputField inputField;
        public Button sendButton;
        public GameObject loadingIndicator;
        public Text chatText1;
        public Text chatText2;
        public GameObject playerItemPrefab;
        public Transform playerItemHandler;
        public Text currentNumberOfPlayersInRoomText;        

        public static ChatClient chatClient;
        //public bool HasChatAppID
        //{
        //    get
        //    {
        //        return !string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat);
        //    }
        //}

        // Internals:
        VerticalLayoutGroup vlg;

        // Use this for initialization
        async void Start()
        {
            vlg = messageDisplay.transform.parent.GetComponent<VerticalLayoutGroup>();

            messageDisplay.text = "";
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Game"))
            {
                for (int i = 0; i < Connector.instance.playersList.Count; i++)
                {
                    if (Connector.instance.networkRunner.IsServer)
                    {
                       await Connector.instance.networkRunner.SpawnAsync
                            (
                            playerItemPrefab,
                            playerItemHandler.position, Quaternion.identity,
                            Connector.instance.playersList[i]);
                    }
                    //Instantiate(playerItemPrefab, playerItemHandler);
                }
            }
            // Connect to the chat server when we join a room:
            ConnectToChat();
        }

        void OnEnable()
        {
            Connector.onJoinRoom += OnJoinedRoom;
            //Connector.onPlayerLeave += OnPlayerLeftRoom;
            //Connector.instance.onLeaveRoom += OnLeftRoom;
            Connector.onConnectedToClient += () => { OnJoinedRoom(PlayerRef.Invalid); };
        }
        void OnDisable()
        {
            Connector.onJoinRoom -= OnJoinedRoom;
            //Connector.onPlayerLeave -= OnPlayerLeftRoom;
            //Connector.instance.onLeaveRoom -= OnLeftRoom;
            Connector.onConnectedToClient -= () => { OnJoinedRoom(PlayerRef.Invalid); };
        }
        //public void OnPlayerLeftRoom(PlayerRef player)
        //{
        //    //for (int i = 0; i < Connector.instance.playersList.Count; i++)
        //    //{
        //    //    var playerItem = Instantiate(playerItemPrefab, playerItemHandler);
        //    //    playerItem.Set();
        //    //}
        //}
        // Update is called once per frame
        void Update()
        {
            if (chatClient != null)
            {
                chatClient.Service();

                // Functionalities:
                if (chatClient.CanChat)
                {
                    sendButton.interactable = true;
                    loadingIndicator.SetActive(false);
                    if (startBTN != null)
                    {
                        startBTN.interactable = Connector.instance.networkRunner.IsServer && 
                            Connector.instance.networkRunner.SessionInfo.PlayerCount > 0 && 
                            Connector.instance.lobbyItems.All(lobbyItem => !string.IsNullOrEmpty(lobbyItem.NickName));
                        startBTN2.interactable = Connector.instance.networkRunner.IsServer &&
                            Connector.instance.networkRunner.SessionInfo.PlayerCount > 0 &&
                            Connector.instance.lobbyItems.All(lobbyItem => !string.IsNullOrEmpty(lobbyItem.NickName));
                    }
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        SendChatMessage();
                    }
                }
                else
                {
                    sendButton.interactable = false;
                    loadingIndicator.SetActive(true);
                    if (startBTN != null)
                    {
                        startBTN.interactable = false;
                        startBTN2.interactable = false;
                    }
                }
            }
        }
        public void ConnectToChat()
        {
            // Only connect if we have a chat ID specified in the PhotonServerSettings:
            chatClient = new ChatClient(this);
            chatClient.Connect("07869b70-56b2-42a8-8ca6-22d75de21fae", Connector.instance.gameVersion, new Photon.Chat.AuthenticationValues(PlayerPrefs.GetString("USERNAME")));
        }
        public void SendChatMessage()
        {
            if (!string.IsNullOrEmpty(inputField.text))
            {
                chatClient.PublishMessage(Connector.instance.networkRunner.SessionInfo.Name, inputField.text);
                inputField.text = string.Empty;
            }
        }
        public void SendSystemChatMessage(string message, bool negative)
        {
            DisplayChat(message, "", false, true, negative);
        }
        void DisplayChat(string message, string from, bool ours, bool systemMessage, bool negative)
        {

            string finalMessage = "";

            if (systemMessage)
            {
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(negative ? negativeNotifColor : positiveNotifColor) + ">" + message + "</color>";
            }
            else
            {
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(ours ? ourColor : othersColor) + ">" + from + "</color>: <color=" + ColorUtility.ToHtmlStringRGBA(ours ? ourChatColor : othersChatColor) + ">" + message + "</color>";
            }
            messageDisplay.text += finalMessage;

            // Canvas refresh:
            Canvas.ForceUpdateCanvases();
            vlg.enabled = false;
            vlg.enabled = true;
        }

        // Photon stuff:
        void OnJoinedRoom(PlayerRef player)
        {
            messageDisplay.text = "";
            // Connect to the chat server when we join a room:
            ConnectToChat();
        }
        void OnLeftRoom()
        {
            // Disconnect from the chat server when we leave a room:
            if (chatClient != null) chatClient.Disconnect();
        }
        public void OnChatStateChange(ChatState state) { }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            // Get the name of the sender:
            int last = senders.Length - 1;
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Game"))
            {
                if (chattext2CR != null)
                {
                    StopCoroutine(chattext2CR);
                }
                if (chatText1.text != "")
                {
                    chattext2CR = StartCoroutine(ChatText2CR(text1Message, text1Sender, text1isOurs));
                }
                if (chattext1CR != null)
                {
                    StopCoroutine(chattext1CR);
                }
                chattext1CR = StartCoroutine(ChatText1CR(messages[last].ToString(), senders[last], senders[last] == PlayerPrefs.GetString("USERNAME")));
            }
            // Display the message:
            DisplayChat(messages[last].ToString(), senders[last], senders[last] == PlayerPrefs.GetString("USERNAME"), false, false);
        }
        Coroutine chattext1CR;
        string text1Sender;
        string text1Message;
        bool text1isOurs;
        IEnumerator ChatText1CR(object message, string from, bool ours)
        {
            float elapsedTime = 0;
            text1Message = message.ToString();
            text1Sender = from;
            text1isOurs = ours;
            while (elapsedTime <= 3)
            {
                string finalMessage = "<color=#" + ColorUtility.ToHtmlStringRGBA(ours ? ourColor : othersColor) + ">" + from + "</color>:<color=" + ColorUtility.ToHtmlStringRGBA(ours ? ourChatColor : othersChatColor) + ">" + message + "</color>";
                chatText1.gameObject.SetActive(true);
                chatText1.text = finalMessage;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            chatText1.gameObject.SetActive(false);
            chatText1.text = "";
            chattext1CR = null;
        }
        Coroutine chattext2CR;
        IEnumerator ChatText2CR(object message, string from, bool ours)
        {
            float elapsedTime = 0;
            while (elapsedTime <= 3)
            {
                string finalMessage = "<color=#" + ColorUtility.ToHtmlStringRGBA(ours ? ourColor : othersColor) + ">" + from + "</color>:<color=" + ColorUtility.ToHtmlStringRGBA(ours ? ourChatColor : othersChatColor) + ">" + message + "</color>";
                chatText2.gameObject.SetActive(true);
                chatText2.text = finalMessage;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            chatText2.gameObject.SetActive(false);
            chatText2.text = "";
            chattext2CR = null;
        }
        public static Action<string, string> OnRoomInvite = delegate { };
        public static Action<string> OnFriendRequestSent = delegate { };
        public static Action<string> OnFriendRequestAccept = delegate { };
        private static List<string> lastMessageIDs = new List<string>();
        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            if (!string.IsNullOrEmpty(message.ToString()))
            {
                var splitnames = channelName.Split(new char[] { ':' });
                var senderName = splitnames[0];

                if (!sender.Equals(senderName, System.StringComparison.OrdinalIgnoreCase))
                {
                    string[] stringsArray = message.ToString().Split("?");
                    if (!lastMessageIDs.Contains(stringsArray[1]) && stringsArray[0] == "ADMINFRIENDREQUESTFUNCTION")
                    {
                        OnFriendRequestSent(sender);
                    }
                    else if (!lastMessageIDs.Contains(stringsArray[1]) && stringsArray[0] == "ADMINFRIENDREQUESTACCEPTFUNCTION")
                    {
                        OnFriendRequestAccept(sender);
                    }
                    else if (!lastMessageIDs.Contains(stringsArray[2]) && stringsArray[1] == "ADMININVITELOBBYFUNCTION")
                    {
                        OnRoomInvite(sender, stringsArray[0]);
                    }
                    lastMessageIDs.Add(stringsArray[stringsArray.Length > 2 ? 2 : 1]);
                }
            }
        }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }
        public void OnConnected()
        {
            if (Connector.instance.networkRunner.IsInSession)
            {
                chatClient.Subscribe(new string[] { Connector.instance.networkRunner.SessionInfo.Name }, 0); // subscribe to the chat channel once connected to the chat server
            }
        }
        public void OnDisconnected() { }
        public void OnSubscribed(string[] channels, bool[] results) { }
        public void OnUnsubscribed(string[] channels) { }
        public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
        {
            if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
            {
                UnityEngine.Debug.LogError(message);
            }
            else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
            {
                UnityEngine.Debug.LogWarning(message);
            }
            else
            {
                UnityEngine.Debug.Log(message);
            }
        }
    }
}