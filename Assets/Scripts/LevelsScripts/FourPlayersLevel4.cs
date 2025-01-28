using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Linq;
using Visyde;

public class FourPlayersLevel4 : NetworkBehaviour
{
    [Networked] Vector2 networkRightWallPos { get; set; }
    [Networked] Vector2 networkRightWallScale { get; set; }
    Vector2 veloc;
    public Transform extendingFloor;
    public Transform extendingFloorAfterPos;
    public Transform leftWall;
    public Transform leftWallAfterPos;
    public Transform rightWall;
    public Transform rightWallAfterPos;
    public Transform fallPos;
    public GameObject weightBlockPrefab;
    public Transform weightBlock1SpawnPos;
    public Transform weightBlock2SpawnPos;
    Vector2 rightWallDefaultPos;
    Vector2 rightWallDefaultScale;
    public LevelItem singleBtn;
    public List<LevelItem> weightBtns = new List<LevelItem>();
    public List<int> btnInts = new List<int>();
    // Start is called before the first frame update
    void Start()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            for (int i = 0; i < weightBtns.Count; i++)
            {
                weightBtns[i].onTriggerStayNew.AddListener((collision,_gameobject)=> 
                {
                    btnInts[weightBtns.IndexOf(_gameobject.GetComponent<LevelItem>())] = 1;
                });
                weightBtns[i].onTriggerExitNew.AddListener((collision, _gameobject) =>
                {
                    btnInts[weightBtns.IndexOf(_gameobject.GetComponent<LevelItem>())] = 0;
                });
            }
            singleBtn.onTriggerEnterNew.AddListener(ExtendFloor);            
        }        
    }
    public override void Spawned()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            rightWallDefaultPos = rightWall.position;
            rightWallDefaultScale = rightWall.localScale;
            SpawnWeightBlocsk();
        }
        if (Connector.instance.networkRunner.SessionInfo.PlayerCount > 2)
        {
            weightBtns[3].gameObject.SetActive(true);
        }
        if (Connector.instance.networkRunner.SessionInfo.PlayerCount > 3)
        {
            weightBtns[4].gameObject.SetActive(true);
        }
    }
    async void SpawnWeightBlocsk()
    {
        var block1 = await Connector.instance.networkRunner.SpawnAsync
            (
            weightBlockPrefab,
            weightBlock1SpawnPos.position,
            weightBlock1SpawnPos.rotation,
            Object.InputAuthority
            );
        block1.transform.localScale = weightBlock1SpawnPos.localScale;

        var block2 = await Connector.instance.networkRunner.SpawnAsync
            (
            weightBlockPrefab,
            weightBlock2SpawnPos.position,
            weightBlock2SpawnPos.rotation,
            Object.InputAuthority
            );
        block2.transform.localScale = weightBlock2SpawnPos.localScale;
    }
    // Update is called once per frame
    void Update()
    {
        if (!Connector.instance.networkRunner.IsServer)
        {
            rightWall.transform.position = Vector2.SmoothDamp(rightWall.transform.position, networkRightWallPos, ref veloc, 0.1f);
            rightWall.transform.localScale = Vector2.SmoothDamp(rightWall.transform.localScale, networkRightWallScale, ref veloc, 0.1f);
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            //int product = btnInts.Aggregate(1, (acc, num) => acc * num);
            int product = btnInts[0] * btnInts[1] * btnInts[2] * (weightBtns[3].gameObject.activeInHierarchy ? btnInts[3] : 1) * (weightBtns[4].gameObject.activeInHierarchy ? btnInts[4] : 1);
            if (product == 1)
            {
                rightWall.position = Vector2.Lerp(rightWall.position, rightWallAfterPos.position, Time.deltaTime * 5);
                rightWall.localScale = Vector2.Lerp(rightWall.localScale, rightWallAfterPos.localScale, Time.deltaTime * 5);
            }
            else
            {
                rightWall.position = Vector2.Lerp(rightWall.position, rightWallDefaultPos, Time.deltaTime * 5);
                rightWall.localScale = Vector2.Lerp(rightWall.localScale, rightWallDefaultScale, Time.deltaTime * 5);
            }
            networkRightWallPos = rightWall.position;
            networkRightWallScale = rightWall.localScale;
        }        
    }
    public void ExtendFloor(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            RPC_ExtendFloor();
        }
    }
    [Rpc]
    public void RPC_ExtendFloor()
    {
        StartCoroutine(ExtendFloorCR());
    }
    IEnumerator ExtendFloorCR()
    {
        float elapsedTime = 0;
        while (elapsedTime <= 2)
        {
            extendingFloor.position = Vector2.Lerp(extendingFloor.position,extendingFloorAfterPos.position,Time.deltaTime * 5);
            extendingFloor.localScale = Vector2.Lerp(extendingFloor.localScale, extendingFloorAfterPos.localScale, Time.deltaTime * 5);
            leftWall.position = Vector2.Lerp(leftWall.position, leftWallAfterPos.position, Time.deltaTime * 5);
            leftWall.localScale = Vector2.Lerp(leftWall.localScale, leftWallAfterPos.localScale, Time.deltaTime * 5);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }     
    public void SubscribeOnFallTrigger(LevelItem levelItem)
    {
        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerEnterNew.AddListener(RepositionFallPlayer);
    }
    public void RepositionFallPlayer(Collider2D collision, GameObject _gameObject)
    {
        collision.transform.position = fallPos.position;
    }
}
