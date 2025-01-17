using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;

namespace Visyde
{    
    /// <summary>
    /// Game Map
    /// - handles map properties such as world bounds and spawn points
    /// </summary>

    public class GameMap : NetworkBehaviour
    {

        [Networked] public bool gameStarted { get; set; }
        [Networked] public float gameStartsIn { get; set; }
        [Networked] public float startTime { get; set; }
        [Networked] public bool isGameOver { get; set; }
        [System.Serializable]
        public class WeaponSpawnPoint
        {
            public Transform point;
            public Weapon onlySpawnThisHere;
        }
        [System.Serializable]
        public class PowerUpSpawnPoint
        {
            public Transform point;
            public PowerUp onlySpawnThisHere;
        }

        [Header("Player Spawn Points:")]
        public List<Transform> playerSpawnPoints;
        [Space]
        [Header("Item Spawn Points:")]
        public WeaponSpawnPoint[] weaponSpawnPoints;
        public PowerUpSpawnPoint[] powerUpSpawnPoints;
        [Space]
        [Header("Spawnable Items:")]
        public Weapon[] spawnableWeapons;
        public PowerUp[] spawnablePowerUps;

        //[Space]
        //[Header("Others:")]
        //public Transform btn1Transfrom;
        //public Transform btn2Transfrom;
        //public Transform keyTransform;
        //public Transform gateTransform;

        [Space]
        [Header("Others:")]
        public GameObject deadZoneVFX;

        [Space]
        [Header("Camera View:")]
        public Vector2 bounds;
        public Vector2 boundOffset;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(boundOffset.x, boundOffset.y), new Vector3(bounds.x * 2, bounds.y * 2, 0));
        }

        public override void FixedUpdateNetwork()
        {
            if (GameManager.gameMode == GameMode.Multiplayer)
            {
                if (Object.HasStateAuthority)
                {
                    gameStarted = GameManager.instance.gameStarted;
                    gameStartsIn = (float)GameManager.instance.gameStartsIn;
                    startTime = (float)GameManager.instance.startTime;
                    isGameOver = GameManager.instance.isGameOver;
                }
            }            
        }
        private void FixedUpdate()
        {
            if (GameManager.gameMode == GameMode.Multiplayer)
            {
                if (!Object.HasStateAuthority)
                {
                    GameManager.instance.gameStarted = gameStarted;
                    GameManager.instance.gameStartsIn = gameStartsIn;
                    GameManager.instance.startTime = startTime;
                    GameManager.instance.isGameOver = isGameOver;
                }
            }            
        }
    }
}
