using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerLevel8 : MonoBehaviour
{
    public LevelItem btn1;
    public LevelItem btn2;
    public Transform wall1;
    public Transform wall1AfterPos;
    public Transform wall2;
    public Transform wall2AfterPos;
    public Transform wall3;
    public Transform wall3AfterPos;
    bool wall1Move = false;
    bool wall2and3Move = false;
    // Start is called before the first frame update
    void Start()
    {
        btn1.onTriggerEnterNew.AddListener(OnTriggerEnterBtn1);
        btn2.onTriggerEnterNew.AddListener(OnTriggerEnterBtn2);
    }

    // Update is called once per frame
    void Update()
    {
        if (wall1Move)
        {
            wall1.transform.position = Vector3.Lerp(wall1.transform.position, wall1AfterPos.position, Time.deltaTime * 2);
        }
        if (wall2and3Move)
        {
            wall2.transform.position = Vector3.Lerp(wall2.transform.position, wall2AfterPos.position, Time.deltaTime * 2);
            wall2.transform.localScale = Vector3.Lerp(wall2.transform.localScale, wall2AfterPos.localScale, Time.deltaTime * 2);
            wall3.transform.position = Vector3.Lerp(wall3.transform.position, wall3AfterPos.position, Time.deltaTime * 2);
            wall3.transform.localScale = Vector3.Lerp(wall3.transform.localScale, wall3AfterPos.localScale, Time.deltaTime * 2);
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
            wall2and3Move = true;
        }
    }
}
