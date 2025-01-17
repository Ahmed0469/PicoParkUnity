using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

namespace Visyde
{
    /// <summary>
    /// Lobby Browser UI
    /// - The script for the sample lobby browser interface
    /// </summary>

    public class LobbyBrowserUI : MonoBehaviour
    {
        [Header("Browser:")]
        public float browserRefreshRate = 3f;   // how many times should the browser refresh itself
        public Transform roomItemHandler;		// this is where the room item prefabs will be spawned
        public RoomItem roomItemPrefab;         // the room item prefab (represents a game session in the lobby list)
        public Text listStatusText;             // displays the current status of the lobby browser (eg. "No games available", "Fetching game list...")

        [Header("Create Screen:")]
        public Text mapNameText;
        public SelectorUI mapSelector;
        public SelectorUI playerNumberSelector;
        public Toggle isPrivateRoom;

        [Header("Joined Screen:")]
        public GameObject playerItemPrefab;
        public Transform playerItemHandler;
        public ChatSystem chatSystem;
        public Text chosenMapText;
        public Text chosenPlayerNumberText;
        public Text enableBotsText;
        public Text currentNumberOfPlayersInRoomText;
        public Button startBTN;

        // Internals:
        string randomRoomName;

        void OnDisable(){
            Connector.instance.onRoomListChange -= onRoomListUpdate;
            Connector.instance.onCreateRoomFailed -= onCreateRoomFailed;
            Connector.onJoinRoom -= OnJoinedRoom;
            Connector.instance.onLeaveRoom -= OnLeftRoom;
            Connector.instance.onPlayerJoin -= OnPlayerJoined;
            Connector.onPlayerLeave -= OnPlayerLeft;
        }
        private void Start()
        {
            Connector.instance.onRoomListChange += onRoomListUpdate;
            Connector.instance.onCreateRoomFailed += onCreateRoomFailed;
            Connector.onJoinRoom += OnJoinedRoom;
            Connector.instance.onLeaveRoom += OnLeftRoom;
            //Debug.Log("lobbyyyyy");
            Connector.instance.onPlayerJoin += OnPlayerJoined;
            Connector.onPlayerLeave += OnPlayerLeft;
        }
        // Update is called once per frame
        void Update()
        {            
            // ***CREATE***
            // Display selected map name:
            mapNameText.text = Connector.instance.maps[mapSelector.curSelected];
        }

