using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EightPlayersLevel1 : NetworkBehaviour
{
    public Transform ballSpawnPoint;
    public GameObject ballPrefab;
    public ContactFilter2D contactFilter;
    [HideInInspector] public int vaseHealth = 5;
    public GameObject vase;
    public bool isVaseBroken = false;
    PlayerChecker playerChecker;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        playerChecker = FindObjectOfType<PlayerChecker>();
        yield return new WaitForSecondsRealtime(7);
        StartCoroutine(SpawnBall());
    }

    private IEnumerator SpawnBall()
    {
        if (!Object.Runner.IsServer) yield return null;
        if (isVaseBroken) yield return null;

        yield return new WaitUntil(() => !playerChecker.isPlayerThere);
        var ballRb = Object.Runner.Spawn(ballPrefab, ballSpawnPoint.position,ballSpawnPoint.rotation,Object.InputAuthority).GetComponent<Rigidbody2D>();
        RPC_BallSpawnSound();
        ballRb.GetComponent<BallScript>().OnCollisionEnter += BallOnCollisionEnter;
        ballRb.gravityScale = 0;
        //ballRb.sharedMaterial.bounciness = 0;
        ballRb.AddForce(Vector2.left * 250);
    }
    public void BallOnCollisionEnter(Collision2D collision,GameObject ball)
    {
        if (collision != null)
        {
            //if (colliders[0].CompareTag("Player"))
            //{
            //    ball.SetActive(false);
            //}
            /*else*/ if (collision.transform.parent != null && collision.transform.parent.name == "Vase")
            {
                vaseHealth--;
                //ball.SetActive(false);
                if (vaseHealth == 0)
                {
                    RPC_DestroyVase();
                }
            }
            Object.Runner.Despawn(ball.GetComponent<NetworkObject>());
            StartCoroutine(CR());
        }
    }
    [Rpc]
    public void RPC_DestroyVase()
    {
        SoundManager.instance.PlayOneShot(SoundManager.instance.vaseSFX);
        isVaseBroken = true;
        vase.SetActive(false);
    }
    [Rpc]
    public void RPC_BallSpawnSound()
    {
        SoundManager.instance.PlayOneShot(SoundManager.instance.cannonSFX);
    }
    IEnumerator CR()
    {
        yield return new WaitForSecondsRealtime(1);
        StartCoroutine(SpawnBall());
    }
}
