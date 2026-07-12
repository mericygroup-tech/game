using UnityEngine;

public class InputSettingsManager : MonoBehaviour
{
    private GameSettingsData settingsData;

    public KeyboardBindings Keyboard => settingsData != null ? settingsData.keyboard : null;
    public GamepadBindings Gamepad => settingsData != null ? settingsData.gamepad : null;

    private void Awake()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        settingsData = SaveSystem.LoadSettings();
    }

    public void SaveSettings()
    {
        if (settingsData != null)
        {
            SaveSystem.SaveSettings(settingsData);
        }
    }

    public void RebindKeyboardKey(string actionName, KeyCode newKey)
    {
        if (settingsData == null || settingsData.keyboard == null)
            return;

        switch (actionName.ToLower())
        {
            case "forward":
            case "moveforward":
                settingsData.keyboard.moveForward = newKey;
                break;
            case "backward":
            case "movebackward":
                settingsData.keyboard.moveBackward = newKey;
                break;
            case "left":
            case "moveleft":
                settingsData.keyboard.moveLeft = newKey;
                break;
            case "right":
            case "moveright":
                settingsData.keyboard.moveRight = newKey;
                break;
            case "dash":
                settingsData.keyboard.dash = newKey;
                break;
            case "interact":
                settingsData.keyboard.interact = newKey;
                break;
            case "attack":
                settingsData.keyboard.attack = newKey;
                break;
            case "skill":
                settingsData.keyboard.skill = newKey;
                break;
            case "ultimate":
                settingsData.keyboard.ultimate = newKey;
                break;
            default:
                Debug.LogWarning("InputSettingsManager: Unknown action name for keyboard rebind: " + actionName);
                return;
        }

        SaveSettings();
    }

    public void ResetToDefaults()
    {
        settingsData = new GameSettingsData();
        SaveSettings();
    }
}
