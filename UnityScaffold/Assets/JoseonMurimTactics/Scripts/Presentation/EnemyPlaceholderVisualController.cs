using UnityEngine;

namespace JoseonMurimTactics
{
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyPlaceholderVisualController : MonoBehaviour
{
    [SerializeField]
    private EnemyPlaceholderVisualData visualData;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    public EnemyPlaceholderVisualData VisualData
    {
        get => visualData;
        set
        {
            visualData = value;
            SetPose("idle");
        }
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        SetPose("idle");
    }

    public void SetPose(string pose)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Sprite sprite = visualData != null ? visualData.Resolve(pose) : null;
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    public void SetIdle()
    {
        SetPose("idle");
    }

    public void SetAttack()
    {
        SetPose("attack");
    }

    public void SetHit()
    {
        SetPose("hit");
    }

    public void SetDefeated()
    {
        SetPose("defeated");
    }

    public void SetActed()
    {
        SetPose("acted");
    }
}
}
