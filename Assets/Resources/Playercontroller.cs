using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家控制：左右移动，按住空格连续发射食物。
/// 加入射速冷却、子弹与玩家碰撞忽略、受击闪烁，手感更稳定。
/// </summary>
public class Playercontroller : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float XRange = 15f;
    [SerializeField] private float fireCooldown = 0.25f;

    private float nextFireTime;
    private float fixedZ;
    private Renderer[] renderers;
    private Coroutine blinkRoutine;
    private Rigidbody body;

    private void Awake()
    {
        fixedZ = transform.position.z;
        renderers = GetComponentsInChildren<Renderer>(true);

        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            // 玩家保持直立、不前后移动，但保留动态刚体用于和动物发生碰撞。
            body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezePositionZ
                | RigidbodyConstraints.FreezeRotation;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (GameFacade.Instance == null || !GameFacade.Instance.IsGameplayActive) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        if (TouchControls.Instance != null && TouchControls.Instance.IsVisible)
        {
            horizontal = Mathf.Clamp(horizontal + TouchControls.Instance.Horizontal, -1f, 1f);
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x + horizontal * speed * Time.deltaTime, -XRange, XRange);
        position.z = fixedZ;
        transform.position = position;

        bool fireHeld = Input.GetKey(KeyCode.Space)
            || (TouchControls.Instance != null && TouchControls.Instance.FireHeld);

        if (fireHeld && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireCooldown;
        }
    }

    private void Fire()
    {
        GameObject food = GameFacade.Instance.LoadFood("Sphere");
        if (food == null) return;

        food.transform.position = transform.position + new Vector3(0f, 0.7f, 0.9f);
        food.transform.rotation = Quaternion.identity;

        Collider foodCollider = food.GetComponent<Collider>();
        Collider playerCollider = GetComponent<Collider>();
        if (foodCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(foodCollider, playerCollider, true);
        }

        FoodMove projectile = food.GetComponent<FoodMove>();
        if (projectile != null)
        {
            projectile.SetDamage(1);
        }

        GameFacade.Instance.PlaySound("hachimi vox", 0.8f);
    }

    public void PlayHitFeedback()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
        }

        blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    public void ResetForLevel()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        SetRenderersEnabled(true);
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        transform.position = new Vector3(0f, transform.position.y, fixedZ);
    }

    private IEnumerator BlinkRoutine()
    {
        for (int i = 0; i < 6; i++)
        {
            SetRenderersEnabled(false);
            yield return new WaitForSeconds(0.07f);
            SetRenderersEnabled(true);
            yield return new WaitForSeconds(0.07f);
        }

        SetRenderersEnabled(true);
        blinkRoutine = null;
    }

    private void SetRenderersEnabled(bool value)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = value;
            }
        }
    }

    private void OnDisable()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        SetRenderersEnabled(true);
    }
}
