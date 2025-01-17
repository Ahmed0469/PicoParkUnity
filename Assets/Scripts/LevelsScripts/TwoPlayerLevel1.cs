using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using Visyde;
using TMPro;

public class TwoPlayerLevel1 : NetworkBehaviour
{
    [Networked] private Vector3 networkWeightBlockPos { get; set; }
    [Networked] private Vector3 networkElevatorPos { get; set; }
    Vector2 veloc = Vector3.zero;
    public bool isLevelTwelve = false;
    public List<GameObject> bridgeBlocks = new List<GameObject>();
    public ContactFilter2D contactFilter;
    public BoxCollider2D playerElevator;
    public LevelItem weightBoxLevelItem;
    public Transform weightBoxObj;
    public TextMeshPro elevatorText;
    Vector2 elevatorDefaultPos;

    private void Start()
    {
        GameManager.instance.deadZone = true;
        if (isLevelTwelve)
        {
            weightBoxLevelItem.onTriggerStayNew.AddListener(OnCollisionStayWeightBox);
        }
    }
    public override void Spawned()
    {
        if (isLevelTwelve)
        {
            elevatorDefaultPos = playerElevator.transform.parent.position;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (isLevelTwelve)
        {
            if (!Object.HasStateAuthority)
            {
                playerElevator.transform.parent.position = Vector2.SmoothDamp(transform.position, networkElevatorPos, ref veloc, 0.1f);
                weightBoxObj.transform.position = Vector2.SmoothDamp(weightBoxObj.transform.position, networkWeightBlockPos, ref veloc, 0.1f);
            }
        }        
    }
    public void OnCollisionStayWeightBox(Collider2D collision,GameObject weightBox)
    {
        if (collision != null && collision.gameObject != null)
        {
            if (collision.gameObject.TryGetComponent<PlayerController>(out var playerController))
            {
                weightBoxObj.transform.position += new Vector3(playerController.rg.position.x - weightBoxObj.transform.position.x, 0);
            }
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (isLevelTwelve)
        {
            if (Connector.instance.networkRunner.IsServer)
            {
                networkWeightBlockPos = weightBoxObj.transform.position;
                var colliders = new Collider2D[10];
                playerElevator.OverlapCollider(contactFilter, colliders);
                var collideds = colliders.ToList().FindAll(col => col != null);
                RPC_ElevatorText(collideds.Count);
                if (collideds.Count == Connector.instance.networkRunner.SessionInfo.PlayerCount + 1)
                {
                    playerElevator.transform.parent.position = Vector2.Lerp(playerElevator.transform.parent.position, new Vector3(playerElevator.transform.parent.position.x, 5), Time.deltaTime / 2);
                }
                else
                {
                    playerElevator.transform.parent.position = Vector2.Lerp(playerElevator.transform.parent.position, elevatorDefaultPos, Time.deltaTime * 1.3f);
                }
                networkElevatorPos = playerElevator.transform.parent.position;
            }
        }        
    }
    public void OnBridgeCollisionEnter(GameObject bridgeBlock)
    {
        RPC_DestroyBlock(bridgeBlock.name);
    }

    [Rpc]
    void RPC_DestroyBlock(string bridgeBlockName)
    {
        StartCoroutine(DestroyBlockCoroutine(bridgeBlockName));
    }
    [Rpc]
    public void RPC_ElevatorText(int collidesCount)
    {
        elevatorText.text = Connector.instance.networkRunner.SessionInfo.PlayerCount + 1 - collidesCount + "";
    }
    IEnumerator DestroyBlockCoroutine(string blockName)
    {
        yield return new WaitForSecondsRealtime(0.75f);
        GameObject blockToDestroy = bridgeBlocks.Find(block => block.name == blockName);
        if (blockToDestroy != null)
        {
            blockToDestroy.SetActive(false);
        }
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
                if (colliders[i].TryGetComponent<PlayerController>(out var playerController))
                {
                    Vector2 veloc = playerController.rg.velocity;
                    veloc.y = 20;
                    playerController.rg.velocity = veloc;

                    // Don't allow jumping right after a jump:
                    playerController.allowJump = false;
                }
                else if(colliders[i].TryGetComponent<Rigidbody2D>(out var collidedRb))
                {
                    Vector2 veloc = collidedRb.velocity;
                    veloc.y = 10;
                    //veloc.x = 5;
                    collidedRb.velocity = veloc;
                }          
            }            
        }
    }
}
