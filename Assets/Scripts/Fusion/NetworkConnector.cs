using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Visyde;
using GameMode = Fusion.GameMode;

public class NetworkConnector : MonoBehaviour, INetworkRunnerCallbacks
{
    public InputField createRoomInputField;
    public NetworkRunner networkRunner;
    public ControlsManager controlsManager;
    // Start is called before the first frame update
    void Start()
    {
        networkRunner = gameObject.AddComponent<NetworkRunner>();
    }
    private void OnDestroy()
    {
    }
    public async void JoinRoom()
    {
        string roomName = createRoomInputField.text.Trim();
        var startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
        };

        var result = await networkRunner.StartGame(startGameArgs);
        if (result.Ok)
        {
            Debug.Log($"Joined room: {roomName}");
        }
        else
        {
            Debug.LogError($"Failed to join room: {result.ShutdownReason}");
        }
    }
    public async void CreateRoom()
    {
        string roomName = createRoomInputField.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name cannot be empty.");
            return;
        }

        var startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
        };

        var result = await networkRunner.StartGame(startGameArgs);
        if (result.Ok)
        {
            Debug.Log($"Room created: {roomName}");
        }
        else
        {
            Debug.LogError($"Failed to create room: {result.ShutdownReason}");
        }
    }
    public SceneRef scene;
    public void LoadGameScene()
    {
        if (networkRunner.IsServer)
        {
            networkRunner.LoadScene(scene);
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
    [HideInInspector] public List<PlayerRef> playersList = new List<PlayerRef>();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (networkRunner.IsServer)
        {
            Debug.LogError("Connecteddddd");
            playersList.Add(player);
            runner.ProvideInput = true;
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
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
            var playerInput = new PlayerInput { xInput = controlsManager.mobileControls ? controlsManager.horizontal : controlsManager.horizontalRaw };
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