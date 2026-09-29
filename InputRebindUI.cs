using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InputRebindUI : MonoBehaviour
{
    [SerializeField] private string actionName; 
    [SerializeField] private int bindingIndex;   
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private TextMeshProUGUI bindingDisplay;
    [SerializeField] private Button rebindButton;
    [SerializeField] private GameObject waitingForInputOverlay;

    private InputAction action;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    void Awake()
    {
        if (rebindButton != null)
            rebindButton.onClick.AddListener(StartRebinding);
    }

    void OnEnable()
    {
        UpdateUI();
    }

    private void StartRebinding()
    {
        if (InputSystem.actions == null) return;
        
        action = InputSystem.actions.FindAction(actionName);
        if (action == null) return;

        action.Disable();

        rebindButton.interactable = false;
        if (waitingForInputOverlay != null) waitingForInputOverlay.SetActive(true);
        if (bindingDisplay != null) bindingDisplay.text = "Listening...";

        // Start the interactive rebind
        rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Pointer>/position") 
            .WithControlsExcluding("<Pointer>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebinding(operation))
            .OnCancel(operation => FinishRebinding(operation))
            .Start();
    }

    private void FinishRebinding(InputActionRebindingExtensions.RebindingOperation operation)
    {
        operation.Dispose();
        
        if (waitingForInputOverlay != null) waitingForInputOverlay.SetActive(false);
        rebindButton.interactable = true;
        
        action.Enable();
        UpdateUI();

        // Save overrides
        string rebinds = InputSystem.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputRebinds", rebinds);
        PlayerPrefs.Save();
    }

    public void UpdateUI()
    {
        if (InputSystem.actions == null) return;
        action = InputSystem.actions.FindAction(actionName);
        
        if (actionLabel != null) actionLabel.text = actionName;
        
        if (bindingDisplay != null && action != null)
        {
            // Removed the invalid 'IgnoreBindingMask' flag
            var displayString = action.GetBindingDisplayString(bindingIndex, 
                InputBinding.DisplayStringOptions.DontUseShortDisplayNames);

            // Manual cleanup to ensure LMB/RMB/MMB are shown correctly
            displayString = displayString.Replace("Left Button", "LMB");
            displayString = displayString.Replace("Right Button", "RMB");
            displayString = displayString.Replace("Middle Button", "MMB");

            bindingDisplay.text = displayString;
        }
    }
}