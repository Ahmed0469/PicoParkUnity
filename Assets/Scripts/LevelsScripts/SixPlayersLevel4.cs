using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class SixPlayersLevel4 : NetworkBehaviour
{
    [Networked] private Vector3 networkElevatorPos { get; set; }
    public List<GameObject> btnsList = new();
    public Slider healthSlider;
    Vector2 veloc = Vector3.zero;
    public ContactFilter2D contactFilter;
    public BoxCollider2D playerElevator;
    public TextMeshPro elevatorText;
    public GameObject key;
    Vector2 elevatorDefaultPos;
    float elapsedTime = 0;

    int btn1 = 0;
    int btn2 = 0;
    int btn3 = 0;
    int btn4 = 0;
    int btn5 = 0;
    int btn6 = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    public override void Spawned()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            elevatorDefaultPos = playerElevator.transform.parent.position;
            networkElevatorPos = playerElevator.transform.parent.position;
            StartCoroutine(WaitSpawn());
        }
        for (int i = 0; i < Connector.instance.networkRunner.SessionInfo.PlayerCount; i++)
        {
            if (i < btnsList.Count)
            {
                btnsList[i].SetActive(true);
            }
        }
    }
    IEnumerator WaitSpawn()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        StartCoroutine(PlayerColideHealthDecreaseMechanic());
    }
    IEnumerator PlayerColideHealthDecreaseMechanic()
    {
        var players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            players[i].gameObject.AddComponent<LevelItem>();
            yield return new WaitForSecondsRealtime(2f);
            var levelItem = players[i].GetComponent<LevelItem>();
            if (levelItem != null)
            {
                Debug.Log("Not null");
                levelItem.onCollisionEnterNew.AddListener(OnCollisionPlayerWithPlayer);
            }
        }
    }
    public void OnCollisionPlayerWithPlayer(Collision2D collision,GameObject _gameObject)
    {
        Debug.Log("Insiiiideeee 1 " + collision != null + "2 " + collision.gameObject.CompareTag("Player"));
        Debug.Log("Ahaayaa");
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            RPC_HealthText();
        }
    }
    [Rpc]
    public void RPC_HealthText()
    {
        healthSlider.value--;
        if (healthSlider.value == 0)
        {
            RPC_GameOver();
        }
    }
    [Rpc]
    public void RPC_GameOver()
    {
        GameManager.instance.isGameOver = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (!Object.HasStateAuthority)
        {
            playerElevator.transform.parent.position = Vector2.SmoothDamp(playerElevator.transform.parent.position, networkElevatorPos, ref veloc, 0.1f);
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            int playersOnBtn = (btnsList[0].activeInHierarchy?btn1:1) * (btnsList[1].activeInHierarchy ? btn2 : 1) * (btnsList[2].activeInHierarchy ? btn3 : 1 ) * (btnsList[3].activeInHierarchy ? btn4 : 1) * (btnsList[4].activeInHierarchy ? btn5 : 1) * (btnsList[5].activeInHierarchy ? btn6 : 1);
            if (playersOnBtn != 0)
            {
                if (!key.activeInHierarchy)
                {
                    RPC_SpawnKey();
                }
            }

            var colliders = new Collider2D[2];
            playerElevator.OverlapCollider(contactFilter, colliders);
            var collideds = colliders.ToList().FindAll(col => col != null);
            RPC_ElevatorText(collideds.Count);
            if (collideds.Count == 2)
            {
                playerElevator.transform.parent.position = Vector2.Lerp(playerElevator.transform.parent.position, new Vector3(playerElevator.transform.parent.position.x, 5), Time.deltaTime / 2);
                elapsedTime = 0;
            }
            else
            {
                if (elapsedTime > 1f)
                {
                    playerElevator.transform.parent.position = Vector2.Lerp(playerElevator.transform.parent.position, elevatorDefaultPos, Time.deltaTime * 1.3f);                    
                }
                else
                {
                    elapsedTime += Time.deltaTime;
                }
            }
            networkElevatorPos = playerElevator.transform.parent.position;
        }
    }
    [Rpc]
    public void RPC_SpawnKey()
    {
        key.SetActive(true);
    }
    [Rpc]
    public void RPC_ElevatorText(int collidesCount)
    {
        elevatorText.text = 2 - collidesCount + "";
    }
    public void SubscribeGlacier(LevelItem levelItem)
    {
        if (!Connector.instance.networkRunner.IsServer) return;

        levelItem.onCollisionEnterNew.RemoveAllListeners();
        levelItem.onCollisionStayNew.RemoveAllListeners();
        levelItem.onCollisionExitNew.RemoveAllListeners();
        levelItem.onCollisionStayNew.AddListener(OnCollisionStayGlacier);
        levelItem.onCollisionExitNew.AddListener(OnCollisionExitGlacier);
    }
    public void OnCollisionStayGlacier(Collision2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerController>().slidePlayer = true;
        }
    }
    public void OnCollisionExitGlacier(Collision2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerController>().slidePlayer = false;
        }
    }

    #region HardCoded
    public void SubscribeBtn1(LevelItem levelItem)
    {
        if (!Connector.instance.networkRunner.IsServer) return;

        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.RemoveAllListeners();
        levelItem.onTriggerExitNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.AddListener(OnCollisionStayBtn1);
        levelItem.onTriggerExitNew.AddListener(OnCollisionExitBtn1);
    }
    public void OnCollisionStayBtn1(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn1 = 1;
        }
    }
    public void OnCollisionExitBtn1(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn1 = 0;
        }
    }

    public void SubscribeBtn2(LevelItem levelItem)
    {
        if (!Connector.instance.networkRunner.IsServer) return;

        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.RemoveAllListeners();
        levelItem.onTriggerExitNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.AddListener(OnCollisionStayBtn2);
        levelItem.onTriggerExitNew.AddListener(OnCollisionExitBtn2);
    }
    public void OnCollisionStayBtn2(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn2 = 1;
        }
    }
    public void OnCollisionExitBtn2(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn2 = 0;
        }
    }

    public void SubscribeBtn3(LevelItem levelItem)
    {
        if (!Connector.instance.networkRunner.IsServer) return;

        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.RemoveAllListeners();
        levelItem.onTriggerExitNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.AddListener(OnCollisionStayBtn3);
        levelItem.onTriggerExitNew.AddListener(OnCollisionExitBtn3);
    }
    public void OnCollisionStayBtn3(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn3 = 1;
        }
    }
    public void OnCollisionExitBtn3(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn3 = 0;
        }
    }

    public void SubscribeBtn4(LevelItem levelItem)
    {
        if (!Connector.instance.networkRunner.IsServer) return;

        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.RemoveAllListeners();
        levelItem.onTriggerExitNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.AddListener(OnCollisionStayBtn4);
        levelItem.onTriggerExitNew.AddListener(OnCollisionExitBtn4);
    }
    public void OnCollisionStayBtn4(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn4 = 1;
        }
    }
    public void OnCollisionExitBtn4(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn4 = 0;
        }
    }

    public void SubscribeBtn5(LevelItem levelItem)
    {
        if (!Connector.instance.networkRunner.IsServer) return;

        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.RemoveAllListeners();
        levelItem.onTriggerExitNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.AddListener(OnCollisionStayBtn5);
        levelItem.onTriggerExitNew.AddListener(OnCollisionExitBtn5);
    }
    public void OnCollisionStayBtn5(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn5 = 1;
        }
    }
    public void OnCollisionExitBtn5(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn5 = 0;
        }
    }

    public void SubscribeBtn6(LevelItem levelItem)
    {
        if (!Connector.instance.networkRunner.IsServer) return;

        levelItem.onTriggerEnterNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.RemoveAllListeners();
        levelItem.onTriggerExitNew.RemoveAllListeners();
        levelItem.onTriggerStayNew.AddListener(OnCollisionStayBtn6);
        levelItem.onTriggerExitNew.AddListener(OnCollisionExitBtn6);
    }
    public void OnCollisionStayBtn6(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn6 = 1;
        }
    }
    public void OnCollisionExitBtn6(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            btn6 = 0;
        }
    }
    #endregion
}
