using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从 Resources 目录加载资源。
/// 这里加入“资源缓存”，同一个预制体只 Resources.Load 一次，
/// 之后每次生成动物/食物时只做 Instantiate，减少重复 IO。
/// </summary>
public class ResourcesAssetFactory : IAssetFactory
{
    protected const string animalPath = "Animals/";
    protected const string foodPath = "Food/";
    protected const string soundPath = "Sound/";
    protected const string imgPath = "Sprite/";

    private readonly Dictionary<string, GameObject> animalAssets = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> foodAssets = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, AudioClip> soundAssets = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, Sprite> imageAssets = new Dictionary<string, Sprite>();

    public GameObject LoadAnimal(string name)
    {
        return InstantiateCached(animalPath + name, animalAssets);
    }

    public GameObject LoadFood(string name)
    {
        return InstantiateCached(foodPath + name, foodAssets);
    }

    public AudioClip LoadSound(string name)
    {
        return LoadCachedAsset(soundPath + name, soundAssets);
    }

    public Sprite LoadImg(string name)
    {
        return LoadCachedAsset(imgPath + name, imageAssets);
    }

    // 给代理工厂使用：只返回预制体资源，不实例化。
    public GameObject LoadAnimalAsset(string name)
    {
        return LoadCachedAsset(animalPath + name, animalAssets);
    }

    public GameObject LoadFoodAsset(string name)
    {
        return LoadCachedAsset(foodPath + name, foodAssets);
    }

    private GameObject InstantiateCached(string path, Dictionary<string, GameObject> cache)
    {
        GameObject asset = LoadCachedAsset(path, cache);
        return asset != null ? Object.Instantiate(asset) : null;
    }

    private T LoadCachedAsset<T>(string path, Dictionary<string, T> cache) where T : Object
    {
        if (cache.TryGetValue(path, out T cached) && cached != null)
        {
            return cached;
        }

        T asset = Resources.Load<T>(path);
        if (asset == null)
        {
            Debug.LogError("[Asset] 无法加载资源，路径：" + path);
            return null;
        }

        cache[path] = asset;
        return asset;
    }
}
