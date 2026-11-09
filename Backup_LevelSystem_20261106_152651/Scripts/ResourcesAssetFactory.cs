using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesAssetFactory : IAssetFactory
{
    protected const string animalPath = "Animals/";
    protected const string foodPath = "Food/";
    protected const string soundPath = "Sound/";
    protected const string imgPath = "Sprite/";
    public GameObject LoadAnimal(string name)
    {
        return InstantiateGameObject(animalPath + name);
    }
    public GameObject LoadFood(string name)
    {
        return InstantiateGameObject(foodPath + name);
    }
    public Sprite LoadImg(string name)
    {
        return Resources.Load<Sprite>(imgPath + name);
    }
    public AudioClip LoadSound(string name)
    {
        return Resources.Load<AudioClip>(soundPath + name);
    }
    public GameObject InstantiateGameObject(string path)
    {
        Object o=Resources.Load(path);
        if (o == null)
        {
            Debug.LogError("无法加载资源，路径：" + path);
            return null;
        }
        return GameObject.Instantiate(o) as GameObject;
    }

}
