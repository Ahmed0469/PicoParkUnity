using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerLevel9 : MonoBehaviour
{
    public LevelItem btn;
    bool wallMoveStart = false;
    public Transform wall;
    public Transform wallAfterPosUp;
    public Transform wallAfterPosDown;
    float elapsedTime = 0;
    // Start is called before the first frame update
    void Start()
    {
        btn.onTriggerEnterNew.AddListener(OnTriggerEnterBtn);
    }

    // Update is called once per frame
    void Update()
    {
        if (wallMoveStart)
        {
            if (elapsedTime <= 2)
            {
                wall.transform.position = Vector3.Lerp(wall.position, wallAfterPosUp.position, Time.deltaTime * 2);
                elapsedTime += Time.deltaTime;
            }
            else if(elapsedTime <= 4)
            {
                wall.transform.position = Vector3.Lerp(wall.position, wallAfterPosDown.position, Time.deltaTime * 2);
                elapsedTime += Time.deltaTime;
            }
            else
            {
                elapsedTime = 0;
            }
        }
    }
    public void OnTriggerEnterBtn(Collider2D collision, GameObject _gameObject)
    {
        wallMoveStart = true;
    }
}
