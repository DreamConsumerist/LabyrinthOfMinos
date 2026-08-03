public static class GameplayMenuState
{
    public static bool AnyMenuOpen => 
    (LocalPauseMenu.Instance != null && LocalPauseMenu.Instance.IsShowing) 
    || (DeathScreenManager.Instance != null && DeathScreenManager.Instance.IsShowing) 
    || (WinScreenManager.Instance != null && WinScreenManager.Instance.IsShowing) 
    || (LeaveConfirmationDialog.Instance != null && LeaveConfirmationDialog.Instance.IsShowing);
}
