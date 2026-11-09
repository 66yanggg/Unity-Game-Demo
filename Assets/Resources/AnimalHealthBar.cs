using UnityEngine;

/// <summary>
/// 动物头顶的血条。使用两个 SpriteRenderer 绘制，
/// 不依赖 World Space Canvas，开销比每个动物一张 Canvas 小很多。
/// </summary>
public class AnimalHealthBar : MonoBehaviour
{
    private static Sprite centeredSprite;
    private static Sprite leftPivotSprite;

    private Transform target;
    private Transform fillTransform;
    private SpriteRenderer fillRenderer;

    private float barWidth = 2.8f;
    private float barHeight = 0.2f;
    private float yOffset = 3f;
    private int maxHp = 1;

    public static AnimalHealthBar Create(Transform target, Collider bodyCollider, int maxHp, int currentHp)
    {
        GameObject go = new GameObject("AnimalHealthBar");
        AnimalHealthBar bar = go.AddComponent<AnimalHealthBar>();
        bar.Initialize(target, bodyCollider, maxHp, currentHp);
        return bar;
    }

    private void Initialize(Transform target, Collider bodyCollider, int maxHp, int currentHp)
    {
        this.target = target;
        this.maxHp = Mathf.Max(1, maxHp);

        float top = 3f;
        if (bodyCollider != null)
        {
            top = bodyCollider.bounds.max.y - target.position.y;
        }

        yOffset = Mathf.Max(1.4f, top + 0.8f);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform, false);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = GetCenteredSprite();
        backgroundRenderer.color = new Color(0f, 0f, 0f, 0.75f);
        backgroundRenderer.sortingOrder = 100;
        background.transform.localScale = new Vector3(barWidth + 0.18f, barHeight + 0.12f, 1f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(transform, false);
        fillTransform = fill.transform;
        fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = GetLeftPivotSprite();
        fillRenderer.sortingOrder = 101;

        fillTransform.localPosition = new Vector3(-barWidth * 0.5f, 0f, -0.01f);
        SetValue(currentHp, this.maxHp);

        FollowTarget();
    }

    public void SetValue(int currentHp, int maxHp)
    {
        this.maxHp = Mathf.Max(1, maxHp);
        float ratio = Mathf.Clamp01((float)currentHp / this.maxHp);

        if (fillTransform != null)
        {
            fillTransform.localScale = new Vector3(barWidth * ratio, barHeight, 1f);
        }

        if (fillRenderer != null)
        {
            if (ratio > 0.6f)
            {
                fillRenderer.color = new Color(0.2f, 0.9f, 0.3f);
            }
            else if (ratio > 0.3f)
            {
                fillRenderer.color = new Color(1f, 0.75f, 0.1f);
            }
            else
            {
                fillRenderer.color = new Color(0.95f, 0.2f, 0.2f);
            }
        }
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position + Vector3.up * yOffset;
        // 相机是俯视视角，把血条平铺在 XZ 平面上。
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static Sprite GetCenteredSprite()
    {
        if (centeredSprite == null)
        {
            centeredSprite = CreateWhiteSprite(new Vector2(0.5f, 0.5f));
        }
        return centeredSprite;
    }

    private static Sprite GetLeftPivotSprite()
    {
        if (leftPivotSprite == null)
        {
            leftPivotSprite = CreateWhiteSprite(new Vector2(0f, 0.5f));
        }
        return leftPivotSprite;
    }

    private static Sprite CreateWhiteSprite(Vector2 pivot)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "HealthBarWhite";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), pivot, 1f);
        sprite.name = "HealthBarSprite";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
