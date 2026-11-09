using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//门面模式 单例模式，媒介模式
public class GameFacade : MonoBehaviour
{
    //单例模式声明
    public static GameFacade instance;
    //声明子系统对象的引用
    private IAssetFactory assetFactory;
    public Soundcontroller soundController;
    private GameController gameController;
    private void Awake()
    {
        instance = this;
        //引用类型变量赋值 用new操作符
        assetFactory = new ResourcesAssetFactory();
        //继承mono behaviour基类的类 ，赋值的时候通过获取游戏物体在获得组件的方式完成
        //获取组件方式
        //1.游戏物体自身通过gameObject直接获得
        //2.其他所所有组件通过GetComponent<组件名>（）获得
        soundController = GetComponent<Soundcontroller>();
        gameController = GameObject.Find("Canvas").GetComponent<GameController>();
    }

    //加载食物
    public GameObject LoadFood(string name)
    {
        return assetFactory.LoadFood(name);
    }
    //播放音效的函数
    public void PlaySound(string name)
    {
        soundController.PlaySound(assetFactory.LoadSound(name));
    }
    //加载图片的函数
    public Sprite LoadImg(string name)
    {
        return assetFactory.LoadImg(name);
    }
    //玩家扣血函数
    public void TakeDamage()
    {
        gameController.TakeDamage();
    }
    //增加得分函数
    public void GetScore()
    {
        gameController.GetScore();
    }
    //加载动物函数
    public GameObject LoadAnimal(string name)
    {
        return assetFactory.LoadAnimal(name);
    }
    //获取游戏是否结束
    public bool GetIsGameOver()
    {
        return gameController.IsGameOver;
    }
}