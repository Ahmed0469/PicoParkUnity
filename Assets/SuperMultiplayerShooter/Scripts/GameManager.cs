using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Threading.Tasks;
using Fusion.Sockets;
using System.Linq;
using System;
using UnityEngine.UI;
//using Photon.Pun;
//using Photon.Realtime;
//using Photon.Pun.UtilityScripts;

namespace Visyde
{
    /// <summary>
    /// Game Manager
    /// - Simply the game manager. The one that controls the game itself. Provides game settings and serves as the central component by
    /// connecting other components to communicate with each other.
    /// </summary>

    public class GameManager : MonoBehaviour,INetworkRunnerCallbacks/*MonoBehaviourPunCallbacks, IInRoomCallbacks, IConnectionCallbacks*/
    {
        public static GameManager instance;

        public GameObject playerPrefab;                     // Name of player prefab. The prefab must be in a "Resources" folder.
        public List<GameObject> playerPrefabs = new();
        public PlayerController singleModePlayerPrefab;

        [Space]
        public List<Maps> mapsList = new List<Maps>();
        [System.Serializable]
        public class Maps
        {
            public GameMap[] maps;
        }
        public GameMap[] maps;
        [HideInInspector] public int chosenMap = -1;

        [Space]
        [Header("Game Settings:")]
        [HideInInspector]public bool useMobileControls;              	// if set to true, joysticks and on-screen buttons will be enabled
        public int respawnTime = 5;             		// delay before respawning after death
        public float invulnerabilityDuration = 3;		// how long players stay invulnerable after spawn
        public int preparationTime = 3;                 // the starting countdown before the game starts
        public int gameLength = 120;                    // time in seconds
        public bool showEnemyHealth = false;
        public bool damagePopups = true;
        [System.Serializable]
        public class KillData
        {
            public bool notify = true;
            public string message;
            public int bonusScore;
        }
        public KillData[] multiKillMessages;
        public float multikillDuration = 3;             // multikill reset delay
        public bool allowHurtingSelf;                   // allow grenades and projectiles to hurt their owner
        public bool deadZone;                           // kill players when below the dead zone line
        public float deadZoneOffset;                    // Y position of the dead zone line

        [Space]
        [Header("Others:")]
        public bool doCamShakesOnDamage;            	// allow cam shakes when taking damage
        public float camShakeAmount = 0.3f;
        public float camShakeDuration = 0.1f;

        [Space]
        [Header("References:")]
        public ItemSpawner itemSpawner;
        public ControlsManager controlsManager;
        public UIManager ui;
        public CameraController gameCam;                // The main camera in the game scene (used for the controls)
        public ObjectPooler pooler;

        // if the starting countdown has already begun:
        public bool countdownStarted
        {
            get { return startingCountdownStarted; }
        }
        // the progress of the starting countdown:
        public float countdown
        {
            get { return (float)(gameMode == GameMode.Multiplayer? gameStartsIn - (Connector.instance.networkRunner.RemoteRenderTime) : gameStartsIn -= Time.deltaTime); }
        }
        // the time elapsed after the starting countdown:
        public float timeElapsed
        {
            get { return (float)elapsedTime; }
        }
        // the remaining time before the game ends:
        public int remainingGameTime
        {
            get { return (int)remainingTime; }
        }
        public GameMap getActiveMap
        {
            get
            {
                foreach (GameMap g in maps)
                {
                    if (g.gameObject.activeInHierarchy) return g;
                }

                return null;
            }
        }

        [System.NonSerialized] public bool gameStarted = false;                                                 // if the game has started already
        [System.NonSerialized] public string[] playerRankings = new string[0];       				            // used at the end of the game
        public PlayerInstance[] bots;                                                   // Player instances of bots
        public PlayerInstance[] players;                                               // Player instances of human players (ours is first)
        [System.NonSerialized] public bool isGameOver;                                                          // is the game over?
        [System.NonSerialized] public PlayerController ourPlayer;                                				// our player's player (the player object itself)        
        [System.NonSerialized] public List<PlayerController> ourPlayers = new List<PlayerController>();                                				// our player's player (the player object itself)
        public List<PlayerController> playerControllers = new List<PlayerController>();	// list of all player controllers currently in the scene
        [System.NonSerialized] public bool dead;
        [System.NonSerialized] public float curRespawnTime;
        public static bool isDraw;                                                          				    // is game draw?
        bool hasBots;                                                                       				    // does the game have bots?

