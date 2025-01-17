using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class FourPlayersLevel2 : NetworkBehaviour
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
                var coin = Object.Runner.Spawn(coinPrefab.gameObject,coinSpawns[i].position, coinSpawns[i].rotation,Object.InputAuthority);
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
}
