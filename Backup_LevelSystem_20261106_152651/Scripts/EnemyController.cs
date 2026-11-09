using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField]
    private float speed = 10;
    private void Update()
    {
        //判断游戏结束了
        if (GameFacade.instance.GetIsGameOver()) return;
        transform.Translate(Vector3.forward*speed*Time.deltaTime);
        if (transform.position.z <= -10)
        {
            Destroy(gameObject);
        }
    }
    //碰撞检查
    //俩个物体同时具有碰撞组件，并且至少有一个具有刚体
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Equals("player"))
        {
            //扣血效果
            GameFacade.instance.TakeDamage();
            //播放受伤音效
            GameFacade.instance.PlaySound("hachimi aminuos");
            //销毁动物
            Destroy (gameObject);
        }
        if (collision.gameObject.CompareTag("Food"))
        {
            //增加得分
            GameFacade.instance.GetScore();
            //播放动物死亡音效
            GameFacade.instance.PlaySound("hachimi oh yeahhhh");
            //销毁动物和食物
            Destroy(collision.gameObject )  ;
            Destroy(gameObject );
        }
    }
}
