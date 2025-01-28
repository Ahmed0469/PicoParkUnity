using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;

public class TwoPlayersLevel3 : NetworkBehaviour
{
    public Transform coinPrefab;
    public GameObject key;
    public List<Transform> coinSpawns = new List<Transform>();
    public ContactFilter2D contactFilter;
    int coinsCollected;
    // Start is called before the first frame update
    void Start()
    {
        if (Object.Runner.IsServer)
        {
            for (int i = 0; i < coinSpawns.Count; i++)
            {
                var coin = Object.Runner.Spawn(coinPrefab.gameObject, coinSpawns[i].position, coinSpawns[i].rotation, Object.InputAuthority);
                coin.GetComponent<LevelItem>().onTriggerEnter.AddListener(() =>
                {
                    Collider2D[] colliders = new Collider2D[1];
                    coin.GetComponent<CircleCollider2D>().OverlapCollider(contactFilter, colliders);
                    if (colliders[0] != null)
                    {
                        Object.Runner.Despawn(coin);
                        coinsCollected++;
                        if (coinsCollected == coinSpawns.Count)
                        {
                            RPC_SpawnKey();
                        }
                    }
                });
            }
        }
    }
    [Rpc]
    public void RPC_SpawnKey()
    {
        key.SetActive(true);
    }

    public void Jumper(GameObject jumper)
    {
        if (!Connector.instance.networkRunner.IsServer)
        {
            return;
        }
        var colliders = new Collider2D[2];
        jumper.GetComponent<BoxCollider2D>().OverlapCollider(contactFilter, colliders);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                var playerController = colliders[i].GetComponent<PlayerController>();
                Vector2 veloc = playerController.rg.velocity;
                veloc.y = 30;
                RPC_JumperSoundSound();
                playerController.rg.velocity = veloc;

                // Don't allow jumping right after a jump:
                playerController.allowJump = false;
            }            
        }
    }
    [Rpc]
    public void RPC_JumperSoundSound()
    {
        SoundManager.instance.PlayOneShot(SoundManager.instance.JumperSFX);
    }
}
