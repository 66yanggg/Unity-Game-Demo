using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Playercontroller : MonoBehaviour
{
    [SerializeField]
    private float speed = 10;
    [SerializeField]
    private float XRange = 15;
    private void Update()
    {
        //判断游戏结束了
        if (GameFacade.instance.GetIsGameOver()) return;
        //单位向量（1，0，0）
        transform.Translate(Vector3.right*Input.GetAxis("Horizontal")*speed*Time.deltaTime);
        if(transform.position.x>XRange)
            transform.position=new Vector3(XRange,0,0);
        if(transform.position.x<-XRange)
            transform.position=new Vector3(-XRange,0,0);

        //发射食物
        if(Input.GetKeyDown(KeyCode.Space))
            {
            //生成食物模型
            GameObject go = GameFacade.instance.LoadFood("Sphere");
            //设置生成后的位置
            go.transform.position= transform.position
            +new Vector3(0, 0.7f, 0);
            //设置销毁时间
            Destroy(go,5);
            //播放攻击音效
            GameFacade.instance.PlaySound("hachimi vox");
        }
    }
    
}
