using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 动物的血量组件。由 LevelManager 在生成动物时注入本关血量，
/// 被食物击中时扣血，血量归零后死亡并通知关卡系统。
/// </summary>
public class AnimalHealth : MonoBehaviour
{
    /// <summary>第一个参数是动物，第二个参数表示是否算作“被击杀”（false 表示逃跑/撞玩家后消失）。</summary>
    public event Action<AnimalHealth, bool> Removed;

    [SerializeField] private int maxHp = 1;
    private int currentHp;
    private int scoreValue = 10;
    private bool initialized;
    private bool dead;

    private AnimalHealthBar healthBar;
    private Rigidbody body;
    private Collider[] colliders;
    private Vector3 originalScale;
    private Coroutine hitRoutine;

    public bool IsDead => dead;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public int ScoreValue => scoreValue;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
        originalScale = transform.localScale;
    }

    public void Initialize(int hp, int score)
    {
        initialized = true;
        dead = false;
        maxHp = Mathf.Max(1, hp);
        currentHp = maxHp;
        scoreValue = Mathf.Max(1, score);
        originalScale = transform.localScale;

        if (healthBar == null)
        {
            Collider bodyCollider = GetComponent<Collider>();
            healthBar = AnimalHealthBar.Create(transform, bodyCollider, maxHp, currentHp);
        }
        else
        {
            healthBar.SetValue(currentHp, maxHp);
        }
    }

    public void TakeDamage(int amount)
    {
        if (dead || !initialized) return;

        currentHp -= Mathf.Max(1, amount);

        if (healthBar != null)
        {
            healthBar.SetValue(currentHp, maxHp);
        }

        if (currentHp <= 0)
        {
            Die(true);
            return;
        }

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            transform.localScale = originalScale;
        }
        hitRoutine = StartCoroutine(HitPunch());
    }

    /// <summary>没有击杀奖励的消失：撞到玩家、跑出场景、关卡清理时使用。</summary>
    public void Despawn()
    {
        if (dead) return;

        dead = true;
        RemoveHealthBar();
        DisableCombat();

        Removed?.Invoke(this, false);
        Removed = null;

        Destroy(gameObject, 0.05f);
    }

    private void Die(bool awardKill)
    {
        if (dead) return;

        dead = true;
        RemoveHealthBar();
        DisableCombat();

        if (awardKill && GameFacade.Instance != null)
        {
            GameFacade.Instance.PlaySound("hachimi oh yeahhhh", 0.9f);
        }

        Removed?.Invoke(this, awardKill);
        Removed = null;

        StartCoroutine(DeathAnimation());
    }

    private IEnumerator HitPunch()
    {
        transform.localScale = originalScale * 1.06f;
        yield return new WaitForSeconds(0.06f);

        if (!dead)
        {
            transform.localScale = originalScale;
        }

        hitRoutine = null;
    }

    private IEnumerator DeathAnimation()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 0.05f;
        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            transform.Rotate(Vector3.up, 540f * Time.deltaTime, Space.World);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void DisableCombat()
    {
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = false;
            }
        }

        if (body != null)
        {
            // EnemyController 已经把动物设为 kinematic；这里只确保不再参与物理模拟。
            body.isKinematic = true;
        }
    }

    private void RemoveHealthBar()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
            healthBar = null;
        }
    }

    private void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
            healthBar = null;
        }
    }
}
