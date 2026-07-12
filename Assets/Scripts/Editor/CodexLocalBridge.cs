using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Local file-based bridge for Codex automation without Unity AI MCP entitlement.
/// Commands are JSON files in Library/CodexBridge/inbox; results are written to outbox.
/// </summary>
[InitializeOnLoad]
public static class CodexLocalBridge
{
    private const double PollIntervalSeconds = 0.25d;
    private static readonly string RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Library", "CodexBridge");
    private static readonly string InboxDirectory = Path.Combine(RootDirectory, "inbox");
    private static readonly string OutboxDirectory = Path.Combine(RootDirectory, "outbox");
    private static readonly string ProcessingDirectory = Path.Combine(RootDirectory, "processing");
    private static double nextPollTime;

    static CodexLocalBridge()
    {
        EnsureDirectories();
        EditorApplication.update += OnEditorUpdate;
        Debug.Log("CodexLocalBridge active. Inbox: " + InboxDirectory);
    }

    [MenuItem("Tools/Codex Local Bridge/Write Status")]
    public static void WriteStatus()
    {
        EnsureDirectories();
        string path = Path.Combine(RootDirectory, "status.json");
        File.WriteAllText(path, BuildResponse("status", true, "CodexLocalBridge is active.", BuildStatusData()));
        Debug.Log("CodexLocalBridge status written to " + path);
    }

    [MenuItem("Tools/Codex Local Bridge/Open Bridge Folder")]
    public static void OpenBridgeFolder()
    {
        EnsureDirectories();
        EditorUtility.RevealInFinder(RootDirectory);
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.timeSinceStartup < nextPollTime)
            return;

        nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;
        ProcessPendingCommands();
    }

    private static void ProcessPendingCommands()
    {
        EnsureDirectories();

        string[] files;
        try
        {
            files = Directory.GetFiles(InboxDirectory, "*.json").OrderBy(File.GetCreationTimeUtc).ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CodexLocalBridge could not read inbox: " + ex.Message);
            return;
        }

        foreach (string file in files)
            ProcessCommandFile(file);
    }

    private static void ProcessCommandFile(string file)
    {
        string processingPath = Path.Combine(ProcessingDirectory, Path.GetFileName(file));

        try
        {
            if (File.Exists(processingPath))
                File.Delete(processingPath);

            File.Move(file, processingPath);
        }
        catch (IOException)
        {
            return;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CodexLocalBridge could not claim command " + file + ": " + ex.Message);
            return;
        }

        BridgeCommand command = null;
        string id = Path.GetFileNameWithoutExtension(processingPath);

        try
        {
            string json = File.ReadAllText(processingPath);
            command = JsonUtility.FromJson<BridgeCommand>(json);
            if (command == null)
                throw new InvalidOperationException("Command JSON is empty or invalid.");

            if (!string.IsNullOrWhiteSpace(command.id))
                id = command.id.Trim();

            string message = ExecuteCommand(command);
            WriteResponse(id, true, message, BuildStatusData());
        }
        catch (Exception ex)
        {
            WriteResponse(id, false, ex.GetType().Name + ": " + ex.Message, BuildStatusData());
            Debug.LogException(ex);
        }
        finally
        {
            try
            {
                if (File.Exists(processingPath))
                    File.Delete(processingPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("CodexLocalBridge could not delete processed command: " + ex.Message);
            }
        }
    }

    private static string ExecuteCommand(BridgeCommand command)
    {
        switch ((command.type ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "ping":
                return "pong";

            case "refresh_assets":
                AssetDatabase.Refresh();
                return "AssetDatabase.Refresh completed.";

            case "execute_menu_item":
                Require(command.menuItem, "menuItem");
                bool executed = EditorApplication.ExecuteMenuItem(command.menuItem);
                if (!executed)
                    throw new InvalidOperationException("Menu item was not found or could not execute: " + command.menuItem);
                return "Executed menu item: " + command.menuItem;

            case "invoke_static":
                Require(command.className, "className");
                Require(command.methodName, "methodName");
                InvokeStaticMethod(command.className, command.methodName);
                return "Invoked static method: " + command.className + "." + command.methodName;

            case "open_scene":
                Require(command.path, "path");
                EditorSceneManager.OpenScene(command.path);
                return "Opened scene: " + command.path;

            case "save_scene":
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                return "Saved active scene.";

            case "set_play_mode":
                EditorApplication.isPlaying = command.boolValue;
                return command.boolValue ? "Entering play mode." : "Exiting play mode.";

            case "write_status":
                WriteStatus();
                return "Status file written.";

            default:
                throw new InvalidOperationException("Unknown command type: " + command.type);
        }
    }

    private static void InvokeStaticMethod(string className, string methodName)
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(className, false))
            .FirstOrDefault(foundType => foundType != null);

        if (type == null)
            throw new InvalidOperationException("Type not found: " + className);

        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
            throw new InvalidOperationException("Static method not found: " + className + "." + methodName);

        if (method.GetParameters().Length != 0)
            throw new InvalidOperationException("Only parameterless static methods are supported: " + className + "." + methodName);

        method.Invoke(null, null);
    }

    private static void Require(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Missing required field: " + fieldName);
    }

    private static void WriteResponse(string id, bool ok, string message, string data)
    {
        EnsureDirectories();
        string path = Path.Combine(OutboxDirectory, SanitizeFileName(id) + ".json");
        File.WriteAllText(path, BuildResponse(id, ok, message, data));
    }

    private static string BuildResponse(string id, bool ok, string message, string data)
    {
        return "{\n" +
               "  \"id\": \"" + EscapeJson(id) + "\",\n" +
               "  \"ok\": " + (ok ? "true" : "false") + ",\n" +
               "  \"message\": \"" + EscapeJson(message) + "\",\n" +
               "  \"timestamp\": \"" + DateTime.UtcNow.ToString("O") + "\",\n" +
               "  \"data\": " + data + "\n" +
               "}\n";
    }

    private static string BuildStatusData()
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        string sceneName = EditorSceneManager.GetActiveScene().name;
        return "{\n" +
               "    \"unityVersion\": \"" + EscapeJson(Application.unityVersion) + "\",\n" +
               "    \"projectPath\": \"" + EscapeJson(Directory.GetCurrentDirectory()) + "\",\n" +
               "    \"activeScene\": \"" + EscapeJson(sceneName) + "\",\n" +
               "    \"activeScenePath\": \"" + EscapeJson(scenePath) + "\",\n" +
               "    \"isPlaying\": " + (EditorApplication.isPlaying ? "true" : "false") + "\n" +
               "  }";
    }

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(InboxDirectory);
        Directory.CreateDirectory(OutboxDirectory);
        Directory.CreateDirectory(ProcessingDirectory);
    }

    private static string SanitizeFileName(string value)
    {
        string safe = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            safe = safe.Replace(invalidChar, '_');
        return safe;
    }

    private static string EscapeJson(string value)
    {
        if (value == null)
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    [Serializable]
    private class BridgeCommand
    {
        public string id;
        public string type;
        public string menuItem;
        public string path;
        public string className;
        public string methodName;
        public bool boolValue;
    }
}
