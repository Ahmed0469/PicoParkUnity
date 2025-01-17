using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Visyde;
using Fusion;

public class SixPlayersLevel2 : NetworkBehaviour
{
    public ContactFilter2D contactFilter;
    [Networked] public int timeRemaining { get; set; } = 5;
    public Text gameTimerText;

    private void Awake()
    {
        FindObjectOfType<UIManager>().gameTimerText.gameObject.SetActive(false);
    }
    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.instance.gameStarted);        
        gameTimerText.text = "00:" + timeRemaining.ToString("00");
        yield return new WaitForSecondsRealtime(1);
        Debug.Log("Time Remaining " + timeRemaining);
        if (Connector.instance.networkRunner.IsServer)
        {
            timeRemaining -= 1;            
        }
        gameTimerText.text = "00:" + timeRemaining.ToString("00");
        if (Connector.instance.networkRunner.IsServer)
        {
            if (timeRemaining <= 0)
            {
                GameManager.instance.isGameOver = true;
            }
        }
        if (timeRemaining > 0)
        {
            StartCoroutine(Start());
        }
    }
    public void Btn(GameObject btn)
    {
        if (!Connector.instance.networkRunner.IsServer)
        {
            return;
        }
        var colliders = new Collider2D[1];
        btn.GetComponent<BoxCollider2D>().OverlapCollider(contactFilter,colliders);
        if (colliders[0] != null)
        {
            btn.transform.GetChild(1).transform.localPosition = new Vector3(btn.transform.GetChild(1).transform.localPosition.x, 0.03f);
            timeRemaining += 3;
            btn.GetComponent<BoxCollider2D>().enabled = false;
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
                var playerController = colliders[i].GetComponent<PlayerController>();
                Vector2 veloc = playerController.rg.velocity;
                veloc.y = 30;
                playerController.rg.velocity = veloc;

                // Don't allow jumping right after a jump:
                playerController.allowJump = false;
            }            
        }
    }
}