        public void RefreshBrowser(){
            // Clear UI room list:
            foreach (Transform t in roomItemHandler)
            {
                Destroy(t.gameObject);
            }

            // If there are available rooms, populate the UI list:
            if (Connector.instance.rooms.Count > 0)
            {
                listStatusText.text = "";
                for (int i = 0; i < Connector.instance.rooms.Count; i++)
                {
                    if (!Connector.instance.networkRunner.IsInSession || (bool)Connector.instance.rooms[i].Properties["isInMatchmaking"] == false)
                    {
                        RoomItem r = Instantiate(roomItemPrefab, roomItemHandler);
                        r.Set(Connector.instance.rooms[i], this);
                    }
                }
            }
            // else, just show an error text:
            else
            {
                listStatusText.text = "No games are currently available";
            }
        }
        public void RefreshPlayerList()
        {

            // Clear list first:
            //foreach (Transform t in playerItemHandler)
            //{
            //    Destroy(t.gameObject);
            //}
            //for (int i = 0; i < players.Length; i++)
            //{
            //    //CustomGamePlayerItem cgp = Instantiate(playerItemPrefab, playerItemHandler, false);
            //    CustomGamePlayerItem cgp = PhotonNetwork.Instantiate(playerItemPrefab.name, playerItemHandler.position, Quaternion.identity).GetComponent<CustomGamePlayerItem>();
            //    cgp.transform.SetParent(playerItemHandler);
            //    cgp.Set(players[i]);
            //}

            //cgp.transform.SetParent(playerItemHandler);
            //cgp.Set(PhotonNetwork.LocalPlayer);

            //CustomGamePlayerItem[] playerItems = FindObjectsOfType<CustomGamePlayerItem>();
            //Debug.Log(playerItems.Length + " Length Test");
            //for (int i = 0; i < playerItems.Length; i++)
            //{
            //    playerItems[i].transform.SetParent(playerItemHandler);
            //    playerItems[i].Set(players[i]);
            //}
            // Player number in room text:
            currentNumberOfPlayersInRoomText.text = "Players (" + Connector.instance.networkRunner.SessionInfo.PlayerCount + "/" + Connector.instance.networkRunner.SessionInfo.MaxPlayers + ")";

            // Enable/disable start button:
            bool allowBots = Connector.instance.networkRunner.SessionInfo.Properties.ContainsKey("customAllowBots") && (bool)Connector.instance.networkRunner.SessionInfo.Properties["customAllowBots"];
        }
        public void Join(SessionInfo room){
            Connector.instance.JoinCustomGame(room);
        }
        public void Leave()
        {
            if (Connector.instance.networkRunner.IsInSession /*&& ChatSystem.chatClient.CanChat*/)
            {
                Connector.instance.Disconnect();
            }
        }
        public void Create(){
            Connector.instance.CreateCustomGame(mapSelector.curSelected, playerNumberSelector.items[playerNumberSelector.curSelected].value, isPrivateRoom.isOn);
        }
        public void StartGame(){
            GameManager.levelInt = 0;
            Connector.instance.StartCustomGame();
        }
        public void LevelBtn(int levelId)
        {
            GameManager.levelInt = levelId;
            Connector.instance.StartCustomGame();
        }
        // Subscribed to Connector's "onRoomListChange" event:
        void onRoomListUpdate(int roomCount)
        {
            RefreshBrowser();
        }
        // Subscribed to Connector's "OnPlayerJoin" event:
        void OnPlayerJoined(PlayerRef player)
        {
            // When a player connects, update the player list:
            RefreshPlayerList();

            // Notify other players through chat:
            //chatSystem.SendSystemChatMessage(player.NickName + " joined the game.", false);
        }
        // Subscribed to Connector's "onPlayerLeave" event:
        void OnPlayerLeft(PlayerRef player)
        {
            // When a player disconnects, update the player list:
            RefreshPlayerList();

            // Notify other players through chat:
            //chatSystem.SendSystemChatMessage(player.NickName + " left the game.", true);
        }
        // Subscribed to Connector's "onCreateRoomFailed" event:
        void onCreateRoomFailed(){
            // Display error:
            DataCarrier.message = "Custom game creation failed.";
        }
        // Subscribed to Connector's "OnJoinRoom" event:
        void OnJoinedRoom(PlayerRef player)
        {
            Debug.Log("Outside Spawning Log");
            if (Connector.instance.networkRunner.IsServer)
            {
                Debug.Log("Spawning Log");
                var networkLobbyItem = Connector.instance.networkRunner.SpawnAsync(playerItemPrefab, playerItemHandler.position, Quaternion.identity, player).Object;
                var lobbyItem = networkLobbyItem.GetComponent<CustomGamePlayerItem>();
                Connector.instance.lobbyItems.Add(lobbyItem);
                /*.GetComponent<CustomGamePlayerItem>().owner = player*/
            }

            // Update the player list when we join a room:
            RefreshPlayerList();

            if (!Connector.instance.networkRunner.SessionInfo.Properties.ContainsKey("isDefault"))
            {
                chosenMapText.text = Connector.instance.maps[(int)Connector.instance.networkRunner.SessionInfo.Properties["map"]];
                chosenPlayerNumberText.text = Connector.instance.networkRunner.SessionInfo.MaxPlayers.ToString();
                if (Connector.instance.networkRunner.SessionInfo.Properties.ContainsKey("customAllowBots")) enableBotsText.text = (bool)Connector.instance.networkRunner.SessionInfo.Properties["customAllowBots"] ? "Yes" : "No";
            }
        }
        // Subscribed to Connector's "onLeaveRoom" event:
        void OnLeftRoom(){
            //if (PhotonNetwork.InRoom){
            //    PhotonNetwork.LeaveRoom();
            //}
        }
    }
}