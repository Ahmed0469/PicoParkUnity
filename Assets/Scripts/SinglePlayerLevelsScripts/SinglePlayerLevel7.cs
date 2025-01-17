using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerLevel7 : MonoBehaviour
{
    public Transform wall;
    public Transform wallAfterPosUp;
    public Transform wallAfterPosDown;
    public LevelItem btn1;
    public LevelItem btn2;
    public LevelItem btn3;
    bool btnPressedUp = false;
    bool btnPressedDown = false;
    // Start is called before the first frame update
    void Start()
    {
        btn1.onTriggerEnterNew.AddListener(OnTriggerEnterBtn1);
        btn2.onTriggerEnterNew.AddListener(OnTriggerEnterBtn2);
        btn3.onTriggerEnterNew.AddListener(OnTriggerEnterBtn1);
    }

    // Update is called once per frame
    void Update()
    {
        if (btnPressedUp)
        {
            wall.transform.position = Vector3.Lerp(wall.position, wallAfterPosUp.position, Time.deltaTime * 2);
        }
        if (btnPressedDown)
        {
            wall.transform.position = Vector3.Lerp(wall.position, wallAfterPosDown.position, Time.deltaTime * 2);
        }
    }
    public void OnTriggerEnterBtn1(Collider2D collision, GameObject _gameobject)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            btnPressedUp = true;
            btnPressedDown = false;
        }
    }
    public void OnTriggerEnterBtn2(Collider2D collision, GameObject _gameobject)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            btnPressedDown = true;
            btnPressedUp = false;
        }
    }
}
