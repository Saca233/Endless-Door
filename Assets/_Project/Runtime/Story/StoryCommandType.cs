namespace OwariNakiTobira
{
    public enum StoryCommandType
    {
        ShowDialogue,
        Wait,
        SetFlag,
        WaitForFlag,
        LockPlayer,
        UnlockPlayer,
        EnablePuzzleRule,
        DisablePuzzleRule,
        OpenDesktopWindow,
        CloseDesktopWindow,
        MoveDesktopWindow,
        FadeIn,
        FadeOut,
        LoadAdditiveLevel,
        UnloadLevel,
        TriggerDoor,
        InvokeConfiguredEvent
    }
}
