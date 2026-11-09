using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private Transform hearts, score, endPanel;

    int hpValue = 2;
    int scoreValue = 0;
    private bool isGameOver=false;

    public bool IsGameOver { get => isGameOver;}

    public void SetGameOver(bool value)
    {
        isGameOver = value;
    }
    private void Start()
    {
        //对于unity自带的组件，赋值的时候通过获取游戏物体再获取组件的方式完成
        //查找游戏物体的方式:
        //1、通过transform.Find(物体的名字) 利用父物体子物体层级查找
        //2、通过GameObject.Find(物体的名字) 查找整个项目中被激活的物体名字
        //3、利用层级的索引查找 GetChild(索引的序号 从0开始)
        //返回值是transform
        hearts = transform.Find("HpImg");
        score = transform.Find("ScoreText/score");
        endPanel = transform.Find("endPanel");

        endPanel.GetChild(1).GetComponent<Button>().onClick.AddListener(RestartBtn);
        endPanel.GetChild(2).GetComponent<Button>().onClick.AddListener(Exit);
        //重复延迟执行的协程
        //要执行的委托，多久开始执行，频率
        InvokeRepeating(nameof(SpawnAnimal), 2, 1);
    }
    //重新开始按钮
    private void RestartBtn()
    {
        SceneManager.LoadScene(0);
    }
    //点击退出按钮
    private void Exit()
    {
        Application.Quit();//pc平台退出
        //UnityEditor.EditorApplication.isPlaying=false;//编辑模式退出，打包需要注释
    }
    private void SpawnAnimal()
    {
        int index = UnityEngine.Random.Range(0, 3);
        //动物生成的随机点位
        Vector3 startPos = new Vector3(UnityEngine.Random.Range(-15f, 15f), 0, UnityEngine.Random.Range(23f, 33f));
        //种类随机生成
        GameObject Animal = null;
        switch (index)
        {
            case 0:
                Animal = GameFacade.instance.LoadAnimal("Chick_1");
                break;
            case 1:
                Animal = GameFacade.instance.LoadAnimal("Deer_1");
                break;
            case 2:
                Animal = GameFacade.instance.LoadAnimal("Dog_1");
                break;
        }
        Animal.transform.position = startPos;

    }
    public void TakeDamage()
    {
        //隐藏游戏物体，使用gameObject
        hearts.GetChild(hpValue).gameObject.SetActive(false);
        hpValue--;
        //判断游戏是否结束
        if (hpValue < 0)
        {
            Debug.Log("游戏失败");
            GameEndHandle("失败(defeat)", "haha");
        }
    }
    //得分函数
    public void GetScore()
    {
        scoreValue++;
        score.GetComponent<Text>().text= scoreValue.ToString();
        if(scoreValue >= 20)
        {
            GameEndHandle("胜利(victory)", "hachimi live family laughter");
        }
    }
    //提取游戏结束后要处理的逻辑
    private void GameEndHandle(string imgName, string soundName)
    {
        //显示结算界面
        endPanel.gameObject.SetActive(true);
        //游戏加载失败的图标
        endPanel.GetChild(0).GetComponent<Image>().sprite =
        GameFacade.instance.LoadImg(imgName);
        //播放失败音效
        GameFacade.instance.PlaySound(soundName);
        //暂停游戏功能.
        isGameOver = true;
        CancelInvoke();
    }

}
