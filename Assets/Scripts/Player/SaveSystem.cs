using System;
using System.IO;
using UnityEngine;

[System.Serializable]
public class KeyboardBindings
{
    public KeyCode moveForward = KeyCode.W;
    public KeyCode moveBackward = KeyCode.S;
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;
    public KeyCode dash = KeyCode.LeftShift;
    public KeyCode interact = KeyCode.E;
    public KeyCode attack = KeyCode.Mouse0;
    public KeyCode skill = KeyCode.Q;
    public KeyCode ultimate = KeyCode.R;
}

[System.Serializable]
public class GamepadBindings
{
    public string dash = "JoystickButton0";
    public string interact = "JoystickButton1";
    public string attack = "JoystickButton2";
    public string skill = "JoystickButton3";
    public string ultimate = "JoystickButton4";
}

[System.Serializable]
public class GameSettingsData
{
    public KeyboardBindings keyboard = new KeyboardBindings();
    public GamepadBindings gamepad = new GamepadBindings();
    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;
    public int qualityLevel = 2;
    public bool fullscreen = true;
}

public static class SaveSystem
{
    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "settings.json");
    private static GameSettingsData cachedData;

    public static GameSettingsData LoadSettings()
    {
        if (cachedData != null)
            return cachedData;

        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                cachedData = JsonUtility.FromJson<GameSettingsData>(json);
                if (cachedData != null)
                {
                    if (cachedData.keyboard == null) cachedData.keyboard = new KeyboardBindings();
                    if (cachedData.gamepad == null) cachedData.gamepad = new GamepadBindings();
                    return cachedData;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("SaveSystem: Failed to load settings from " + SavePath + ". Exception: " + ex.Message);
            }
        }

        cachedData = new GameSettingsData();
        return cachedData;
    }

    public static void SaveSettings(GameSettingsData data)
    {
        cachedData = data;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(SavePath, json);
            Debug.Log("SaveSystem: Saved settings to " + SavePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("SaveSystem: Failed to save settings to " + SavePath + ". Exception: " + ex.Message);
        }
    }
}
