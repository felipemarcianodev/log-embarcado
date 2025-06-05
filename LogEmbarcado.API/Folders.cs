namespace LogEmbarcado.API
{
    public static class Folders
    {
        public static string GetBackupFolder()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "MetricsBackups");
        }

        public static string GetDatabasePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_metrics.db");
        }
    }
}
