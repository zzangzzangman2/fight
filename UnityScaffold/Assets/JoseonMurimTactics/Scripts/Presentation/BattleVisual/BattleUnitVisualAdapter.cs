using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleUnitVisualAdapter : MonoBehaviour
{
    public CharacterVisualData visual;
    public CharacterVisualController spriteController;
    public CharacterModelVisualController modelController;
    public string sortingLayerName = "Characters";
    public int baseSortingOrder = 1000;

    private bool usingModel3D;

    public int CurrentBodySortingOrder
    {
        get
        {
            if (usingModel3D && modelController != null)
            {
                return modelController.CurrentBodySortingOrder;
            }

            return spriteController == null ? baseSortingOrder : spriteController.CurrentBodySortingOrder;
        }
    }

    public void Bind(CombatantData combatant, bool selected)
    {
        Bind(combatant == null ? null : combatant.visual, selected);
    }

    public void Bind(CharacterVisualData visualData, bool selected)
    {
        visual = visualData;
        usingModel3D = visual != null && visual.battleVisualMode == CharacterBattleVisualMode.Model3D &&
                       visual.modelPrefab != null;
        if (usingModel3D)
        {
            EnsureModelController();
            if (spriteController != null)
            {
                SetSpriteVisible(false);
            }

            modelController.sortingLayerName = sortingLayerName;
            modelController.baseSortingOrder = baseSortingOrder;
            modelController.Bind(visual, selected);
            modelController.SetVisible(true);
            return;
        }

        EnsureSpriteController();
        if (modelController != null)
        {
            modelController.SetVisible(false);
        }

        spriteController.sortingLayerName = sortingLayerName;
        spriteController.baseSortingOrder = baseSortingOrder;
        SetSpriteVisible(true);
        spriteController.visual = visual;
        spriteController.ApplyVisual();
        spriteController.SetSelected(selected);
    }

    public void BindExisting(CharacterVisualController controller, CharacterVisualData visualData, bool selected)
    {
        spriteController = controller;
        Bind(visualData, selected);
    }

    public void SetSelected(bool value)
    {
        if (usingModel3D && modelController != null)
        {
            modelController.SetSelected(value);
        }
        else if (spriteController != null)
        {
            spriteController.SetSelected(value);
        }
    }

    public void SetActed(bool value)
    {
        if (usingModel3D && modelController != null)
        {
            modelController.SetActed(value);
        }
        else if (spriteController != null)
        {
            spriteController.SetActed(value);
        }
    }

    public void SetDefeated(bool value)
    {
        if (usingModel3D && modelController != null)
        {
            modelController.SetDefeated(value);
        }
        else if (spriteController != null)
        {
            spriteController.SetDefeated(value);
        }
    }

    public void FaceToward(Vector3 worldPosition)
    {
        if (usingModel3D && modelController != null)
        {
            modelController.FaceToward(worldPosition);
        }
        else if (spriteController != null)
        {
            spriteController.FaceToward(worldPosition);
        }
    }

    public void FaceDirection(Vector2 direction)
    {
        if (usingModel3D && modelController != null)
        {
            modelController.FaceDirection(direction);
        }
        else if (spriteController != null)
        {
            spriteController.FaceDirection(direction);
        }
    }

    public CombatActionTimeline CreateTimeline(bool special)
    {
        if (usingModel3D && modelController != null)
        {
            return modelController.CreateTimeline(special);
        }

        return spriteController != null ? spriteController.CreateTimeline(special) : new CombatActionTimeline(null, special);
    }

    public float WalkSecondsPerTile()
    {
        if (usingModel3D && modelController != null)
        {
            return modelController.WalkSecondsPerTile();
        }

        return spriteController != null ? spriteController.WalkSecondsPerTile() : 0.24f;
    }

    public float MoveSettleTime()
    {
        if (usingModel3D && modelController != null)
        {
            return modelController.MoveSettleTime();
        }

        return spriteController != null ? spriteController.MoveSettleTime() : 0.10f;
    }

    public void PlayIdle()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlayIdle();
        }
        else if (spriteController != null)
        {
            spriteController.PlayIdle();
        }
    }

    public void PlayMove()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlayMove();
        }
        else if (spriteController != null)
        {
            spriteController.PlayMove();
        }
    }

    public void SetMoveStridePhase(float phase)
    {
        if (!usingModel3D && spriteController != null)
        {
            spriteController.SetMoveStridePhase(phase);
        }
    }

    public void PlayAttack()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlayAttack();
        }
        else if (spriteController != null)
        {
            spriteController.PlayAttack();
        }
    }

    public void PlaySkill()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlaySkill();
        }
        else if (spriteController != null)
        {
            spriteController.PlaySkill();
        }
    }

    public void PlayHit()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlayHit();
        }
        else if (spriteController != null)
        {
            spriteController.PlayHit();
        }
    }

    public void PlayGuard()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlayGuard();
        }
        else if (spriteController != null)
        {
            spriteController.PlayGuard();
        }
    }

    public void PlayWait()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlayWait();
        }
        else if (spriteController != null)
        {
            spriteController.PlayWait();
        }
    }

    public void PlayVictory()
    {
        if (usingModel3D && modelController != null)
        {
            modelController.PlayVictory();
        }
        else if (spriteController != null)
        {
            spriteController.PlayVictory();
        }
    }

    private void EnsureSpriteController()
    {
        if (spriteController == null)
        {
            spriteController = GetComponent<CharacterVisualController>();
        }

        if (spriteController == null)
        {
            spriteController = gameObject.AddComponent<CharacterVisualController>();
        }
    }

    private void EnsureModelController()
    {
        if (modelController == null)
        {
            modelController = GetComponent<CharacterModelVisualController>();
        }

        if (modelController == null)
        {
            modelController = gameObject.AddComponent<CharacterModelVisualController>();
        }
    }

    private void SetSpriteVisible(bool value)
    {
        if (spriteController == null)
        {
            return;
        }

        spriteController.enabled = value;
        SetRenderer(spriteController.bodyRenderer, value);
        SetRenderer(spriteController.baseLayerRenderer, value);
        SetRenderer(spriteController.outfitLayerRenderer, value);
        SetRenderer(spriteController.hairLayerRenderer, value);
        SetRenderer(spriteController.faceLayerRenderer, value);
        SetRenderer(spriteController.weaponLayerRenderer, value);
        SetRenderer(spriteController.accessoryLayerRenderer, value);
        SetRenderer(spriteController.shadowRenderer, value);
        SetRenderer(spriteController.leftFootRenderer, value);
        SetRenderer(spriteController.rightFootRenderer, value);
        SetRenderer(spriteController.selectionRenderer, value);
        SetRenderer(spriteController.effectRenderer, value);
    }

    private static void SetRenderer(SpriteRenderer renderer, bool value)
    {
        if (renderer != null)
        {
            renderer.enabled = value;
        }
    }
}
}
