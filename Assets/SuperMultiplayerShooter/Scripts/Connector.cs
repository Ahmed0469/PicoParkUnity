using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using Photon.Voice.Unity;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading;
using Fusion.Photon.Realtime;

namespace Visyde
{
    /// <summary>
    /// Connector
    /// - manages the initial connection and matchmaking
    /// </summary>

    public class Connector : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static Connector instance;
        public NetworkRunner networkRunnerPrefab;
        public NetworkRunner networkRunner;
        public Dictionary<int, PlayerData> playerData = new();
        public List<CustomGamePlayerItem> lobbyItems = new();

        [Header("Settings:")]
        public string gameVersion = "0.1";
        public string gameSceneName = "";
        public int requiredPlayers;
        public string[] maps;

        [Header("Bot Players:")]
        [Tooltip("This is only for matchmaking.")] public bool createBots;
        public float startCreatingBotsAfter;		// (Only if `createBots` is enabled) after this delay, the game will start on generating bots to fill up the room.
        public float minBotCreationTime;			// minimum bot join/creation delay
        public float maxBotCreationTime;			// maximum bot join/creation delay
        public string[] botPrefixes;                // names for bots

        [Header("Other References:")]
        public CharacterSelector characterSelector;
        public Recorder recorder;
        public static UnityAction onVoiceDetected;
        public static UnityAction onVoiceNotDetected;

        public bool tryingToJoinCustom { get; protected set; }
        bool inCustom;
        public bool isInCustomGame
        {
            get
            {
                return inCustom && networkRunner.IsInSession;
            }
        }

        public int selectedMap { get; protected set; }
        public int totalPlayerCount { get; protected set; }
        public List<SessionInfo> rooms { get; protected set; }

        public delegate void IntEvent(int i);
        //public UnityAction onRoomListChange;
        public IntEvent onRoomListChange;
        public UnityAction onCreateRoomFailed;
        public static UnityAction onConnectedToClient;
        public static UnityAction<PlayerRef> onJoinRoom;
        public UnityAction onLeaveRoom;
        public UnityAction onDisconnect;
        public delegate void PlayerEvent(PlayerRef player);
        public PlayerEvent onPlayerJoin;
        public static PlayerEvent onPlayerLeave;
        public static bool regionSelected = false;
        public static string selectedRegion;
        public static int selectedRegionPing;
        public Transform regionItemHandler;
        public Transform regionItem;
        public Transform regionSelectorPanel;
        CancellationTokenSource _tokenSource;
        // Internal variables:
        Bot[] curBots;
        int bnp;
        bool startCustomGameNow;
        bool loadNow;                       // if true, the game scene will be loaded. Matchmaking will set this to true instantly when enough 
                                            // players are present, custom games on the other hand will require the host to press the "Start" button first.
        bool isLoadingGameScene;

        class Bot
        {
            public string name;				// bot name
            public Vector3 scores; 			// x = kills, y = deaths, z = other scores
            public int characterUsing;		// the chosen character of the bot (index only)
            public int hat;
        }

        void Awake()
        {            
            if (instance != null && instance != this)
            {                
                Destroy(instance.gameObject);
                instance = null;
            }            
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (networkRunner == null)
            {
                networkRunner = FindObjectOfType<NetworkRunner>();
                if (networkRunner == null)
                {
                    networkRunner = Instantiate(networkRunnerPrefab);
                    recorder = networkRunner.GetComponent<Recorder>();
                }                
            }
            //networkRunner = Instantiate(networkRunnerPrefab).GetComponent<NetworkRunner>();
            networkRunner.AddCallbacks(this);
        }

