using System;
using System.IO;

namespace AmongUsExternal
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        private static readonly object LockObj = new object();

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message, null);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARN", message, null);
        }

        public static void LogError(string message, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[HATA] " + message + (ex != null ? " -> " + ex.Message : ""));
            Console.ResetColor();

            WriteLog("ERROR", message, ex);
        }

        private static void WriteLog(string level, string message, Exception ex)
        {
            try
            {
                lock (LockObj)
                {
                    string logLine = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", DateTime.Now, level, message);
                    if (ex != null)
                    {
                        logLine += Environment.NewLine + "Exception: " + ex.ToString();
                    }
                    logLine += Environment.NewLine;

                    File.AppendAllText(LogFilePath, logLine);
                }
            }
            catch
            {
                // Prevent crash if logging fails
            }
        }
    }
}
