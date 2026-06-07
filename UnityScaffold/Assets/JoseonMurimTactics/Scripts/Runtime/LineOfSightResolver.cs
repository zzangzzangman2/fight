namespace JoseonMurimTactics
{
    public sealed class LineOfSightResolver
    {
        private readonly MovementResolver movementResolver;

        public LineOfSightResolver(MovementResolver movementResolver)
        {
            this.movementResolver = movementResolver;
        }

        public bool HasLineOfSight(string fromNodeId, string toNodeId)
        {
            BattleNodeData to = movementResolver.FindNode(toNodeId);
            if (to == null)
            {
                return false;
            }

            return to.hazardType != HazardType.Smoke;
        }

        public int CoverBonus(string targetNodeId, int range)
        {
            if (range <= 1)
            {
                return 0;
            }

            BattleNodeData target = movementResolver.FindNode(targetNodeId);
            if (target == null)
            {
                return 0;
            }

            if (target.coverType == CoverType.Heavy)
            {
                return 4;
            }

            return target.coverType == CoverType.Light ? 2 : 0;
        }
    }
}