        // Local copy of bot stats (so we don't periodically have to download them when we need them):
        string[] bNames = new string[0];		// Bot names
        Vector3[] bScores = new Vector3[0];		// Bot scores (x = kills, y = deaths, z = other scores)
        int[] bChars = new int[0];				// Bot's chosen character's index
        int[] bHats = new int[0];               // Bot hats (cosmetics)

        // Used for time syncronization:
        [System.NonSerialized] public double startTime, elapsedTime, remainingTime, gameStartsIn;
        bool startingCountdownStarted, doneGameStart;

        // For respawning:
        double deathTime;

        // Others:
        public PlayerRef[] punPlayersAll;
        Playerr[] playerAll = new Playerr[2];
        public GameObject LevelWinScreen;
        public Button nextLevelBtn;
        public Button jumpBtn;
        public Text pingText;

        public static GameMode gameMode;
        [HideInInspector] public bool userVerticalAxis = false;

        public class Playerr
        {
            public string nickName;
            public int actorNumber;
            public bool isLocal;

            public Playerr(string nickName, int actorNumber, bool isLocal)
            {
                this.nickName = nickName;
                this.actorNumber = actorNumber;
                this.isLocal = isLocal;
            }            
        }


        void Awake()
        {
            instance = this;
            // Determine the type of controls:
#if PLATFORM_ANDROID
            controlsManager.mobileControls = true;
            useMobileControls = true;
#endif
#if UNITY_EDITOR
            controlsManager.mobileControls = false;
            useMobileControls = false;
#endif

            players = new PlayerInstance[0];

            if (gameMode == GameMode.Multiplayer)
            {                
                // Prepare player instance arrays:
                bots = new PlayerInstance[0];

                // Cache current player list:
                if (gameMode == GameMode.Multiplayer)
                {
                    if (Connector.instance.networkRunner.IsServer)
                    {
                        punPlayersAll = Connector.instance.playersList.ToArray();
                    }
                }
                else
                {
                    playerAll[0] = new Playerr("SinglePlayer", 0, true);
                    playerAll[1] = new Playerr("SinglePlayer", 1, false);
                }
            }

            // Generate human player instances:
            players = GeneratePlayerInstances(true);

            // Don't allow the device to sleep while in game:
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        public static int levelInt = 0;
        // Use this for initialization
        async void Start()
        {            
            Connector.instance.networkRunner.AddCallbacks(this);
            SoundManager.instance.StopMusic();
            SoundManager.instance.audioSource.volume = 1;
            // Setups:
            isDraw = false;
            //gameCam.gm = this;
            
            //controlsManager.mobileControls = useMobileControls;
            SceneManager.activeSceneChanged += (scene1,scene2)=> 
            {
                if (scene1.name == "Game" && scene2.name == "MainMenu")
                {
                    Connector.instance.lobbyItems = new();
                    Connector.instance.playerData = new();
                    SceneManager.activeSceneChanged -= (scene1, scene2) =>
                    {
                        if (scene1.name == "Game" && scene2.name == "MainMenu")
                        {
                            Connector.instance.lobbyItems = new();
                            Connector.instance.playerData = new();
                        }
                    };
                }
            };
            // Get the chosen map then enable it:
            if (gameMode == GameMode.Multiplayer)
            {
                //switch (Connector.instance.networkRunner.SessionInfo.PlayerCount)
                //{
                //    case 1:
                //        maps = mapsList[0].maps;
                //        break;
                //    case 2:
                //        maps = mapsList[0].maps;
                //        break;
                //    case 3:
                //        maps = mapsList[1].maps;
                //        break;
                //    case 4:
                //        maps = mapsList[1].maps;
                //        break;
                //    case 5:
                //        maps = mapsList[2].maps;
                //        break;
                //    case 6:
                //        maps = mapsList[2].maps;
                //        break;
                //    case 7:
                //        maps = mapsList[3].maps;
                //        break;
                //    case 8:
                //        maps = mapsList[3].maps;
                //        break;
                //}
                maps = mapsList[levelInt].maps;
                chosenMap = (int)Connector.instance.networkRunner.SessionInfo.Properties["map"];                
                if (Connector.instance.networkRunner.IsServer)
                {
                   await Connector.instance.networkRunner.SpawnAsync(maps[chosenMap].gameObject, maps[chosenMap].transform.position, Quaternion.identity, Connector.instance.networkRunner.LocalPlayer);
                }
            }
            else
            {
                chosenMap = PlayerPrefs.GetInt("SingleLevels", 0);
                Instantiate(maps[chosenMap], maps[chosenMap].transform.position, Quaternion.identity);
            }
            //for (int i = 0; i < maps.Length; i++)
            //{
            //    maps[i].gameObject.SetActive(chosenMap == i);
            //}

            // After loading the scene, we (the local player) are now ready for the game:
            Ready();

            // Start checking if all players are ready:
            InvokeRepeating("CheckIfAllPlayersReady", 1, 0.5f);
        }

        private void OnDestroy()
        {
            if (Connector.instance.networkRunner != null)
            {
                Connector.instance.networkRunner.RemoveCallbacks(this);
            }
        }
        void CheckIfAllPlayersReady()
        {            
            if (!isGameOver)
            {
                // Check if players are ready:
                if (!startingCountdownStarted)
                {
                    bool allPlayersReady = true;

                    if (gameMode == GameMode.Multiplayer)
                    {
                        for (int i = 0; i < punPlayersAll.Length; i++)
                        {
                            // If a player hasn't yet finished loading, don't start:
                            //if (punPlayersAll[i].GetScore() == -1)
                            //{
                            //    allPlayersReady = false;
                            //}
                        }
                    }
                    // Start the preparation countdown when all players are done loading:
                    if (gameMode == GameMode.Multiplayer)
                    {
                        if (allPlayersReady && Connector.instance.networkRunner.IsServer)
                        {
                            StartGamePrepare();
                        }
                    }
                    else
                    {
                        StartGamePrepare();
                    }
                }
            }
        }
        [HideInInspector] public int clientPing;        
        // Update is called once per frame
        void Update()
        {
            if (gameMode == GameMode.Multiplayer)
            {                
                pingText.text = $"Connected! {Connector.instance.networkRunner.LobbyInfo.Region} | Ping: " + clientPing;
            }
            if (gameStartsIn != 0)
            {
                startingCountdownStarted = true;
            }
            if (!isGameOver)
            {
                // Start the game when preparation countdown is finished:
                if (startingCountdownStarted)
                {
                    if (elapsedTime >= (gameStartsIn - startTime) && !gameStarted && !doneGameStart)
                    {
                        // GAME HAS STARTED!
                        if (gameMode == GameMode.Multiplayer)
                        {
                            if (Connector.instance.networkRunner.IsServer)
                            {
                                doneGameStart = true;
                                gameStarted = true;
                                StartGameTimer();
                            }
                        }
                        else
                        {
                            doneGameStart = true;
                            gameStarted = true;
                            StartGameTimer();
                        }                    

                        CancelInvoke("CheckIfAllPlayersReady");
                    }
                }

                // Respawning:
                if (dead)
                {
                    if (deathTime == 0)
                    {
                        deathTime = gameMode == GameMode.Multiplayer? Connector.instance.networkRunner.RemoteRenderTime : Time.deltaTime + respawnTime;
                    }
                    curRespawnTime = (float)(deathTime - (gameMode == GameMode.Multiplayer ? Connector.instance.networkRunner.RemoteRenderTime : Time.deltaTime));
                    if (curRespawnTime <= 0)
                    {
                        dead = false;
                        deathTime = 0;
                        //Spawn();
                    }
                }

                // Calculating the elapsed and remaining time:
                CheckTime();

                // Finish game when elapsed time is greater than or equal to game length:
                if (elapsedTime + 1 >= gameLength && gameStarted && !isGameOver)
                {
                    // Post the player rankings:
                    if (Connector.instance.networkRunner.IsServer)
                    {
                        // Get player list by order based on scores and also set "draw" to true (the player sorter will set this to false if not draw):
                        //isDraw = true;

                        // List of player names for the rankings:
                        PlayerInstance[] ps = SortPlayersByScore();
                        string[] p = new string[ps.Length];
                        for (int i = 0; i < ps.Length; i++)
                        {
                            p[i] = ps[i].playerName;
                        }

                        isDraw = ps.Length > 1 && isDraw;

                        //// Mark as game over:
                        //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                        //h.Add("rankings", p);
                        //h.Add("draw", isDraw);
                        //PhotonNetwork.CurrentRoom.SetCustomProperties(h);

                        // Hide room from lobby:
                        Connector.instance.networkRunner.SessionInfo.IsVisible = false;
                    }
                    isGameOver = true;
                }

                // Check if game is over:
                if (playerRankings.Length > 0)
                {
                    //isGameOver = true;
                }
            }
        }
        void CheckTime()
        {
            elapsedTime = ((gameMode == GameMode.Multiplayer ? Connector.instance.networkRunner.RemoteRenderTime : Time.time) - startTime);
            remainingTime = gameLength - (elapsedTime % gameLength);
        }

        // Called when we enter the game world:
        void Ready()
        {
            // Set our score to 0 on start (this is not the player's actual score, this is only used to determine if we're ready or not, 0 = ready, -1 = not):
            //PhotonNetwork.LocalPlayer.SetScore(0);

            Spawn();            
        }

        /// <summary>
        /// Spawns the player.
        /// </summary>
        public async void Spawn()
        {
            // There are 2 values in the player's instatiation data. The first one is reserved and only used if the player is a bot, while the 
            // second is for the cosmetics (in this case we only have 1 which is for the chosen hat, but you can add as many as your game needs):
            if (gameMode == GameMode.Multiplayer)
            {
                if (Connector.instance.networkRunner.IsServer)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        int actorNumber = punPlayersAll.ToList().IndexOf(punPlayersAll[i]);
                        Transform spawnPoint = maps[chosenMap].playerSpawnPoints[actorNumber/* - 1*/];
                        var player = await Connector.instance.networkRunner.SpawnAsync
                            (
                            playerPrefabs[players[i].character], spawnPoint.position, Quaternion.identity, punPlayersAll[i]
                            );
                        if (players[i].cosmeticItems.hat != -1)
                        {
                            await Connector.instance.networkRunner.SpawnAsync
                            (
                             ItemDatabase.instance.hats[players[i].cosmeticItems.hat].prefab,
                             player.GetComponent<PlayerController>().character.hatPoint.position, Quaternion.identity,
                             punPlayersAll[i]
                            );
                        }
                        if (players[i].cosmeticItems.glasses != -1)
                        {
                            await Connector.instance.networkRunner.SpawnAsync
                            (
                             ItemDatabase.instance.glasses[players[i].cosmeticItems.glasses].prefab,
                             player.GetComponent<PlayerController>().character.glassesPoint.position, Quaternion.identity,
                             punPlayersAll[i]
                            );
                        }
                        if (players[i].cosmeticItems.necklace != -1)
                        {
                            await Connector.instance.networkRunner.SpawnAsync
                            (
                             ItemDatabase.instance.necklaces[players[i].cosmeticItems.necklace].prefab,
                             player.GetComponent<PlayerController>().character.necklacePoint.position, Quaternion.identity,
                             punPlayersAll[i]
                            );
                        }
                        //hat.transform.SetParent(player.GetComponent<PlayerController>().character.hatPoint);
                    }
                    //for (int i = 0; i < punPlayersAll.Length; i++)
                    //{
                    //    int actorNumber = punPlayersAll.ToList().IndexOf(punPlayersAll[i]);
                    //    Transform spawnPoint = maps[chosenMap].playerSpawnPoints[actorNumber/* - 1*/];
                    //    await Connector.instance.networkRunner.SpawnAsync
                    //        (
                    //        playerPrefab, spawnPoint.position, Quaternion.identity,punPlayersAll[i]
                    //        );
                    //}
                }
            }
            else
            {

                for (int i = 0; i < 2; i++)
                {
                    Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
                    var player = Instantiate(singleModePlayerPrefab, spawnPoint.transform.position, Quaternion.identity);
                    player.actorNumber = i;
                    ourPlayers.Add(player);
                }
            }
        }

