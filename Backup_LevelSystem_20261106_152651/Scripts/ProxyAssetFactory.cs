using System.Collections.Generic;
using UnityEngine;
//代理模式 ，代理工厂，对资源加载的优化
public class ProxyAssetFactory : IAssetFactory
{
    //原工厂的对象引用
    private ResourcesAssetFactory resources = new ResourcesAssetFactory();
    //创建字典类型的数据存储资源 键值对机制
    private Dictionary<string, GameObject> animalDict = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> foodDict = new Dictionary<string, GameObject>();
    private Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, Sprite> imgDict = new Dictionary<string, Sprite>();

    public GameObject LoadAnimal(string name)
    {
        if (animalDict.ContainsKey(name))
        {
            return GameObject.Instantiate(animalDict[name]);
        }
        else
        {
            GameObject asset = resources.LoadAnimal(name);
            animalDict.Add(name, asset);
            return GameObject.Instantiate(asset);
        }
    }

    public GameObject LoadFood(string name)
    {
        if (foodDict.ContainsKey(name))
        {
            return GameObject.Instantiate(foodDict[name]);
        }
        else
        {
            GameObject asset = resources.LoadFood(name);
            foodDict.Add(name, asset);
            return GameObject.Instantiate(asset);
        }
    }

    public AudioClip LoadSound(string name)
    {
        if (soundDict.ContainsKey(name))
        {
            return soundDict[name];
        }
        else
        {
            AudioClip clip = resources.LoadSound(name);
            soundDict.Add(name, clip);
            return clip;
        }
    }

    public Sprite LoadImg(string name)
    {
        if (imgDict.ContainsKey(name))
        {
            return imgDict[name];
        }
        else
        {
            Sprite sp = resources.LoadImg(name);
            imgDict.Add(name, sp);
            return sp;
        }
    }

}
