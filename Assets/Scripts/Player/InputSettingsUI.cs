using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputSettingsUI : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject settingsPanelPrefab;

    [Header("Rebind Button Mappings")]
    public List<RebindButtonMapping> buttonMappings = new List<RebindButtonMapping>();

    [Header("Controls")]
    public Button resetButton;
    public Button closeButton;

    [System.Serializable]
    public class RebindButtonMapping
    {
        public string actionName; // e.g., "Forward", "Backward", "Left", "Right", "Dash", "Interact", "Attack"
        public Button rebindButton;
        public TMP_Text keyDisplayLabel;
    }

    private GameObject activePanelInstance;
    private InputSettingsManager inputSettings;
    private PlayerController3D playerController;
    private string activeRebindAction = null;
    private bool isMenuOpen = false;

    private void Start()
    {
        // Find player and settings components in the scene
        playerController = FindAnyObjectByType<PlayerController3D>();
        if (playerController != null)
        {
            inputSettings = playerController.GetComponent<InputSettingsManager>();
        }

        // Setup default prefab if none assigned and it exists in Resources
        if (settingsPanelPrefab == null)
        {
            settingsPanelPrefab = Resources.Load<GameObject>("UI/InputSettingsPanel");
        }

        // Initialize Panel state
        if (settingsPanelPrefab != null)
        {
            activePanelInstance = Instantiate(settingsPanelPrefab, transform);
            activePanelInstance.SetActive(false);
            BindControlsFromInstance();
        }
        else
        {
            Debug.LogWarning("InputSettingsUI: settingsPanelPrefab is missing. Please assign it in the Inspector or place it in a Resources/UI folder.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenu();
        }

        if (isMenuOpen && !string.IsNullOrEmpty(activeRebindAction))
        {
            DetectAndApplyRebind();
        }
    }

    private void ToggleMenu()
    {
        if (activePanelInstance == null)
            return;

        isMenuOpen = !isMenuOpen;
        activePanelInstance.SetActive(isMenuOpen);

        if (playerController != null)
        {
            playerController.InputLocked = isMenuOpen;
            
            // Halt movement velocity when menu is opened
            if (isMenuOpen)
            {
                CharacterController cc = playerController.GetComponent<CharacterController>();
                if (cc != null)
                {
                    // Call EndDash just in case they open menu while dashing
                    playerController.EndDash();
                }
            }
        }

        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UpdateUI();
        }
        else
        {
            activeRebindAction = null;
            // Restore default cursor locking configuration based on camera setup
            ThirdPersonCamera cam = FindAnyObjectByType<ThirdPersonCamera>();
            if (cam != null)
            {
                Cursor.lockState = cam.lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !cam.lockCursor;
            }
        }
    }

    private void BindControlsFromInstance()
    {
        if (activePanelInstance == null)
            return;

        // Try to automatically find Reset and Close buttons if they aren't assigned
        if (resetButton == null)
        {
            Transform resetTrans = activePanelInstance.transform.Find("ResetButton");
            if (resetTrans != null) resetButton = resetTrans.GetComponent<Button>();
        }
        if (closeButton == null)
        {
            Transform closeTrans = activePanelInstance.transform.Find("CloseButton");
            if (closeTrans != null) closeButton = closeTrans.GetComponent<Button>();
        }

        if (resetButton != null) resetButton.onClick.AddListener(ResetSettings);
        if (closeButton != null) closeButton.onClick.AddListener(ToggleMenu);

        // Bind clicks for rebind buttons
        foreach (var mapping in buttonMappings)
        {
            if (mapping.rebindButton != null)
            {
                string actionName = mapping.actionName;
                mapping.rebindButton.onClick.AddListener(() => StartRebind(actionName));
            }
        }

        // If mappings are empty, search dynamically for named buttons in children to make it auto-setup
        if (buttonMappings.Count == 0)
        {
            string[] actions = { "Forward", "Backward", "Left", "Right", "Dash", "Interact", "Attack" };
            foreach (string action in actions)
            {
                Transform buttonTrans = activePanelInstance.transform.Find("Btn_" + action);
                if (buttonTrans != null)
                {
                    Button btn = buttonTrans.GetComponent<Button>();
                    TMP_Text txt = buttonTrans.GetComponentInChildren<TMP_Text>();
                    if (btn != null)
                    {
                        var mapping = new RebindButtonMapping
                        {
                            actionName = action,
                            rebindButton = btn,
                            keyDisplayLabel = txt
                        };
                        buttonMappings.Add(mapping);
                        btn.onClick.AddListener(() => StartRebind(action));
                    }
                }
            }
        }
    }

    private void StartRebind(string actionName)
    {
        activeRebindAction = actionName;
        UpdateUI();
    }

    private void DetectAndApplyRebind()
    {
        KeyCode pressedKey = KeyCode.None;

        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    if (key != KeyCode.Tab && key != KeyCode.Escape)
                    {
                        pressedKey = key;
                        break;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    pressedKey = (KeyCode)((int)KeyCode.Mouse0 + i);
                    break;
                }
            }
        }

        if (pressedKey != KeyCode.None)
        {
            if (inputSettings != null)
            {
                inputSettings.RebindKeyboardKey(activeRebindAction, pressedKey);
            }
            activeRebindAction = null;
            UpdateUI();
        }
    }

    private void ResetSettings()
    {
        if (inputSettings != null)
        {
            inputSettings.ResetToDefaults();
        }
        activeRebindAction = null;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (inputSettings == null || inputSettings.Keyboard == null)
            return;

        var keyboard = inputSettings.Keyboard;

        foreach (var mapping in buttonMappings)
        {
            if (mapping.keyDisplayLabel == null)
                continue;

            if (mapping.actionName == activeRebindAction)
            {
                mapping.keyDisplayLabel.text = "... Press Key ...";
            }
            else
            {
                mapping.keyDisplayLabel.text = GetKeyName(mapping.actionName, keyboard);
            }
        }
    }

    private string GetKeyName(string actionName, KeyboardBindings keyboard)
    {
        KeyCode code = KeyCode.None;
        switch (actionName.ToLower())
        {
            case "forward":
            case "moveforward":
                code = keyboard.moveForward;
                break;
            case "backward":
            case "movebackward":
                code = keyboard.moveBackward;
                break;
            case "left":
            case "moveleft":
                code = keyboard.moveLeft;
                break;
            case "right":
            case "moveright":
                code = keyboard.moveRight;
                break;
            case "dash":
                code = keyboard.dash;
                break;
            case "interact":
                code = keyboard.interact;
                break;
            case "attack":
                code = keyboard.attack;
                break;
            case "skill":
                code = keyboard.skill;
                break;
            case "ultimate":
                code = keyboard.ultimate;
                break;
        }

        return code.ToString();
    }
}
