using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerLevel1 : MonoBehaviour
{
    public Transform wall;
    public Transform wallAfterPos;
    public LevelItem btn;
    bool btnPressed = false;
    // Start is called before the first frame update
    void Start()
    {
        btn.onTriggerEnterNew.AddListener(OnTriggerEnterBtn);
    }

    // Update is called once per frame
    void Update()
    {
        if (btnPressed)
        {
            wall.transform.position = Vector3.Lerp(wall.position, wallAfterPos.position, Time.deltaTime * 2);
            wall.transform.localScale = Vector3.Lerp(wall.localScale, wallAfterPos.localScale, Time.deltaTime * 2);
        }
    }
    public void OnTriggerEnterBtn(Collider2D collision,GameObject _gameobject)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            btnPressed = true;
        }
    }
}
