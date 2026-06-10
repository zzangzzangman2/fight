using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim/Enemy Placeholder Visual Data")]
public sealed class EnemyPlaceholderVisualData : ScriptableObject
{
    public string enemyId;
    public Sprite idleSprite;
    public Sprite moveSprite;
    public Sprite attackSprite;
    public Sprite skillSprite;
    public Sprite hitSprite;
    public Sprite defeatedSprite;
    public Sprite actedSprite;

    public Sprite Resolve(string pose)
    {
        switch (pose)
        {
            case "move":
                return moveSprite != null ? moveSprite : idleSprite;
            case "attack":
                return attackSprite != null ? attackSprite : idleSprite;
            case "skill":
                return skillSprite != null ? skillSprite : attackSprite != null ? attackSprite : idleSprite;
            case "hit":
                return hitSprite != null ? hitSprite : idleSprite;
            case "defeated":
                return defeatedSprite != null ? defeatedSprite : idleSprite;
            case "acted":
                return actedSprite != null ? actedSprite : idleSprite;
            default:
                return idleSprite;
        }
    }
}
}
