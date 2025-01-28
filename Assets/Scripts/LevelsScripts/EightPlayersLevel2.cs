using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;

public class EightPlayersLevel2 : NetworkBehaviour
{
    public GameObject gameHat;
    public GameObject key;
    public ContactFilter2D contactFilter;
    public List<GameObject> balls = new List<GameObject>();
    public List<GameObject> glassBlocks = new List<GameObject>();
    public override void Spawned()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            for (int i = 0; i < glassBlocks.Count; i++)
            {
                glassBlocks[i].GetComponent<LevelItem>().onCollisionEnterNew.AddListener(OnCollisionGlassBlock);
            }
            for (int i = 0; i < balls.Count; i++)
            {
                balls[i].GetComponent<BallScript>().OnCollisionEnter += OnCollisionBall;
                balls[i].GetComponent<BallScript>().OnCollisionExit += OnCollisionExitBall;
            }
            StartCoroutine(WaitSpawn());
        }
    }
    IEnumerator WaitSpawn()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        SpawnHat();
    }
    async void SpawnHat()
    {
        var players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].character.hatPoint.childCount > 0)
            {
                Destroy(players[i].character.hatPoint.GetChild(0).gameObject);
            }
            await Connector.instance.networkRunner.SpawnAsync
                (
                 gameHat,
                 players[i].character.hatPoint.position, Quaternion.identity,
                 players[i].Object.InputAuthority
                );            
        }
    }
    public void OnCollisionBall(Collision2D collision,GameObject ball)
    {
        //ball.GetComponent<Rigidbody2D>().AddForce((ball.transform.position - collision.transform.position).normalized * 150f);
        if (collision.gameObject.name == "Floor")
        {
            int ballId = balls.IndexOf(ball);
            RPC_DestroyBall(ballId);
        }
    }
    public void OnCollisionExitBall(Collision2D collision, GameObject ball)
    {
        //var ballDir = ball.transform.position;
        var ballRb = ball.GetComponent<Rigidbody2D>();
        float velocX = ballRb.velocity.x != 0 ? ballRb.velocity.x < 0 ? -4 : 4 : 0;
        float velocY = ballRb.velocity.y != 0 ? ballRb.velocity.y < 0 ? -4 : 4 : 0;
        //ballRb.velocity = new Vector2(velocX,velocY);
        //ball.GetComponent<Rigidbody2D>().AddForce(ballDir,ForceMode2D.Impulse);
        ballRb.velocity = new Vector2(velocX,velocY);
    }
    public void OnCollisionGlassBlock(Collision2D collision,GameObject block)
    {
        if (collision != null && collision.gameObject.TryGetComponent<BallScript>(out var ball))
        {
            int blockId = glassBlocks.IndexOf(block);
            //StartCoroutine(DestroyBlock(blockId));
            RPC_DestroyBlock(blockId);
        }
    }
    IEnumerator DestroyBlock(int blockId)
    {
        yield return new WaitForSecondsRealtime(0.07f);
        RPC_DestroyBlock(blockId);
    }
    [Rpc]
    public void RPC_DestroyBall(int ballId)
    {
        balls[ballId].SetActive(false);
        if (Connector.instance.networkRunner.IsServer)
        {
            int ballsLeft = balls.FindAll(ball => ball.activeInHierarchy).Count;
            if (ballsLeft < 1)
            {
                RPC_GameOver();
            }
        }
    }
    [Rpc]
    public void RPC_GameOver()
    {
        GameManager.instance.isGameOver = true;
    }
    [Rpc]
    public void RPC_DestroyBlock(int blockId)
    {
        glassBlocks[blockId].SetActive(false);
        SoundManager.instance.PlayOneShot(SoundManager.instance.wallBreakSFX);
        if (Connector.instance.networkRunner.IsServer)
        {
            int blocksLeft = glassBlocks.FindAll(block => block.activeInHierarchy).Count;
            if (blocksLeft < 1)
            {
                RPC_SpawnKey();
            }
        }
    }
    [Rpc]
    public void RPC_SpawnKey()
    {
        if (key != null)
        {
            key.SetActive(true);
        }
    }
}
