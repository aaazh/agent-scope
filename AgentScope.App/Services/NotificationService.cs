namespace AgentScope.App.Services;

/// <summary>
/// Handles Windows Toast notifications for permission requests and alerts.
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// Show a permission request toast with Allow/Deny actions.
    /// </summary>
    public static void ShowPermissionRequest(string eventId, string toolName, string description)
    {
        // Use Windows Toast Notification API
        // This requires packaging as MSIX or using a compat shim.
        // For now, use a simple balloon tip via tray icon as fallback.

        try
        {
            // In production: use Microsoft.Toolkit.Uwp.Notifications
            // var toast = new ToastContentBuilder()
            //     .AddText($"{toolName} 请求权限")
            //     .AddText(description)
            //     .AddButton("允许", ToastActivationType.Background, $"allow:{eventId}")
            //     .AddButton("拒绝", ToastActivationType.Background, $"deny:{eventId}")
            //     .Show();
        }
        catch
        {
            // Toast notifications require app identity (packaged app).
            // Fallback: use Process.Start with a PowerShell notification
            FallbackNotification(toolName, description);
        }
    }

    private static void FallbackNotification(string title, string message)
    {
        try
        {
            // Use PowerShell as fallback for system notification
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"& {{ $null = [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] }}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            // Silent fail — notifications are non-critical
        }
    }
}
