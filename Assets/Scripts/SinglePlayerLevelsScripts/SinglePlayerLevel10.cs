using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerLevel10 : MonoBehaviour
{
    public LevelItem btn1;
    public Transform wall1;
    public Transform wall1AfterPos;
    public LevelItem btn2;
    public Transform wall2;
    public Transform wall2AfterPos;
    public LevelItem btn3;
    public Transform wall3;
    public Transform wall3AfterPos;
    public LevelItem btn4Down;
    public Transform wall4;
    public Transform wall4AfterPosDown;
    public Transform wall4AfterPosUp;

    bool wall1Move = false;
    bool wall2Move = false;
    bool wall3Move = false;
    bool wall4Move = false;

    Vector3 wall1DefaultPos;
    Vector3 wall2DefaultPos;
    Vector3 wall3DefaultPos;

    float elapsedTime1;
    float elapsedTime2;
    float elapsedTime3;
    float elapsedTime4;
    // Start is called before the first frame update
    void Start()
    {
        btn1.onTriggerEnterNew.AddListener(OnTriggerEnterBtn1);
        btn2.onTriggerEnterNew.AddListener(OnTriggerEnterBtn2);
        btn3.onTriggerEnterNew.AddListener(OnTriggerEnterBtn3);
        btn4Down.onTriggerEnterNew.AddListener(OnTriggerEnterBtn4);

        wall1DefaultPos = wall1.transform.position;
        wall2DefaultPos = wall2.transform.position;
        wall3DefaultPos = wall3.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (wall1Move)
        {
            if (elapsedTime1 <= 2)
            {
                wall1.position = Vector3.Lerp(wall1.position, wall1AfterPos.position, Time.deltaTime * 2);
                elapsedTime1 += Time.deltaTime;
            }
            else if (elapsedTime1 <= 4)
            {
                wall1.position = Vector3.Lerp(wall1.position, wall1DefaultPos, Time.deltaTime * 2);
                elapsedTime1 += Time.deltaTime;
            }
            else
            {
                elapsedTime1 = 0;
            }
        }
        if (wall2Move)
        {
            if (elapsedTime2 <= 2)
            {
                wall2.position = Vector3.Lerp(wall2.position, wall2AfterPos.position, Time.deltaTime * 2);
                elapsedTime2 += Time.deltaTime;
            }
            else if (elapsedTime2 <= 4)
            {
                wall2.position = Vector3.Lerp(wall2.position, wall2DefaultPos, Time.deltaTime * 2);
                elapsedTime2 += Time.deltaTime;
            }
            else
            {
                elapsedTime2 = 0;
            }
        }
        if (wall3Move)
        {
            if (elapsedTime3 <= 2)
            {
                wall3.position = Vector3.Lerp(wall3.position, wall3AfterPos.position, Time.deltaTime * 2);
                elapsedTime3 += Time.deltaTime;
            }
            else if (elapsedTime3 <= 4)
            {
                wall3.position = Vector3.Lerp(wall3.position, wall3DefaultPos, Time.deltaTime * 2);
                elapsedTime3 += Time.deltaTime;
            }
            else
            {
                elapsedTime3 = 0;
            }
        }
        if (wall4Move)
        {
            if (elapsedTime4 <= 2)
            {
                wall4.position = Vector3.Lerp(wall4.position, wall4AfterPosDown.position, Time.deltaTime * 2);
                elapsedTime4 += Time.deltaTime;
            }
            else if (elapsedTime4 <= 4)
            {
                wall4.position = Vector3.Lerp(wall4.position, wall4AfterPosUp.position, Time.deltaTime * 2);
                elapsedTime4 += Time.deltaTime;
            }
            else
            {
                elapsedTime4 = 0;
            }
        }
    }
    public void OnTriggerEnterBtn1(Collider2D collision,GameObject _gameObject)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            wall1Move = true;
        }
    }
    public void OnTriggerEnterBtn2(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            wall2Move = true;
        }
    }
    public void OnTriggerEnterBtn3(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            wall3Move = true;
        }
    }
    public void OnTriggerEnterBtn4(Collider2D collision, GameObject _gameObject)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            wall4Move = true;
        }
    }
}
