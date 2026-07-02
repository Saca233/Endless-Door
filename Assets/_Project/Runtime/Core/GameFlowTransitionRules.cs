namespace OwariNakiTobira
{
    public static class GameFlowTransitionRules
    {
        public static bool IsValid(GameFlowState from, GameFlowState to)
        {
            if (from == to)
            {
                return true;
            }

            switch (from)
            {
                case GameFlowState.Boot:
                    return to == GameFlowState.DesktopIntro;
                case GameFlowState.DesktopIntro:
                    return to == GameFlowState.Level01;
                case GameFlowState.Level01:
                    return to == GameFlowState.Level02 || to == GameFlowState.Blackout || to == GameFlowState.LoopMenu;
                case GameFlowState.Level02:
                    return to == GameFlowState.Level03 || to == GameFlowState.Blackout || to == GameFlowState.LoopMenu;
                case GameFlowState.Level03:
                    return to == GameFlowState.FinalSequence || to == GameFlowState.Blackout || to == GameFlowState.LoopMenu;
                case GameFlowState.FinalSequence:
                    return to == GameFlowState.Blackout;
                case GameFlowState.Blackout:
                    return to == GameFlowState.Epilogue || to == GameFlowState.LoopMenu;
                case GameFlowState.Epilogue:
                    return to == GameFlowState.LoopMenu || to == GameFlowState.StaticEnding;
                case GameFlowState.LoopMenu:
                    return to == GameFlowState.Level01 || to == GameFlowState.DesktopIntro || to == GameFlowState.StaticEnding;
                case GameFlowState.StaticEnding:
                    return false;
                default:
                    return false;
            }
        }
    }
}
