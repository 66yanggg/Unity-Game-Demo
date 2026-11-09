using UnityEngine;

/// <summary>
/// 玩家发射的食物子弹。向前飞行，击中动物后由动物脚本调用 Consume 销毁。
/// 使用无重力刚体，避免子弹下坠导致打不中。
/// </summary>
public class FoodMove : MonoBehaviour
{
    [SerializeField] private float speed = 22f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 4f;

    private bool consumed;

    public int Damage => damage;

    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.useGravity = false;
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (consumed) return;
        if (GameFacade.Instance != null && !GameFacade.Instance.IsGameplayActive) return;

        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.World);

        if (transform.position.z > 42f)
        {
            Destroy(gameObject);
        }
    }

    public void SetDamage(int value)
    {
        damage = Mathf.Max(1, value);
    }

    public void Consume()
    {
        if (consumed) return;

        consumed = true;
        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        Destroy(gameObject);
    }
}
