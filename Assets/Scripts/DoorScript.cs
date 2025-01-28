using UnityEngine;
using Visyde;
using Fusion;
using System.Collections;

public class DoorScript : NetworkBehaviour
{
    public Sprite doorOpenSprite;
    bool isUnlocked = false;
    int enteredPlayers = 0;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.gameMode == Visyde.GameMode.Multiplayer)
        {
            if (!Object.Runner.IsServer)
            {
                return;
            }
            collision.gameObject.TryGetComponent<PlayerController>(out var collided);
            if (collided != null)
            {
                if (collision.gameObject.GetComponent<PlayerController>().hasDoorKey)
                {
                    isUnlocked = true;
                    RPC_DestroyKey();
                    //collision.gameObject.GetComponent<PlayerController>().keyObj.SetActive(false);
                }
                if (isUnlocked)
                {
                    RPC_OpenDoor();
                    collision.gameObject.GetComponent<PlayerController>().Die();
                    enteredPlayers++;
                    if (GameManager.gameMode == Visyde.GameMode.Multiplayer)
                    {
                        if (enteredPlayers == Connector.instance.networkRunner.SessionInfo.PlayerCount)
                        {
                            RPC_LevelComplete();
                        }
                    }
                    else
                    {
                        if (enteredPlayers == 2)
                        {
                            if (!FindObjectOfType<UIManager>().gameOverPanel.activeInHierarchy)
                            {
                                GameManager.instance.LevelWinScreen.SetActive(true);
                                SoundManager.instance.PlayOneShot(SoundManager.instance.winSFX);
                                if (Connector.instance.networkRunner.IsServer)
                                {
                                    StartCoroutine(NextLevelBtnDelay());
                                }
                                else
                                {
                                    GameManager.instance.nextLevelBtn.interactable = Connector.instance.networkRunner.IsServer;
                                }
                            }

                        }
                    }
                }
            }            
        }
        else
        {
            collision.gameObject.TryGetComponent<PlayerController>(out var collided);
            if (collided != null)
            {
                if (collision.gameObject.GetComponent<PlayerController>().hasDoorKey)
                {
                    isUnlocked = true;
                    DestroyKey();
                    //collision.gameObject.GetComponent<PlayerController>().keyObj.SetActive(false);
                }
                if (isUnlocked)
                {
                    OpenDoor();
                    Destroy(collision.gameObject);
                    enteredPlayers++;
                    if (enteredPlayers == 2)
                    {
                        if (!FindObjectOfType<UIManager>().gameOverPanel.activeInHierarchy)
                        {
                            GameManager.instance.LevelWinScreen.SetActive(true);
                            //AdsManager.instance.ShowInterstitialAd();
                            SoundManager.instance.PlayOneShot(SoundManager.instance.winSFX);
                            GameManager.instance.nextLevelBtn.interactable = true;
                        }

                    }
                }
            }
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (GameManager.gameMode == Visyde.GameMode.Multiplayer)
        {
            if (!Object.Runner.IsServer)
            {
                return;
            }
            collision.gameObject.TryGetComponent<PlayerController>(out var collided);
            if (collided != null)
            {
                if (collision.gameObject.GetComponent<PlayerController>().hasDoorKey)
                {
                    isUnlocked = true;
                    RPC_DestroyKey();
                    //collision.gameObject.GetComponent<PlayerController>().keyObj.SetActive(false);
                }
                if (isUnlocked)
                {
                    RPC_OpenDoor();
                    collision.gameObject.GetComponent<PlayerController>().Die();
                    enteredPlayers++;
                    if (GameManager.gameMode == Visyde.GameMode.Multiplayer)
                    {
                        if (enteredPlayers == Connector.instance.networkRunner.SessionInfo.PlayerCount)
                        {
                            RPC_LevelComplete();
                        }
                    }
                    else
                    {
                        if (enteredPlayers == 2)
                        {
                            if (!FindObjectOfType<UIManager>().gameOverPanel.activeInHierarchy)
                            {
                                GameManager.instance.LevelWinScreen.SetActive(true);
                                SoundManager.instance.PlayOneShot(SoundManager.instance.winSFX);
                                if (Connector.instance.networkRunner.IsServer)
                                {
                                    StartCoroutine(NextLevelBtnDelay());
                                }
                                else
                                {
                                    GameManager.instance.nextLevelBtn.interactable = Connector.instance.networkRunner.IsServer;
                                }
                            }

                        }
                    }
                }
            }
        }
        else
        {
            collision.gameObject.TryGetComponent<PlayerController>(out var collided);
            if (collided != null)
            {
                if (collision.gameObject.GetComponent<PlayerController>().hasDoorKey)
                {
                    isUnlocked = true;
                    DestroyKey();
                    //collision.gameObject.GetComponent<PlayerController>().keyObj.SetActive(false);
                }
                if (isUnlocked)
                {
                    OpenDoor();
                    collision.gameObject.GetComponent<PlayerController>().Die();
                    enteredPlayers++;
                    if (enteredPlayers == 2)
                    {
                        if (!FindObjectOfType<UIManager>().gameOverPanel.activeInHierarchy)
                        {
                            GameManager.instance.LevelWinScreen.SetActive(true);
                            //AdsManager.instance.ShowInterstitialAd();
                            SoundManager.instance.PlayOneShot(SoundManager.instance.winSFX);
                            GameManager.instance.nextLevelBtn.interactable = true;
                        }

                    }
                }
            }
        }
    }
    IEnumerator NextLevelBtnDelay()
    {
        yield return new WaitForSecondsRealtime(1);
        GameManager.instance.nextLevelBtn.interactable = true;
    }
    [Rpc]
    public void RPC_LevelComplete()
    {
        if (!FindObjectOfType<UIManager>().gameOverPanel.activeInHierarchy)
        {
            GameManager.instance.LevelWinScreen.SetActive(true);
            SoundManager.instance.PlayOneShot(SoundManager.instance.winSFX);
            if (Connector.instance.networkRunner.IsServer)
            {
                StartCoroutine(NextLevelBtnDelay());
            }
            else
            {
                GameManager.instance.nextLevelBtn.interactable = Connector.instance.networkRunner.IsServer;
            }
        }
    }
    [Rpc]
    public void RPC_OpenDoor()
    {
        GetComponentInChildren<SpriteRenderer>().sprite = doorOpenSprite;
    }
    [Rpc]
    public void RPC_DestroyKey()
    {
        FindObjectOfType<KeyScript>().gameObject.SetActive(false);
    }
    public void LevelComplete()
    {
        if (!FindObjectOfType<UIManager>().gameOverPanel.activeInHierarchy)
        {
            GameManager.instance.LevelWinScreen.SetActive(true);
            SoundManager.instance.PlayOneShot(SoundManager.instance.winSFX);
            GameManager.instance.nextLevelBtn.interactable = true;
        }
    }    
    public void OpenDoor()
    {
        GetComponentInChildren<SpriteRenderer>().sprite = doorOpenSprite;
    }
    public void DestroyKey()
    {
        FindObjectOfType<KeyScript>().gameObject.SetActive(false);
    }
}
