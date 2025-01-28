using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class FourPlayersLevel1 : NetworkBehaviour
{
    public Transform ballSpawnPoint;
    public Transform ballPrefab;
    public ContactFilter2D contactFilter;
    public GameObject vase;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(6);
        SpawnBall();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public async void SpawnBall()
    {
        if (Object.Runner.IsServer)
        {
            RPC_BallSpawnSound();
            var ball = await Object.Runner.SpawnAsync(ballPrefab.gameObject, ballSpawnPoint.position, ballSpawnPoint.rotation, Object.Runner.LocalPlayer);                
            var ballRb = ball.GetComponent<Rigidbody2D>();
            ballRb.AddForce(Vector2.left * 50);
            ballRb.GetComponent<BallScript>().OnCollisionEnter += BallOnCollisionEnter;                      
        }
    }

    public void BallOnCollisionEnter(Collision2D collision,GameObject _gameObject)
    {
        if (Object.Runner.IsServer)
        {            
            if (collision != null && !collision.gameObject.CompareTag("Player") && !Visyde.GameManager.instance.LevelWinScreen.activeInHierarchy && collision.transform.parent.name != "Vase")
            {
                Object.Runner.Despawn(_gameObject.GetComponent<NetworkObject>());
                SpawnBall();
            }
        }        
    }
    public void VaseOnTriggerEnter(GameObject vaseTrigger)
    {
        if (Object.Runner.IsServer)
        {
            Collider2D[] colliders = new Collider2D[1];
            vaseTrigger.GetComponent<Collider2D>().OverlapCollider(contactFilter, colliders);
            if (colliders[0] != null && colliders[0].name.Contains("Ball"))
            {
                Object.Runner.Despawn(colliders[0].gameObject.GetComponent<NetworkObject>());
                RPC_DestoryVase();
            }
        }        
    }
    [Rpc]
    public void RPC_BallSpawnSound()
    {
        SoundManager.instance.PlayOneShot(SoundManager.instance.cannonSFX);
    }
    [Rpc]
    void RPC_DestoryVase()
    {
        SoundManager.instance.PlayOneShot(SoundManager.instance.vaseSFX);
        vase.SetActive(false);
    }
}
