using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelDefinition
{
    public string displayName = "新关卡";
    [Min(1)] public int targetKills = 8;
    [Min(0.15f)] public float spawnInterval = 1.5f;
    [Min(0f)] public float firstSpawnDelay = 1.2f;
    [Min(0.1f)] public float speedMultiplier = 0.6f;
    [Min(1)] public int minHp = 1;
    [Min(1)] public int maxHp = 1;
    [Min(1)] public int maxAlive = 6;
    [Min(0)] public int chickWeight = 100;
    [Min(0)] public int deerWeight = 0;
    [Min(0)] public int dogWeight = 0;
}

/// <summary>
/// 五关关卡系统：负责出怪节奏、动物血量/速度倍率、击杀目标和过关流程。
/// 数值从第 1 关到第 5 关逐步提高。
/// </summary>
public class LevelManager : MonoBehaviour
{
    public LevelDefinition[] levels = CreateDefaultLevels();

    private GameController game;
    private readonly List<AnimalHealth> alive = new List<AnimalHealth>();
    private Coroutine spawnRoutine;
    private Coroutine completeRoutine;

    private bool levelActive;
    private bool playerDefeated;
    private int currentLevelIndex;
    private int kills;
    private int targetKills;
    private int scoreAtLevelStart;

    public bool IsLevelActive => levelActive;
    public int CurrentLevelIndex => currentLevelIndex;
    public int TotalLevels => levels != null ? levels.Length : 0;

    public void Initialize(GameController controller)
    {
        game = controller;
    }

    public void StartGame()
    {
        if (game == null) return;

        StopAllCoroutines();
        levelActive = false;
        CleanupScene();

        currentLevelIndex = 0;
        scoreAtLevelStart = 0;
        game.SetScore(0);
        StartLevel(0);
    }

    public void StartLevel(int index)
    {
        if (game == null || levels == null || levels.Length == 0) return;

        index = Mathf.Clamp(index, 0, levels.Length - 1);

        StopAllCoroutines();
        levelActive = false;
        CleanupScene();

        currentLevelIndex = index;
        kills = 0;
        targetKills = Mathf.Max(1, levels[index].targetKills);
        scoreAtLevelStart = game.Score;
        playerDefeated = false;
        levelActive = true;

        game.PrepareLevel(index, levels.Length, kills, targetKills, levels[index].displayName);
        spawnRoutine = StartCoroutine(SpawnRoutine(levels[index]));
    }

    public void NextLevel()
    {
        if (game == null) return;

        if (currentLevelIndex + 1 < TotalLevels)
        {
            StartLevel(currentLevelIndex + 1);
        }
        else
        {
            StartGame();
        }
    }

    public void RestartCurrentLevel()
    {
        if (game == null) return;

        game.SetScore(scoreAtLevelStart);
        StartLevel(currentLevelIndex);
    }

    public void RestartGame()
    {
        StartGame();
    }

    public void OnPlayerDefeated()
    {
        if (game == null || playerDefeated) return;

        playerDefeated = true;
        levelActive = false;
        StopAllCoroutines();
        CleanupScene();
        game.ShowGameOver(currentLevelIndex, TotalLevels);
    }