        /// <summary>
        /// Spawns a bot (only works on master client).
        /// </summary>
        public void SpawnBot(int bot)
        {
            //if (!PhotonNetwork.IsMasterClient) return;

            //Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
            //// Instantiate the bot. Bots are assigned with random hats (second value of the instantiation data):
            //PlayerController botP = PhotonNetwork.InstantiateSceneObject(playerPrefab, spawnPoint.position, Quaternion.identity, 0, new object[] { bot }).GetComponent<PlayerController>();
        }

        public void SomeoneDied(int dying, int killer)
        {
            // Add scores (master client only)
            if (Connector.instance.networkRunner.IsServer)
            {
                // Kill score to killer:
                if (killer != dying) AddScore(GetPlayerInstance(killer), true, false, 0);

                // ... and death to dying, and score deduction if suicide:
                AddScore(GetPlayerInstance(dying), false, true, killer == dying ? -1 : 0);
            }

            // Display kill feed.
            ui.SomeoneKilledSomeone(GetPlayerInstance(dying), GetPlayerInstance(killer));
        }

        /// <summary>
        /// Returns the PlayerController of the player with the given name.
        /// </summary>
        public PlayerController GetPlayerControllerOfPlayer(string name)
        {
            for (int i = 0; i < playerControllers.Count; i++)
            {
                // Check if current item matches a player:
                if (string.CompareOrdinal(playerControllers[i].playerInstance.playerName, name) == 0)
                {
                    return playerControllers[i];
                }
            }

            return null;
        }
        public PlayerController GetPlayerControllerOfPlayer(PlayerInstance player)
        {
            for (int i = 0; i < playerControllers.Count; i++)
            {
                // Check if current item matches a player:
                if (playerControllers[i].playerInstance == player)
                {
                    return playerControllers[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Checks how many players are still in the game. If there's only 1 left, the game will end.
        /// </summary>
        public void CheckPlayersLeft()
        {
            if (GetPlayerList().Length <= 1 && Connector.instance.networkRunner.SessionInfo.MaxPlayers > 1)
            {
                print("GAME OVER!");
                //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                double skip = 0;
                startTime = skip;
                //h["gameStartTime"] = skip;
                //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
            }
        }

        // Player leaderboard sorting:
        IComparer SortPlayers()
        {
            return (IComparer)new PlayerSorter();
        }
        public PlayerInstance[] SortPlayersByScore()
        {
            // Get the full player list:
            PlayerInstance[] allPlayers = GetPlayerList();

            // ...then sort them out based on scores:
            System.Array.Sort(allPlayers, SortPlayers());
            return allPlayers;
        }

        public PlayerInstance GetPlayerInstance(int playerID)
        {
            // Look in human player list:
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].playerID == playerID)
                {
                    return players[i];
                }
            }
            // Look in bots:
            if (hasBots)
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    if (bots[i].playerID == playerID)
                    {
                        return bots[i];
                    }
                }
            }

