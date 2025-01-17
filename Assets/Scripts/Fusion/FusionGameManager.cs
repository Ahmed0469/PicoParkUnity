using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;

//public class FusionGameManager : MonoBehaviour
//{
//    public GameObject playerPrefab;
//    public List<Transform> playerSpawns = new List<Transform>();
//    public ControlsManager controlsManager;
//    // Start is called before the first frame update
//    async void Start()
//    {
//        var networkConnector = FindObjectOfType<Connector>();
//        networkConnector.controlsManager = controlsManager;
//        if (networkConnector.networkRunner.IsServer)
//        {
//            for (int i = 0; i < networkConnector.playersList.Count; i++)
//            {
//                  var player = await networkConnector.networkRunner.SpawnAsync(
//                    playerPrefab,
//                    new Vector2(-23.56f, 0.29f),
//                    Quaternion.identity,
//                    networkConnector.playersList[i]                    
//                    );

//                //player.GetComponent<PlayerController>().networkPos = new Vector2(-23.56f, 0.29f);
//            }
//        }
//    }
//    // Update is called once per frame
//    void Update()
//    {
        
//    }
//}
