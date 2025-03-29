using System;
using System.IO;
using UnityEngine;

public static class Logger
{
    private static string logFilePath;

    static Logger()
    {
        logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime_log.txt");

        try
        {
            File.WriteAllText(logFilePath, $"==== Log Start ({DateTime.Now}) ===={Environment.NewLine}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"⚠️ ログファイル初期化失敗: {ex.Message}");
        }
    }

    public static void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string fullMessage = $"[{timestamp}] {message}";

#if UNITY_EDITOR
        Debug.Log(message);
#else
        try
        {
            File.AppendAllText(logFilePath, fullMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"⚠️ ログファイル書き込み失敗: {ex.Message}");
        }
#endif

    }

    public static void LogWarning(string message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#else
        Log("⚠️ " + message);
#endif
    }

    public static void LogError(string message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#else
        Log("❌ " + message);
#endif
    }
}