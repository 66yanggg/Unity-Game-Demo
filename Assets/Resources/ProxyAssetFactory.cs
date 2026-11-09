using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 代理工厂：为资源加载增加缓存层。
/// 注意这里缓存的是“预制体资源”，不是场景中的实例；
/// 每次取用时再 Instantiate，避免把缓存实例误当成模板。
/// </summary>
public class ProxyAssetFactory : IAssetFactory
{
    private readonly ResourcesAssetFactory resources = new ResourcesAssetFactory();

    private readonly Dictionary<string, GameObject> animalDict = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> foodDict = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, Sprite> imageDict = new Dictionary<string, Sprite>();

    public GameObject LoadAnimal(string name)
    {
        if (!animalDict.TryGetValue(name, out GameObject prefab) || prefab == null)
        {
            prefab = resources.LoadAnimalAsset(name);
            if (prefab == null) return null;
            animalDict[name] = prefab;
        }

        return Object.Instantiate(prefab);
    }

    public GameObject LoadFood(string name)
    {
        if (!foodDict.TryGetValue(name, out GameObject prefab) || prefab == null)
        {
            prefab = resources.LoadFoodAsset(name);
            if (prefab == null) return null;
            foodDict[name] = prefab;
        }

        return Object.Instantiate(prefab);
    }

    public AudioClip LoadSound(string name)
    {
        if (soundDict.TryGetValue(name, out AudioClip clip) && clip != null)
        {
            return clip;
        }

        clip = resources.LoadSound(name);
        if (clip != null) soundDict[name] = clip;
        return clip;
    }

    public Sprite LoadImg(string name)
    {
        if (imageDict.TryGetValue(name, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        sprite = resources.LoadImg(name);
        if (sprite != null) imageDict[name] = sprite;
        return sprite;
    }
}
