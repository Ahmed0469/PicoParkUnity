using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;
using Visyde;

public class FourPlayersLevel3 : NetworkBehaviour
{
    [Networked] Vector3 networkRightPlatformPos { get; set; }
    [Networked] Vector3 networkLeftPlatformPos { get; set; }
    [Networked] Quaternion networkTilesRot { get; set; }
    [Networked] float networkSubtractedAmount { get; set; }

    public GameObject key;
    public Transform ballSpawnPoint;
    public Transform ballPrefab;
    public List<Transform> rotatingTiles = new();
    public Collider2D rightPlatform;
    public Collider2D leftPlatform;
    public LevelItem keyBtn;
    public ContactFilter2D contactFilter;
    Vector3 rightPlatformDefaultPos;
    Vector3 leftPlatformDefaultPos;
    Vector3 rightVeloc;
    Vector3 leftVeloc;
    private IEnumerator Start()
    {
        rightPlatformDefaultPos = rightPlatform.transform.parent.localPosition;
        leftPlatformDefaultPos = leftPlatform.transform.parent.localPosition;
        keyBtn.onTriggerEnterNew.AddListener(OnTriggerEnterBtn);
        yield return new WaitForSecondsRealtime(6);
        SpawnBall();
    }
    public async void SpawnBall()
    {
        if (Object.Runner.IsServer)
        {
            var ball = await Object.Runner.SpawnAsync(ballPrefab.gameObject, ballSpawnPoint.position, ballSpawnPoint.rotation, Object.Runner.LocalPlayer);
            RPC_BallSpawnSound();
            ball.GetComponent<BallScript>().OnCollisionEnter += BallOnCollisionEnter;
        }
    }
    public void BallOnCollisionEnter(Collision2D collision, GameObject _gameObject)
    {
        if (Object.Runner.IsServer)
        {
            if (collision != null && collision.gameObject.name == "Box" && !GameManager.instance.LevelWinScreen.activeInHierarchy)
            {
                Object.Runner.Despawn(_gameObject.GetComponent<NetworkObject>());
                StartCoroutine(SpawnBallWithWait());
            }
        }
    }
    IEnumerator SpawnBallWithWait()
    {
        yield return new WaitForSecondsRealtime(1f);
        SpawnBall();
    }
    public void OnTriggerEnterBtn(Collider2D collision, GameObject _gameobject)
    {
        if(collision != null)
        {
            RPC_SpawnKey();
        }
    }
    [Rpc]
    public void RPC_BallSpawnSound()
    {
        SoundManager.instance.PlayOneShot(SoundManager.instance.cannonSFX);
    }
    [Rpc]
    public void RPC_SpawnKey()
    {
        key.SetActive(true);

    }
    private void Update()
    {
        if (!Connector.instance.networkRunner.IsServer)
        {
            rightPlatform.transform.parent.localPosition = Vector3.SmoothDamp
                (rightPlatform.transform.parent.localPosition,
                networkRightPlatformPos,
                ref rightVeloc,
                0.1f
                );
            leftPlatform.transform.parent.localPosition = Vector3.SmoothDamp
                (leftPlatform.transform.parent.localPosition,
                networkLeftPlatformPos,
                ref leftVeloc,
                0.1f
                );
            for (int i = 0; i < rotatingTiles.Count; i++)
            {
                rotatingTiles[i].rotation = Quaternion.RotateTowards(rotatingTiles[i].rotation, networkTilesRot, Time.deltaTime * 4 * (networkSubtractedAmount == 0 ? 1 : networkSubtractedAmount < 0 ? -networkSubtractedAmount : networkSubtractedAmount));

                if (Quaternion.Angle(rotatingTiles[i].rotation, networkTilesRot) < 0.1f)
                {
                    rotatingTiles[i].rotation = networkTilesRot;
                }
            }
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            var rightPlatformColliders = new Collider2D[4];
            rightPlatform.OverlapCollider(contactFilter, rightPlatformColliders);
            int rightPlatformPlayers = rightPlatformColliders.ToList().FindAll(player => player != null).Count;

            var leftPlatformColliders = new Collider2D[4];
            leftPlatform.OverlapCollider(contactFilter, leftPlatformColliders);
            int leftPlatformPlayers = leftPlatformColliders.ToList().FindAll(player => player != null).Count;


            int subtractedAmount = leftPlatformPlayers - rightPlatformPlayers;

            var targetRot = Quaternion.Euler(0, 0, subtractedAmount > 0 ? 10 : subtractedAmount < 0 ? -10 : 0);

            if (rightPlatformPlayers > 0)
            {
                rightPlatform.transform.parent.localPosition = Vector3.MoveTowards
                (
                rightPlatform.transform.parent.localPosition,
                new Vector3(rightPlatform.transform.parent.localPosition.x, -1.05f),
                (Time.deltaTime * (subtractedAmount == 0 ? 1 : subtractedAmount < 0 ? -subtractedAmount : subtractedAmount)) / 2
                );
            }
            else
            {
                rightPlatform.transform.parent.localPosition = Vector3.MoveTowards
                (
                rightPlatform.transform.parent.localPosition,
                rightPlatformDefaultPos,
                Time.deltaTime * 2
                );
            }
            if (leftPlatformPlayers > 0)
            {
                leftPlatform.transform.parent.localPosition = Vector3.MoveTowards
                (
                leftPlatform.transform.parent.localPosition,
                new Vector3(leftPlatform.transform.parent.localPosition.x, -1.05f),
                (Time.deltaTime * (subtractedAmount == 0 ? 1 : subtractedAmount < 0 ? -subtractedAmount : subtractedAmount)) / 2
                );
            }
            else
            {
                leftPlatform.transform.parent.localPosition = Vector3.MoveTowards
                (
                leftPlatform.transform.parent.localPosition,
                leftPlatformDefaultPos,
                Time.deltaTime * 2
                );
            }


            for (int i = 0; i < rotatingTiles.Count; i++)
            {
                rotatingTiles[i].rotation = Quaternion.RotateTowards(rotatingTiles[i].rotation, targetRot, Time.deltaTime * 4 * (subtractedAmount == 0 ? 1 : subtractedAmount < 0 ? -subtractedAmount : subtractedAmount));

                if (Quaternion.Angle(rotatingTiles[i].rotation, targetRot) < 0.1f)
                {
                    rotatingTiles[i].rotation = targetRot;
                }
            }
            networkRightPlatformPos = rightPlatform.transform.parent.localPosition;
            networkLeftPlatformPos = leftPlatform.transform.parent.localPosition;
            networkTilesRot = targetRot;
            networkSubtractedAmount = subtractedAmount;
        }
    }
}