            return null;
        }
        public PlayerInstance GetPlayerInstance(string playerName)
        {
            // Look in human player list:
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].playerName == playerName)
                {
                    return players[i];
                }
            }
            // Look in bots:
            if (hasBots)
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    if (bots[i].playerName == playerName)
                    {
                        return bots[i];
                    }
                }
            }

            return null;
        }

        // Get player list (humans + bots):
        public PlayerInstance[] GetPlayerList()
        {
            // Get the (human) player list:
            PlayerInstance[] tempP = players;

            // If we have bots, include them to the player list:
            if (hasBots)
            {
                // Merge the human list and bot list into one array:
                PlayerInstance[] p = new PlayerInstance[tempP.Length + bots.Length];
                tempP.CopyTo(p, 0);
                bots.CopyTo(p, tempP.Length);

                // ...then replace the human player list with the full player list array:
                tempP = p;
            }
            return tempP;
        }

        // Generates a PlayerInstance array for bots: 
        PlayerInstance[] GenerateBotPlayerInstances()
        {
            PlayerInstance[] p = bots;

            if (hasBots)
            {
                // If it's the first time generating, player instances should be created first: 
                if (bots.Length == 0)
                {
                    p = new PlayerInstance[bNames.Length];
                    for (int i = 0; i < p.Length; i++)
                    {
                        // Create a cosmetics instance:
                        Cosmetics cosmetics = new Cosmetics(bHats[i]);

                        // Create this bot's player instance (parameters: player ID, bot's name, not ours, is bot, chosen character, no cosmetic item, kills, deaths, otherScore):
                        //p[i] = new PlayerInstance(i + PhotonNetwork.CurrentRoom.MaxPlayers, bNames[i], false, true, bChars[i], cosmetics, Mathf.RoundToInt(bScores[i].x), Mathf.RoundToInt(bScores[i].y), Mathf.RoundToInt(bScores[i].z), null);
                    }
                }
                // ...otherwise, we can just set the stats directly:
                else
                {
                    for (int i = 0; i < p.Length; i++)
                    {
                        p[i].SetStats(Mathf.RoundToInt(bScores[i].x), Mathf.RoundToInt(bScores[i].y), Mathf.RoundToInt(bScores[i].z), false);
                    }
                }
            }
            return p;
        }
        public void SetBotPlayerInstance(string botName, int kills, int deaths, int otherScore)
        {

        }
        // Generates a PlayerInstance array for human players: 
        PlayerInstance[] GeneratePlayerInstances(bool fresh)
        {
            PlayerInstance[] p = players;
            // If it's the first time generating, player instances should be created first:
            if (players.Length == 0 || fresh)
            {
                if (gameMode == GameMode.Multiplayer)
                {
                    if (Connector.instance.networkRunner.IsServer)
                    {
                        p = new PlayerInstance[punPlayersAll.Length];
                    }
                }
                else
                {
                    p = new PlayerInstance[2];
                }

                for (int i = 0; i < p.Length; i++)
                {
                    // Create a cosmetics instance:
                    if (gameMode == GameMode.Multiplayer)
                    {                      
                        // Then create the player instance:
                        if (Connector.instance.networkRunner.IsServer)
                        {
                            Debug.Log("actor number = " + punPlayersAll.ToList().IndexOf(punPlayersAll[i]));
                            int actorNumber = punPlayersAll.ToList().IndexOf(punPlayersAll[i]);
                            Debug.Log(Connector.instance.playerData.Count + "NickName Count");
                            string nickName = Connector.instance.playerData[actorNumber].nickName;
                            int character = Connector.instance.playerData[actorNumber].characterData;
                            Cosmetics cosmetics = new Cosmetics
                                (
                                hat: Connector.instance.playerData[actorNumber].choosenHat,
                                glasses: Connector.instance.playerData[actorNumber].choosenGlasses,
                                necklace: Connector.instance.playerData[actorNumber].choosenNecklace
                                );

                            p[i] = new PlayerInstance(
                                actorNumber, nickName, 
                                Connector.instance.networkRunner.LocalPlayer == punPlayersAll[i],
                                false,
                                character,
                                cosmetics,
                                punPlayersAll[i]
                                );
                        }                        
                    }
                    else
                    {
                        // Then create the player instance:
                        bool isMine = i == 0;
                        p[i] = new PlayerInstance(i, $"SinglePlayer{i}", isMine, false,
                            i,
                            new Cosmetics
                            (
                                hat: DataCarrier.chosenHat,
                                glasses: DataCarrier.chosenGlasses,
                                necklace: DataCarrier.chosenNecklace
                                ),
                            playerAll[i]);
                    }
                }
            }
            // ...otherwise, we can just set the stats directly:
            else
            {
                for (int i = 0; i < p.Length; i++)
                {
                    if (gameMode == GameMode.Multiplayer)
                    {
                        //if (i < punPlayersAll.Length - 1)
                        //{
                        //    p[i].SetStats((int)punPlayersAll[i].CustomProperties["kills"], (int)punPlayersAll[i].CustomProperties["deaths"], (int)punPlayersAll[i].CustomProperties["otherScore"], true);
                        //}
                    }
                }
            }
            return p;
        }

        /// <summary>
        /// Set player instance stats. This is only for human players.
        /// </summary>
        public void SetPlayerInstance(PlayerRef forPlayer)
        {
            //PlayerInstance p = GetPlayerInstance(forPlayer.NickName);
            //p.SetStats((int)forPlayer.CustomProperties["kills"], (int)forPlayer.CustomProperties["deaths"], (int)forPlayer.CustomProperties["otherScore"], false);
        }

        /// <summary>
        /// Add score to a player.
        /// </summary>
        public void AddScore(PlayerInstance player, bool kill, bool death, int others)
        {
            player.AddStats(kill ? 1 : 0, death ? 1 : 0, others, true);  // the PlayerInstance will also automatically handle the uploading
        }

        // Upload an updated bot score list to the room properties:
        public void UpdateBotStats()
        {
            //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();

            // Get each bot's scores and store them as a Vector3:
            bScores = new Vector3[bots.Length];
            for (int i = 0; i < bots.Length; i++)
            {
                bScores[i] = new Vector3((int)bots[i].kills, (int)bots[i].deaths, (int)bots[i].otherScore);
            }

            //h.Add("botScores", bScores);
            //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
        }

        // Others:        

        // Calling this will make us disconnect from the current game/room:
        public void QuitMatch()
        {
            Connector.instance.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }
        public void RestartGame()
        {
            if (gameMode == GameMode.Multiplayer)
            {
                Connector.instance.networkRunner.LoadScene("Game");
            }
            else
            {
                SceneManager.LoadScene("SinglePlayerGameScene");
            }
        }
        public void NextLevel()
        {
            if (gameMode == GameMode.Multiplayer)
            {
                levelInt++;
                if (levelInt == 20)
                {
                    levelInt = 0;
                }
                Connector.instance.networkRunner.LoadScene("Game");
            }
            else
            {
                int levelId = PlayerPrefs.GetInt("SingleLevels", 0);
                levelId++;
                if (levelId == 10)
                {
                    levelId = 0;
                }
                PlayerPrefs.SetInt("SingleLevels",levelId);
                SceneManager.LoadScene("SinglePlayerGameScene");
            }            
        }
        #region Timer Sync
        void StartGameTimer()
        {
            //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            //h["gameStartTime"] = PhotonNetwork.Time;
            //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
            startTime = gameMode == GameMode.Multiplayer ? Connector.instance.networkRunner.RemoteRenderTime : Time.time;
        }
        void StartGamePrepare()
        {
            if (gameMode == GameMode.Multiplayer)
            {
                //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                //h["gameStartsIn"] = PhotonNetwork.Time + preparationTime;
                //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
                gameStartsIn = Connector.instance.networkRunner.RemoteRenderTime + preparationTime;
            }
            else
            {
                gameStartsIn = Time.deltaTime + preparationTime;
                //startingCountdownStarted = true;
                //gameStarted = true;
            }
        }
        #endregion

        #region Photon calls
        //public override void OnLeftRoom()
        //{
        //    DataCarrier.message = "";
        //    DataCarrier.LoadScene("MainMenu");
        //}
        //public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        //{
        //    if (propertiesThatChanged.ContainsKey("started"))
        //    {
        //        gameStarted = (bool)PhotonNetwork.CurrentRoom.CustomProperties["started"];
        //    }
        //    if (propertiesThatChanged.ContainsKey("gameStartsIn"))
        //    {
        //        gameStartsIn = (double)propertiesThatChanged["gameStartsIn"];
        //        startingCountdownStarted = true;
        //    }
        //    // Game timer:
        //    if (propertiesThatChanged.ContainsKey("gameStartTime"))
        //    {
        //        startTime = (double)propertiesThatChanged["gameStartTime"];
        //        CheckTime();
        //    }
        //    // Check if game is over:
        //    if (propertiesThatChanged.ContainsKey("rankings"))
        //    {
        //        playerRankings = (string[])propertiesThatChanged["rankings"];
        //        isDraw = (bool)propertiesThatChanged["draw"];
        //    }

        //    // Update our copy of bot stats if the online version changed:
        //    if (propertiesThatChanged.ContainsKey("botNames"))
        //    {
        //        bNames = (string[])PhotonNetwork.CurrentRoom.CustomProperties["botNames"];
        //        bots = GenerateBotPlayerInstances();
        //    }
        //    if (propertiesThatChanged.ContainsKey("botScores"))
        //    {
        //        bScores = (Vector3[])PhotonNetwork.CurrentRoom.CustomProperties["botScores"];
        //        bots = GenerateBotPlayerInstances();
        //    }
        //    if (propertiesThatChanged.ContainsKey("botCharacters"))
        //    {
        //        bChars = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botCharacters"];
        //        bots = GenerateBotPlayerInstances();
        //    }
        //}
        //public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        //{
        //    SetPlayerInstance(targetPlayer);
        //    ui.UpdateBoards();
        //}
        //public override void OnMasterClientSwitched(Player newMasterClient)
        //{
        //    // Game timer:
        //    if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("gameStartTime"))
        //    {
        //        StartGameTimer();
        //    }
        //}
        //public override void OnPlayerEnteredRoom(Player newPlayer)
        //{
        //    punPlayersAll = PhotonNetwork.PlayerList;

        //    players = GeneratePlayerInstances(false);
        //}
        //public override void OnPlayerLeftRoom(Player otherPlayer)
        //{
        //    punPlayersAll = PhotonNetwork.PlayerList;

        //    players = GeneratePlayerInstances(true);

        //    // Display a message when someone disconnects/left the game/room:
        //    ui.DisplayMessage(otherPlayer.NickName + " left the match", UIManager.MessageType.LeftTheGame);

        //    // Refresh bot list (only if we have bots):
        //    if (hasBots)
        //    {
        //        bots = GenerateBotPlayerInstances();
        //    }

        //    // Other refreshes:
        //    ui.UpdateBoards();
        //    CheckPlayersLeft();
        //}
        //public override void OnDisconnected(DisconnectCause cause)
        //{
        //    DataCarrier.message = "You've been disconnected from the game!";
        //    DataCarrier.LoadScene("MainMenu");
        //}
        #endregion

        void OnDrawGizmos()
        {
            // Dead zone:
            if (deadZone)
            {
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                Gizmos.DrawCube(new Vector3(0, deadZoneOffset - 50, 0), new Vector3(1000, 100, 0));
            }
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if(Connector.instance.networkRunner.LocalPlayer == player)
            {

            }
            else
            {
                punPlayersAll = Connector.instance.playersList.ToArray();

                players = GeneratePlayerInstances(false);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if(Connector.instance.networkRunner.LocalPlayer == player)
            {
                DataCarrier.message = "";
                DataCarrier.LoadScene("MainMenu");
            }
            else
            {
                punPlayersAll = Connector.instance.playersList.ToArray();

                players = GeneratePlayerInstances(true);
                
                // Refresh bot list (only if we have bots):
                if (hasBots)
                {
                    bots = GenerateBotPlayerInstances();
                }

                // Other refreshes:
                ui.UpdateBoards();

                if (Connector.instance.networkRunner.IsServer)
                {
                    CheckPlayersLeft();
                }
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            //DataCarrier.message = "You've been disconnected from the game!";
            DataCarrier.LoadScene("MainMenu");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            //DataCarrier.message = "You've been disconnected from the game!";
            DataCarrier.LoadScene("MainMenu");
            Connector.instance.networkRunner.Shutdown();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            throw new NotImplementedException();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            throw new NotImplementedException();
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            throw new NotImplementedException();
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            throw new NotImplementedException();
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Game") && controlsManager != null)
            {
                var playerInput = new PlayerInput 
                {
                    xInput = controlsManager.mobileControls ? controlsManager.horizontal : controlsManager.horizontalRaw,
                    yInput = userVerticalAxis ? controlsManager.mobileControls ? controlsManager.vertical : controlsManager.verticalRaw : 0
                };
                input.Set(playerInput);
            }
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            throw new NotImplementedException();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            throw new NotImplementedException();
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            throw new NotImplementedException();
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }
    }

    // Player sorter helper:
    public class PlayerSorter : IComparer
    {
        int IComparer.Compare(object a, object b)
        {
            int p1 = (((PlayerInstance)a).kills) + ((PlayerInstance)a).otherScore;
            int p2 = (((PlayerInstance)b).kills) + ((PlayerInstance)b).otherScore;
            if (p1 == p2)
            {
                return 0;
            }
            else
            {
                GameManager.isDraw = false;  // game isn't a draw if a player as a different score

                if (p1 > p2)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }
    }
    public enum GameMode
    {
        SinglePlayer,
        Multiplayer
    }
}