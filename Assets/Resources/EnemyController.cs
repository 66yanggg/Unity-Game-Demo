using UnityEngine;

/// <summary>
/// 动物移动与碰撞。血量、死亡表现交给 AnimalHealth，
/// 本脚本只负责：向前移动、撞玩家、被食物击中。
/// </summary>
public class EnemyController : MonoBehaviour
{
    [SerializeField] private float speed = 3f;

    private float baseSpeed;
    private AnimalHealth health;
    private BoxCollider bodyCollider;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
        baseSpeed = speed;

        bodyCollider = GetComponent<BoxCollider>();
        if (bodyCollider == null)
        {
            bodyCollider = gameObject.AddComponent<BoxCollider>();
        }

        FitColliderToModel();

        health = GetComponent<AnimalHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<AnimalHealth>();
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            // 动物用 Transform 移动，物理只负责碰撞检测，避免互相推挤和翻倒。
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    /// <summary>由 LevelManager 调用：套用本关速度倍率与血量。</summary>
    public void Configure(float speedMultiplier, AnimalHealth healthComponent, int hp, int score)
    {
        speed = baseSpeed * Mathf.Max(0.1f, speedMultiplier);

        health = healthComponent != null ? healthComponent : GetComponent<AnimalHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<AnimalHealth>();
        }

        health.Initialize(hp, score);
    }

    private void Update()
    {
        if (health == null || health.IsDead) return;
        if (GameFacade.Instance == null || !GameFacade.Instance.IsGameplayActive) return;

        cachedTransform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);

        if (cachedTransform.position.z < -10f)
        {
            health.Despawn();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (health == null || health.IsDead) return;
        if (GameFacade.Instance == null || !GameFacade.Instance.IsGameplayActive) return;

        // 撞到玩家：玩家扣血，动物消失（不计击杀）。
        Playercontroller player = collision.collider.GetComponentInParent<Playercontroller>();
        if (player != null)
        {
            GameFacade.Instance.PlayerTakeDamage(1);
            health.Despawn();
            return;
        }

        // 被食物击中：按食物伤害扣血，食物自身销毁。
        FoodMove food = collision.collider.GetComponentInParent<FoodMove>();
        if (food != null || collision.collider.CompareTag("Food"))
        {
            int damage = food != null ? food.Damage : 1;
            if (food != null)
            {
                food.Consume();
            }
            health.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Deer_1 / Dog_1 预制体原本的 BoxCollider 过小，食物经常打不中。
    /// 根据模型 Renderer 的包围盒重设碰撞体，保证命中判定和视觉一致。
    /// </summary>
    private void FitColliderToModel()
    {
        if (bodyCollider == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(cachedTransform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (renderer is SpriteRenderer) continue;
            if (renderer is TrailRenderer) continue;
            if (renderer is ParticleSystemRenderer) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds) return;

        Vector3 lossyScale = cachedTransform.lossyScale;
        Vector3 size = new Vector3(
            bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x)),
            bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y)),
            bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z)));

        if (size.x < 0.01f || size.z < 0.01f) return;

        bodyCollider.center = cachedTransform.InverseTransformPoint(bounds.center);
        bodyCollider.size = new Vector3(
            Mathf.Max(0.2f, size.x * 1.05f),
            Mathf.Max(0.2f, size.y * 1.05f),
            Mathf.Max(0.2f, size.z * 1.05f));
    }
}
