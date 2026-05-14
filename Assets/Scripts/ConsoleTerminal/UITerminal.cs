using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UITerminal : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the visual console panel here (ConsolePanel)")]
    public GameObject consolePanel;

    [Tooltip("Drag the TextMeshPro Input Field here")]
    [SerializeField] private TMP_InputField inputField;

    [Tooltip("Drag the TextMeshProUGUI text object used for the log display")]
    [SerializeField] private TextMeshProUGUI logTextDisplay;


    [Header("Settings")]
    [Tooltip("Key to open/close the console. BackQuote is usually the key to the left of 1 (| or ~)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;

    // We store the timescale here so if a command changes it, we restore the new value when closing
    public float cachedTimeScale = 1f;

    private List<string> commandsHistory = new List<string>();
    private int currentHistoryIndex = 0;

    private void Start()
    {
        // For safety, ensure the console is turned off when the game starts
        consolePanel.SetActive(false);

        // Listen to the 'Enter' key being pressed on the Input Field
        inputField.onSubmit.AddListener(OnInputSubmit);
    }

    private void Update()
    {
        // Check if the user pressed the assigned toggle key
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleConsole();
        }

        if (consolePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                ShowCommandHistory(-1);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ShowCommandHistory(1);
            }
        }
    }

    private void OnEnable()
    {
        if (TerminalManager.Instance != null)
            TerminalManager.Instance.OnLogGenerated += ShowLog;
    }

    private void OnDisable()
    {
        if (TerminalManager.Instance != null)
            TerminalManager.Instance.OnLogGenerated -= ShowLog;
    }


    private void ToggleConsole()
    {
        // Toggle the current state. If off, turn it on. If on, turn it off.
        bool isActive = consolePanel.activeSelf;
        consolePanel.SetActive(!isActive);

        // Optional: Pause the game while the console is open
        if (!isActive) // If it was NOT active (meaning it just turned on)
        {
            cachedTimeScale = Time.timeScale; // Save the current time scale
            Time.timeScale = 0f; // Pause the game

            // Auto-focus the input field so the user can type immediately
            inputField.ActivateInputField();
            inputField.Select();
            inputField.text = ""; // Clear any previous text

            if (InputManager.Instance != null)
                InputManager.Instance.enabled = false;

            currentHistoryIndex = commandsHistory.Count;
        }
        else // If it just turned off
        {
            Time.timeScale = cachedTimeScale; // Resume the game with the saved (or modified) speed

            if (InputManager.Instance != null)
                InputManager.Instance.enabled = true;
        }
    }

    private void OnInputSubmit(string inputValue)
    {
        // We send the text to our core manager to process it
        TerminalManager.Instance.ProcessCommand(inputValue);

        // Add the input to history if it's not empty
        if (inputField.text.Trim().Length > 0)
        {
            commandsHistory.Add(inputValue);
            currentHistoryIndex = commandsHistory.Count;
        }

        // Clear the input field and re-focus it so they can type another command
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();
    }

    // Ahora recibimos un -1 o un +1
    private void ShowCommandHistory(int direction)
    {
        if (commandsHistory.Count == 0) return;

        currentHistoryIndex += direction;

        currentHistoryIndex = Mathf.Clamp(currentHistoryIndex, 0, commandsHistory.Count);

        if (currentHistoryIndex == commandsHistory.Count)
        {
            inputField.text = "";
        }
        else
        {
            inputField.text = commandsHistory[currentHistoryIndex];
            inputField.caretPosition = inputField.text.Length;
        }
    }


    private void ShowLog(string message, string color)
    {
        if (logTextDisplay == null) return;

        // Rich text format: <color=value>text</color>
        logTextDisplay.text += $"<color={color}>{message}</color>\n";
    }
}