    private IEnumerator SpawnRoutine(LevelDefinition definition)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, definition.firstSpawnDelay));

        while (levelActive && kills < targetKills)
        {
            if (alive.Count >= definition.maxAlive)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            SpawnAnimal(definition);
            yield return new WaitForSeconds(Mathf.Max(0.15f, definition.spawnInterval));
        }

        spawnRoutine = null;
    }

    private void SpawnAnimal(LevelDefinition definition)
    {
        if (GameFacade.Instance == null) return;

        alive.RemoveAll(animal => animal == null);
        if (alive.Count >= definition.maxAlive) return;

        string animalName = PickAnimal(definition);
        GameObject animal = GameFacade.Instance.LoadAnimal(animalName);
        if (animal == null)
        {
            Debug.LogError("[Level] 生成动物失败：" + animalName);
            return;
        }

        Vector3 startPosition = new Vector3(
            UnityEngine.Random.Range(-15f, 15f),
            0f,
            UnityEngine.Random.Range(24f, 32f));
        animal.transform.position = startPosition;

        EnemyController enemy = animal.GetComponent<EnemyController>();
        if (enemy == null)
        {
            enemy = animal.AddComponent<EnemyController>();
        }

        AnimalHealth health = animal.GetComponent<AnimalHealth>();
        if (health == null)
        {
            health = animal.AddComponent<AnimalHealth>();
        }

        int hp = UnityEngine.Random.Range(definition.minHp, definition.maxHp + 1);
        int score = 10 + (hp - 1) * 5;

        enemy.Configure(definition.speedMultiplier, health, hp, score);
        health.Removed += OnAnimalRemoved;
        alive.Add(health);
    }

    private string PickAnimal(LevelDefinition definition)
    {
        int total = definition.chickWeight + definition.deerWeight + definition.dogWeight;
        if (total <= 0) return "Chick_1";

        int roll = UnityEngine.Random.Range(0, total);
        if (roll < definition.chickWeight) return "Chick_1";

        roll -= definition.chickWeight;
        if (roll < definition.deerWeight) return "Deer_1";

        return "Dog_1";
    }

    private void OnAnimalRemoved(AnimalHealth animal, bool killed)
    {
        if (animal != null)
        {
            animal.Removed -= OnAnimalRemoved;
            alive.Remove(animal);
        }

        if (!levelActive || !killed) return;

        kills++;
        int points = animal != null ? animal.ScoreValue : 10;
        game.AddScore(points);
        game.UpdateKillProgress(kills, targetKills);

        if (kills >= targetKills)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        if (!levelActive) return;

        levelActive = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        // 过关奖励，让玩家有持续累积的分数目标。
        game.AddScore(50);
        completeRoutine = StartCoroutine(CompleteRoutine());
    }

    private IEnumerator CompleteRoutine()
    {
        yield return new WaitForSeconds(0.45f);

        CleanupScene();

        game.ShowLevelClear(
            currentLevelIndex,
            TotalLevels,
            currentLevelIndex >= TotalLevels - 1);
    }

    private void CleanupScene()
    {
        for (int i = 0; i < alive.Count; i++)
        {
            if (alive[i] != null)
            {
                alive[i].Removed -= OnAnimalRemoved;
            }
        }
        alive.Clear();

        AnimalHealth[] animals = FindObjectsOfType<AnimalHealth>();
        for (int i = 0; i < animals.Length; i++)
        {
            if (animals[i] != null)
            {
                Destroy(animals[i].gameObject);
            }
        }

        FoodMove[] foods = FindObjectsOfType<FoodMove>();
        for (int i = 0; i < foods.Length; i++)
        {
            if (foods[i] != null)
            {
                Destroy(foods[i].gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();

        for (int i = 0; i < alive.Count; i++)
        {
            if (alive[i] != null)
            {
                alive[i].Removed -= OnAnimalRemoved;
            }
        }
        alive.Clear();
    }

    [ContextMenu("恢复默认五关数值")]
    private void RestoreDefaultLevels()
    {
        levels = CreateDefaultLevels();
    }

    private static LevelDefinition[] CreateDefaultLevels()
    {
        return new LevelDefinition[]
        {
            new LevelDefinition
            {
                displayName = "牧场热身",
                targetKills = 8,
                spawnInterval = 1.50f,
                firstSpawnDelay = 1.20f,
                speedMultiplier = 0.60f,
                minHp = 1,
                maxHp = 1,
                maxAlive = 6,
                chickWeight = 100,
                deerWeight = 0,
                dogWeight = 0
            },
            new LevelDefinition
            {
                displayName = "鸡飞狗跳",
                targetKills = 10,
                spawnInterval = 1.30f,
                firstSpawnDelay = 1.10f,
                speedMultiplier = 0.72f,
                minHp = 2,
                maxHp = 2,
                maxAlive = 7,
                chickWeight = 65,
                deerWeight = 20,
                dogWeight = 15
            },
            new LevelDefinition
            {
                displayName = "森林来客",
                targetKills = 12,
                spawnInterval = 1.15f,
                firstSpawnDelay = 1.00f,
                speedMultiplier = 0.85f,
                minHp = 2,
                maxHp = 2,
                maxAlive = 8,
                chickWeight = 40,
                deerWeight = 40,
                dogWeight = 20
            },
            new LevelDefinition
            {
                displayName = "猎手试炼",
                targetKills = 15,
                spawnInterval = 1.00f,
                firstSpawnDelay = 0.90f,
                speedMultiplier = 0.98f,
                minHp = 3,
                maxHp = 3,
                maxAlive = 10,
                chickWeight = 25,
                deerWeight = 45,
                dogWeight = 30
            },
            new LevelDefinition
            {
                displayName = "终极围猎",
                targetKills = 18,
                spawnInterval = 0.85f,
                firstSpawnDelay = 0.80f,
                speedMultiplier = 1.10f,
                minHp = 3,
                maxHp = 4,
                maxAlive = 12,
                chickWeight = 15,
                deerWeight = 45,
                dogWeight = 40
            }
        };
    }
}
