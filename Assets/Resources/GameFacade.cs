using UnityEngine;

/// <summary>
/// 游戏门面（Facade）/单例。
/// 对玩家、动物等对象隐藏资源加载、音效、UI 等子系统的细节，
/// 同时集中提供“当前是否还能操作”的全局状态。
/// </summary>
public class GameFacade : MonoBehaviour
{
    public static GameFacade instance;
    public static GameFacade Instance => instance;

    public Soundcontroller soundController;

    private IAssetFactory assetFactory;
    private GameController gameController;
    private LevelManager levelManager;

    public GameController GameController => gameController;
    public LevelManager LevelManager => levelManager;

    /// <summary>只有关卡进行中、且没有结算时才允许移动/射击/动物前进。</summary>
    public bool IsGameplayActive
    {
        get
        {
            return gameController != null
                && !gameController.IsGameOver
                && levelManager != null
                && levelManager.IsLevelActive;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this && instance.gameObject != null)
        {
            Destroy(instance.gameObject);
        }

        instance = this;
        assetFactory = new ProxyAssetFactory();

        if (soundController == null)
        {
            soundController = GetComponent<Soundcontroller>();
        }

        gameController = FindObjectOfType<GameController>();
    }

    public void RegisterLevelManager(LevelManager manager)
    {
        levelManager = manager;
    }

    public GameObject LoadAnimal(string name)
    {
        return assetFactory.LoadAnimal(name);
    }

    public GameObject LoadFood(string name)
    {
        return assetFactory.LoadFood(name);
    }

    public AudioClip LoadSound(string name)
    {
        return assetFactory.LoadSound(name);
    }

    public Sprite LoadImg(string name)
    {
        return assetFactory.LoadImg(name);
    }

    public void PlaySound(string name, float volume = 1f)
    {
        if (soundController == null)
        {
            soundController = GetComponent<Soundcontroller>();
        }

        if (soundController == null) return;

        AudioClip clip = assetFactory.LoadSound(name);
        if (clip != null)
        {
            soundController.PlaySound(clip, volume);
        }
    }

    /// <summary>兼容旧脚本：让动物/玩家受一次伤害。</summary>
    public void TakeDamage()
    {
        PlayerTakeDamage(1);
    }

    public bool PlayerTakeDamage(int damage)
    {
        if (gameController == null) return false;
        return gameController.PlayerTakeDamage(damage);
    }

    /// <summary>兼容旧脚本：加 1 分。</summary>
    public void GetScore()
    {
        if (gameController != null) gameController.AddScore(1);
    }

    public bool GetIsGameOver()
    {
        return gameController != null && gameController.IsGameOver;
    }
}
