namespace OwariNakiTobira
{
    public static class PlayerStateTransitionRules
    {
        public static bool IsValid(PlayerStateId from, PlayerStateId to)
        {
            if (from == to)
            {
                return true;
            }

            switch (from)
            {
                case PlayerStateId.Idle:
                    return to == PlayerStateId.Run || to == PlayerStateId.Jump || to == PlayerStateId.Fall || to == PlayerStateId.Disabled;
                case PlayerStateId.Run:
                    return to == PlayerStateId.Idle || to == PlayerStateId.Jump || to == PlayerStateId.Fall || to == PlayerStateId.Disabled;
                case PlayerStateId.Jump:
                    return to == PlayerStateId.Fall || to == PlayerStateId.Land || to == PlayerStateId.Disabled;
                case PlayerStateId.Fall:
                    return to == PlayerStateId.Jump || to == PlayerStateId.Land || to == PlayerStateId.Disabled;
                case PlayerStateId.Land:
                    return to == PlayerStateId.Idle || to == PlayerStateId.Run || to == PlayerStateId.Jump || to == PlayerStateId.Fall || to == PlayerStateId.Disabled;
                case PlayerStateId.Disabled:
                    return to == PlayerStateId.Idle || to == PlayerStateId.Run || to == PlayerStateId.Fall;
                default:
                    return false;
            }
        }
    }
}
