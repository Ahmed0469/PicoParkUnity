using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;
public class TwoPlayersLevel4 : NetworkBehaviour
{
    [Networked] private Vector3 networkWeightBlockPos { get; set; }
    public ContactFilter2D contactFilter;
    public List<GameObject> triangleObjList = new List<GameObject>();
    public LevelItem weightBoxLevelItem;
    Vector2 veloc;
    public Transform weightBoxObj;
    // Start is called before the first frame update
    void Start()
    {
        weightBoxLevelItem.onTriggerStayNew.AddListener(OnCollisionStayWeightBox);
    }
    public override void FixedUpdateNetwork()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            networkWeightBlockPos = weightBoxObj.transform.position;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!Connector.instance.networkRunner.IsServer)
        {
            weightBoxObj.transform.position = Vector2.SmoothDamp(weightBoxObj.transform.position, networkWeightBlockPos, ref veloc,0.1f);
        }
    }
    public void OnCollisionStayWeightBox(Collider2D collision, GameObject weightBox)
    {
        if (collision != null && collision.gameObject != null)
        {
            if (collision.gameObject.TryGetComponent<PlayerController>(out var playerController))
            {
                weightBoxObj.transform.position += new Vector3(playerController.rg.position.x - weightBoxObj.transform.position.x, 0);
            }
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
                    veloc.y = 27;
                    playerController.rg.velocity = veloc;

                    // Don't allow jumping right after a jump:
                    playerController.allowJump = false;
                }
                else if (colliders[i].TryGetComponent<Rigidbody2D>(out var collidedRb))
                {
                    Vector2 veloc = collidedRb.velocity;
                    veloc.y = 25;
                    //veloc.x = 5;
                    collidedRb.velocity = veloc;
                }
            }
        }
    }
    public void SubsscribeTriangleTriggers(LevelItem levelItem)
    {
        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerEnterNew.AddListener(OnTriggerTriangleFall);
    }
    public void OnTriggerTriangleFall(Collider2D collision,GameObject _gameObject)
    {
        if (collision != null)
        {
            int triangleId = triangleObjList.IndexOf(_gameObject.transform.parent.gameObject);
            RPC_FallTriangle(triangleId);
        }
    }
    [Rpc]
    public void RPC_FallTriangle(int triangleId)
    {
        triangleObjList[triangleId].GetComponent<Rigidbody2D>().gravityScale = 10;        
    }
    public void SubsscribeTrianglesObj(LevelItem levelItem)
    {
        levelItem.onCollisionEnterNew.RemoveAllListeners();
        levelItem.onCollisionEnterNew.AddListener(OnCollisionTriangle);
    }
    public void OnCollisionTriangle(Collision2D collision, GameObject _gameObject)
    {
        if (collision != null)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<PlayerController>().Die();
                RPC_GameOver();
            }
            else
            {
                int triangleId = triangleObjList.IndexOf(_gameObject);
                RPC_DestroyTriangle(triangleId);
            }          
        }
    }
    [Rpc]
    public void RPC_DestroyTriangle(int triangleId)
    {
        triangleObjList[triangleId].SetActive(false);
    }
    [Rpc]
    public void RPC_GameOver()
    {
        GameManager.instance.isGameOver = true;
    }
}
