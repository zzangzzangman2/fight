using UnityEngine;

namespace JoseonMurimTactics
{
    public sealed class TerrainResolver
    {
        private readonly BattleMapData map;
        private readonly DiceRoller dice;
        private readonly MovementResolver movement;
        private readonly CombatLog log;

        public TerrainResolver(BattleMapData map, DiceRoller dice, MovementResolver movement, CombatLog log)
        {
            this.map = map;
            this.dice = dice;
            this.movement = movement;
            this.log = log;
        }

        public bool TryUseObject(CombatantRuntime actor, InteractableObjectData interactable)
        {
            if (interactable == null || !actor.actions.CanSpend(interactable.requiredActionSlot))
            {
                return false;
            }

            if (movement.Distance(actor.currentNodeId, interactable.nodeId) > 1)
            {
                log.Add("Terrain", actor.DisplayName + " 상호작용 실패: 거리 밖.");
                return false;
            }

            DiceRoll roll = dice.RollD20();
            int total = roll.total + actor.StatModifier(interactable.stat) + actor.Proficiency;
            bool success = roll.natural == 20 || total >= interactable.dc;
            actor.actions.Spend(interactable.requiredActionSlot);

            log.Add("Terrain", actor.DisplayName + " " + interactable.displayName + ": d20 " + roll.detail + " = " + total + " vs DC " + interactable.dc);

            if (!success)
            {
                return false;
            }

            ApplyEffect(interactable);
            return true;
        }

        private void ApplyEffect(InteractableObjectData interactable)
        {
            BattleNodeData node = map.nodes.Find(item => item.id == interactable.nodeId);
            if (node == null)
            {
                return;
            }

            switch (interactable.effectType)
            {
                case InteractableEffectType.CreateCover:
                    node.coverType = CoverType.Heavy;
                    break;
                case InteractableEffectType.CreateSmoke:
                    node.coverType = CoverType.Heavy;
                    node.hazardType = HazardType.Smoke;
                    break;
                case InteractableEffectType.CreateFire:
                    node.hazardType = HazardType.Fire;
                    break;
                case InteractableEffectType.CreateIce:
                    node.hazardType = HazardType.Ice;
                    break;
                case InteractableEffectType.CollapseBridge:
                    node.hazardType = HazardType.Fall;
                    node.coverType = CoverType.None;
                    break;
                case InteractableEffectType.ShatterAltar:
                    node.hazardType = HazardType.Fall;
                    break;
                case InteractableEffectType.BlockSight:
                    node.coverType = CoverType.Heavy;
                    break;
            }

            log.Add("Terrain", interactable.displayName + " 효과 적용: " + interactable.effectType + ".");
        }
    }
}