        void Start()
        {
            //PhotonNetwork.AutomaticallySyncScene = true;
            loadNow = false;
            rooms = new List<SessionInfo>();
            //StartCoroutine(Reconnection());
            // Do connection loop:
        }
        private void OnDestroy()
        {
            if (networkRunner != null)
            {
                networkRunner.RemoveCallbacks(this);
            }
        }
        public async void Disconnect()
        {
            GameManager.gameMode = GameMode.SinglePlayer;
            playerData = new Dictionary<int, PlayerData>();
            playersList = new List<PlayerRef>();
            lobbyItems = new();
            networkRunner.RemoveCallbacks(this);
            await networkRunner.Shutdown();
            if (SceneManager.GetActiveScene().name == "MainMenu" && regionSelected)
            {
                networkRunner = Instantiate(networkRunnerPrefab);
                recorder = networkRunner.GetComponent<Recorder>();
                networkRunner.AddCallbacks(this);
                StartCoroutine(Reconnection());
            }            
        }
        public IEnumerator Reconnection()
        {
            if (regionSelected)
            {
                if (networkRunner == null)
                {
                    networkRunner = Instantiate(networkRunnerPrefab);
                    recorder = networkRunner.GetComponent<Recorder>();
                    networkRunner.AddCallbacks(this);
                }
                yield return new WaitForSeconds(0.5f);
                if (!networkRunner.IsCloudReady /*|| PhotonNetwork.NetworkClientState == ClientState.ConnectingToMasterServer*/)
                {
                    string roomName = PlayerPrefs.GetString("USERNAME") + "Default";
                    var customProps = new Dictionary<string, SessionProperty>();

                    customProps["isDefault"] = (bool)true;
                    networkRunner.JoinSessionLobby(SessionLobby.Custom, "default", null, BuildCustomAppSetting(selectedRegion));
                    //CreateSession(roomName, false, 1, customProps);
                }
            }
        }
        public async void RefreshRegionDropdown()
        {
            regionSelectorPanel.gameObject.SetActive(true);
            for (int i = 0; i < regionItemHandler.childCount; i++)
            {
                Destroy(regionItemHandler.GetChild(i).gameObject);
            }
            _tokenSource = new CancellationTokenSource();
            var regions = await NetworkRunner.GetAvailableRegions(cancellationToken: _tokenSource.Token);            
            for (int i = 0; i < regions.Count; i++)
            {
                var item = Instantiate(regionItem, regionItemHandler, false);
                item.transform.GetChild(0).GetComponent<Text>().text = regions[i].RegionCode;
                item.transform.GetChild(1).GetComponent<Text>().text = regions[i].RegionPing.ToString();
                if (regions[i].RegionPing <= 120)
                {
                    item.transform.GetChild(1).GetComponent<Text>().color = Color.green;
                    item.transform.SetAsFirstSibling();
                }
                if (regions[i].RegionPing > 120 && regions[i].RegionPing <= 150)
                {
                    item.transform.GetChild(1).GetComponent<Text>().color = Color.yellow;
                }
                if (regions[i].RegionPing > 150)
                {
                    item.transform.GetChild(1).GetComponent<Text>().color = Color.red;
                }
                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    selectedRegion = item.transform.GetChild(0).GetComponent<Text>().text;
                    selectedRegionPing = int.Parse(item.transform.GetChild(1).GetComponent<Text>().text);
                    regionSelectorPanel.gameObject.SetActive(false);
                    regionSelected = true;
                    StartCoroutine(Reconnection());
                }
                );
            }
        }
        public async void CreateSession(string roomName, bool isVisible, int maxPlayers, Dictionary<string, SessionProperty> sessionProperty)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning("Room name cannot be empty.");
                return;
            }
            if (networkRunner.IsRunning)
            {
                Destroy(networkRunner);
                networkRunner = null;
                //await networkRunner.Shutdown();
                networkRunner = Instantiate(networkRunnerPrefab);
                recorder = networkRunner.GetComponent<Recorder>();
            }
            var startGameArgs = new StartGameArgs
            {
                GameMode = Fusion.GameMode.Host,
                SessionName = roomName,
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>(),
                IsVisible = isVisible,
                SessionProperties = sessionProperty,
                PlayerCount = maxPlayers,
                CustomLobbyName = "default",
                CustomPhotonAppSettings = BuildCustomAppSetting(selectedRegion)
        };

            var result = await networkRunner.StartGame(startGameArgs);
            if (result.Ok)
            {
                Debug.Log($"Room created: {roomName}");
                OnJoinedRoom();
            }
            else
            {
                tryingToJoinCustom = false;

                Debug.LogError($"Failed to create room: {result.ShutdownReason}");
            }
        }
        public async void JoinSession(string roomName)
        {
            var startGameArgs = new StartGameArgs
            {
                GameMode = Fusion.GameMode.Client,
                SessionName = roomName,
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
            };

            var result = await networkRunner.StartGame(startGameArgs);
            if (result.Ok)
            {
                Debug.Log($"Joined room: {roomName}");
                OnJoinedRoom();
            }
            else
            {
                Debug.LogError($"Failed to join room: {result.ShutdownReason}");
            }
        }
        private FusionAppSettings BuildCustomAppSetting(string region, string customAppID = null, string appVersion = "1.0.0")
        {

            var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy(); ;

            appSettings.UseNameServer = true;
            appSettings.AppVersion = appVersion;

            if (string.IsNullOrEmpty(customAppID) == false)
            {
                appSettings.AppIdFusion = customAppID;
            }

            if (string.IsNullOrEmpty(region) == false)
            {
                appSettings.FixedRegion = region.ToLower();
            }

            // If the Region is set to China (CN),
            // the Name Server will be automatically changed to the right one
            // appSettings.Server = "ns.photonengine.cn";

            return appSettings;
        }
        float timeElapsed = 0;
        // Update is called once per frame
        void Update()
        {
            if (timeElapsed > 2)
            {
                if (networkRunner == null)
                {
                    StartCoroutine(Reconnection());
                }
                if (networkRunner != null && !networkRunner.IsCloudReady)
                {
                    StartCoroutine(Reconnection());
                }
                timeElapsed = 0;
            }
            else
            {
                timeElapsed += Time.deltaTime;
            }            
            if (networkRunner.IsInSession)
            {
                if (recorder != null && recorder.VoiceDetector.Detected && onVoiceDetected != null)
                {
                    onVoiceDetected();
                }
                else if (onVoiceDetected != null)
                {
                    onVoiceNotDetected();
                }
            }
            // Room managing:
            if (networkRunner.IsInSession && !isLoadingGameScene)
            {
                // Set the variable "loadNow" to true if the room is already full:
                if (/*totalPlayerCount >= networkRunner.SessionInfo.MaxPlayers && */((isInCustomGame && startCustomGameNow)/* || !isInCustomGame*/))
                {
                    loadNow = true;
                }
                // Go to the game scene if the variable "loadNow" is true:
                if (loadNow)
                {
                    if (networkRunner.IsServer)
                    {
                        networkRunner.SessionInfo.IsOpen = false;
                        networkRunner.LoadScene(gameSceneName);
                        loadNow = false;
                        isLoadingGameScene = true;
                    }
                }
            }
        }

        // Matchmaking:
        public void FindMatch()
        {
            //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            //h.Add("isInMatchmaking", true);
            //PhotonNetwork.JoinRandomRoom(h, 0);
        }
        public void CancelMatchmaking()
        {
            //PhotonNetwork.LeaveRoom();
            //// Clear bots list:
            //curBots = new Bot[0];
        }

        // Custom Game:
        public void JoinCustomGame(SessionInfo room)
        {
            tryingToJoinCustom = true;
            JoinSession(room.Name);
        }
        public void CreateCustomGame(int selectedMap, int maxPlayers, bool privateRoom)
        {
            if (networkRunner.LobbyInfo.IsValid)
            {
                var customProps = new Dictionary<string, SessionProperty>();

                customProps["started"] = (bool)false;
                customProps["map"] = (int)selectedMap;
                customProps["customAllowBots"] = (bool)false;
                customProps["isInMatchmaking"] = (bool)false;
                Debug.Log("UserName " + PlayerPrefs.GetString("USERNAME"));
                CreateSession(PlayerPrefs.GetString("USERNAME"), !privateRoom, maxPlayers, customProps);
            }
        }
        public void StartCustomGame()
        {
            Debug.Log("incustom = " + inCustom + "load now = " + loadNow);
            // Start creating bots (if bots are allowed) as this will fill out the empty players:
            if (inCustom && !loadNow)
            {
                // Create the bots if allowed:
                if ((bool)networkRunner.SessionInfo.Properties["customAllowBots"])
                {
                    // Clear the bots array first:
                    curBots = new Bot[0];
                    // Generate a number to be attached to the bot names:
                    bnp = Random.Range(0, 9999);
                    int numCreatedBots = 0;
                    //int max = PhotonNetwork.CurrentRoom.MaxPlayers - totalPlayerCount;
                    //while (numCreatedBots < max)
                    //{
                    //    CreateABot();
                    //    numCreatedBots++;
                    //}
                }

                startCustomGameNow = true;
            }
        }

        // Bot Creation:
        void StartCreatingBots()
        {
            // Generate a number to be attached to the bot names:
            bnp = Random.Range(0, 9999);
            Invoke("CreateABot", Random.Range(minBotCreationTime, maxBotCreationTime));
        }
        void CreateABot()
        {
            //if (PhotonNetwork.InRoom)
            //{
            //    // Add a new bot to the bots array:
            //    Bot[] b = new Bot[curBots.Length + 1];
            //    for (int i = 0; i < curBots.Length; i++)
            //    {
            //        b[i] = curBots[i];
            //    }
            //    b[b.Length - 1] = new Bot();

            //    // Setup the new bot (set the name and the character chosen):
            //    b[b.Length - 1].name = botPrefixes[Random.Range(0, botPrefixes.Length)] + bnp;
            //    b[b.Length - 1].characterUsing = Random.Range(0, characterSelector.characters.Length);
            //    // And choose a random hat, or none:
            //    b[b.Length - 1].hat = Random.Range(-1, ItemDatabase.instance.hats.Length);
            //    bnp += 1;   // make next bot name unique

            //    // Now replace the old bot array with the new one:
            //    curBots = b;

            //    // ...and upload the new bot array to the room properties:
            //    ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();

            //    string[] bn = new string[b.Length];
            //    Vector3[] bs = new Vector3[b.Length];
            //    int[] bc = new int[b.Length];
            //    int[] bHats = new int[b.Length];
            //    for (int i = 0; i < b.Length; i++)
            //    {
            //        bn[i] = b[i].name;
            //        bs[i] = b[i].scores;
            //        bc[i] = b[i].characterUsing;
            //        bHats[i] = b[i].hat;
            //    }
            //    bn[bn.Length - 1] = b[b.Length - 1].name;
            //    bs[bs.Length - 1] = b[b.Length - 1].scores;
            //    bc[bc.Length - 1] = b[b.Length - 1].characterUsing;
            //    bHats[bc.Length - 1] = b[b.Length - 1].hat;

            //    h.Add("botNames", bn);
            //    h.Add("botScores", bs);
            //    h.Add("botCharacters", bc);
            //    h.Add("botHats", bHats);
            //    PhotonNetwork.CurrentRoom.SetCustomProperties(h);

            //    if (!isInCustomGame)
            //    {
            //        // Continue adding another bot after a random delay (to give human players enough time to join, and also to simulate realism):
            //        Invoke("CreateABot", Random.Range(minBotCreationTime, maxBotCreationTime));
            //    }
            //}
        }

        //Bot[] GetBotList()
        //{
        //    Bot[] list = new Bot[0];

        //    // Download the bots list if we already have one:
        //    if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("botNames"))
        //    {
        //        string[] bn = (string[])PhotonNetwork.CurrentRoom.CustomProperties["botNames"];
        //        Vector3[] bs = (Vector3[])PhotonNetwork.CurrentRoom.CustomProperties["botScores"];
        //        int[] bc = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botCharacters"];

        //        list = new Bot[bn.Length];
        //        for (int i = 0; i < list.Length; i++)
        //        {
        //            list[i] = new Bot();
        //            list[i].name = bn[i];
        //            list[i].scores = bs[i];
        //            list[i].characterUsing = bc[i];
        //        }
        //    }
        //    return list;
        //}

        void UpdatePlayerCount()
        {
            // Get the "Real" player count:
            int players = networkRunner.SessionInfo.PlayerCount;

            // Set the total player count:
            totalPlayerCount = players;
        }
        //public override void OnJoinedLobby()
        //{
        //    if (PlayfabLogin.playFabFriends != null && PlayfabLogin.playFabFriends.Friends.Count > 0)
        //    {
        //        PhotonNetwork.FindFriends(PlayfabLogin.playFabFriends.Friends.Select(y => y.Username).ToArray());
        //    }
        //}
        //public List<FriendInfo> photonFriends = new List<FriendInfo>();
        public PlayfabLogin loginManager;
        //public override void OnFriendListUpdate(List<Photon.Realtime.FriendInfo> friendList)
        //{
        //    Debug.Log("Phtont Friends List Debug");
        //    photonFriends = friendList;
        //}

        //public override void OnPlayerEnteredRoom(Player player)
        //{

        //}
        //public override void OnPlayerLeftRoom(Player player)
        //{

        //}

        //public override void OnRoomListUpdate(List<RoomInfo> roomList)
        //{

        //}
        //public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        //{
        //    // A bot might have joined, so update the total player count:
        //    UpdatePlayerCount();
        //}
        //public override void OnCreateRoomFailed(short returnCode, string message){


        //}
        //public override void OnJoinRandomFailed(short returnCode, string message)
        //{

        //}
        //public override void OnJoinRoomFailed(short returnCode, string message)
        //{
        //    DataCarrier.message = message;
        //    if (!tryingToJoinCustom) PhotonNetwork.JoinRandomRoom();
        //}
        public void OnJoinedRoom()
        {
            if (!networkRunner.SessionInfo.Properties.ContainsKey("isDefault"))
            {
                inCustom = true;
            }
            SoundManager.instance.audioSource.volume = 0.1f;
            GameManager.gameMode = GameMode.Multiplayer;
            tryingToJoinCustom = false;

            // Know if the room we joined in is a custom game or not:
            //inCustom = !(bool)networkRunner.SessionInfo.Properties["isInMatchmaking"];
            // This is only used to check if we've loaded the game and ready. This sets to 0 after loading the game scene:
            //PhotonNetwork.LocalPlayer.SetScore(-1); // -1 = not ready, 0 = ready

            //// Setup scores (these are the actual player scores):
            //ExitGames.Client.Photon.Hashtable p = new ExitGames.Client.Photon.Hashtable();
            //p.Add("kills", 0);
            //p.Add("deaths", 0);
            //p.Add("otherScore", 0);
            //// Also set the chosen character:
            //p.Add("character", DataCarrier.chosenCharacter);
            //// ...and the cosmetics:
            //int[] cosmetics = new int[1];   // You can have as many items as you want, but in our case we only need 1 and that's for the hat
            //cosmetics[0] = DataCarrier.chosenHat;
            ///* // Sample usage:
            //cosmetics[1] = DataCarrier.chosenBackpack;
            //cosmetics[2] = DataCarrier.chosenShoes; */
            //p.Add("cosmetics", cosmetics);

            //// Apply:
            //PhotonNetwork.LocalPlayer.SetCustomProperties(p);

            // Let's update the total player count (for local reference):
            UpdatePlayerCount();

            // (MATCHMAKING ONLY) Start creating bots (if bots are allowed):
            //if (PhotonNetwork.IsMasterClient && createBots && !isInCustomGame)
            //{
            //    // Clear the bots array first:
            //    curBots = new Bot[0];
            //    // then start creating new ones:
            //    Invoke("StartCreatingBots", startCreatingBotsAfter);
            //}

        }
        //public override void OnLeftRoom(){

        //}

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new System.NotImplementedException();
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            //throw new System.NotImplementedException();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // When a player connects, update the player count:
            UpdatePlayerCount();
            // Events:
            onPlayerJoin(player);
            onJoinRoom(player);

            playersList.Add(player);
            if (networkRunner.IsServer)
            {
                runner.ProvideInput = true;
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (networkRunner.LocalPlayer == player)
            {
                GameManager.gameMode = GameMode.SinglePlayer;
                tryingToJoinCustom = false;
                isLoadingGameScene = false;
                onLeaveRoom();
            }
            else
            {
                UpdatePlayerCount();
                onPlayerLeave(player);
                if (SceneManager.GetActiveScene().name == "Game")
                {
                    GameManager.instance.ui.DisplayMessage(instance.playerData[playersList.IndexOf(player)].nickName + " left the match", UIManager.MessageType.LeftTheGame);
                }
            }
            playerData.Remove(playersList.IndexOf(player));
            playersList.Remove(player);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            GameManager.gameMode = GameMode.SinglePlayer;
            StartCoroutine(Reconnection());
            //throw new System.NotImplementedException();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            GameManager.gameMode = GameMode.SinglePlayer;
            isLoadingGameScene = false;

            // Events:
            onDisconnect?.Invoke();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            //throw new System.NotImplementedException();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            throw new System.NotImplementedException();
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            throw new System.NotImplementedException();
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data)
        {
            throw new System.NotImplementedException();
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            throw new System.NotImplementedException();
        }
        public ControlsManager controlsManager;
        [HideInInspector] public List<PlayerRef> playersList = new List<PlayerRef>();

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {

        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            //throw new System.NotImplementedException();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            loginManager.GetPlayers();
            //onConnectedToClient();
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            // List only custom rooms:
            rooms = new List<SessionInfo>();
            List<SessionInfo> r = sessionList;
            Debug.Log("sessioList COunt = " + sessionList.Count);
            for (int i = 0; i < r.Count; i++)
            {
                if (r[i].Properties.ContainsKey("isInMatchmaking"))
                {
                    Debug.Log("Propertiessss = " + r[i].Properties["isInMatchmaking"].ToString());
                    if ((bool)r[i].Properties["isInMatchmaking"] == false)
                    {

                        rooms.Add(r[i]);
                    }
                }
                else
                {
                    Debug.Log("It does not coantain");
                }
            }

            // Do events:
            if (onRoomListChange != null)
            {
                onRoomListChange(r.Count);
            }
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            throw new System.NotImplementedException();
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            throw new System.NotImplementedException();
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            throw new System.NotImplementedException();
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            throw new System.NotImplementedException();
        }
    }
}
public struct PlayerInput : INetworkInput
{
    public float xInput;
    public float yInput;
}
public class PlayerData
{
    public string nickName;
    public int characterData;
    public int choosenHat;
    public int choosenGlasses;
    public int choosenNecklace;
}