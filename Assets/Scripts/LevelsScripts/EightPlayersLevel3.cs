using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;

public class EightPlayersLevel3 : NetworkBehaviour
{
    public Transform mapTransformPos;
    public Transform mapEndPos;
    public GameObject planePrefab;
    public ContactFilter2D contactFilter;
    // Start is called before the first frame update
    void Start()
    {
        GameManager.instance.userVerticalAxis = true;
        GameManager.instance.jumpBtn.gameObject.SetActive(false);
    }
    public override void Spawned()
    {
        StartCoroutine(WaitSpawn());
    }
    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.gameStarted && !GameManager.instance.isGameOver && !GameManager.instance.LevelWinScreen.activeInHierarchy)
        {
            mapTransformPos.localPosition = Vector3.MoveTowards(mapTransformPos.localPosition, mapEndPos.position, Time.deltaTime * 7);
        }
    }
    IEnumerator WaitSpawn()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        SpawnPlanes();
    }
    async void SpawnPlanes()
    {
        var players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            var playerRb = players[i].GetComponent<Rigidbody2D>();
            playerRb.gravityScale = 0;
            playerRb.velocity = Vector2.zero;
            players[i].transform.position = new Vector2(players[i].rg.position.x, 3.87f);
            players[i].PlayerAtBottomCheckTrigger.gameObject.SetActive(false);
            players[i].stopLookingBackwards = true;
            if (Connector.instance.networkRunner.IsServer)
            {
                await Connector.instance.networkRunner.SpawnAsync
                (
                planePrefab,
                players[i].transform.position,
                Quaternion.identity,
                players[i].Object.InputAuthority
                );
            }            
        }
    }
    public void OnCollisionWithWalls(GameObject _object)
    {
        if (!Connector.instance.networkRunner.IsServer)
        {
            return;
        }
        var colliders = new Collider2D[5];
        _object.GetComponent<BoxCollider2D>().OverlapCollider(contactFilter,colliders);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                Debug.Log("Collided With " + colliders[i].name);
                //if (colliders[i].transform.CompareTag("Player"))
                //{
                //    colliders[i].GetComponent<PlayerController>().Die();
                //    RPC_GameOver();
                //}
                //if (colliders[i].transform.parent != null && colliders[i].transform.parent.CompareTag("Player"))
                //{
                //colliders[i].GetComponent<PlayerController>().Die();
                RPC_GameOver();
                //}
            }
        }
    }
    [Rpc]
    public void RPC_GameOver()
    {
        GameManager.instance.isGameOver = true;
    }
}
