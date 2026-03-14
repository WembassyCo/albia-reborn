namespace Albia.Core
{
    /// <summary>
    /// Game version information
    /// </summary>
    public static class GameVersion
    {
        public const string VERSION = "0.3.0";
        public const string BUILD_DATE = "2026-03-14";
        public const int WAVE = 4;
        public const string STATUS = "MVP+";
        
        public static string GetVersionString() => $"Albia Reborn v{VERSION} ({STATUS}) - Wave {WAVE}";
    }
}