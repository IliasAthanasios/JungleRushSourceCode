using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Use {ActionName} to insert the current key. Example: 'Press {Jump} to Jump!'")]
    [TextArea(3, 10)]
    public string message = "Press {Jump} to Jump!";
    public float displayDuration = 5f;
    public bool triggerOnlyOnce = true;

    private bool hasBeenTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered && triggerOnlyOnce) return;

        if (other.CompareTag("Player"))
        {
            if (TutorialManager.Instance != null)
            {
                string finalMessage = ProcessMessage(message);
                TutorialManager.Instance.ShowTutorial(finalMessage, displayDuration);
                hasBeenTriggered = true;
            }
        }
    }

    private string ProcessMessage(string rawMessage)
    {
        string processed = rawMessage;

        // Find all text inside curly brackets { }
        int startIndex = 0;
        while ((startIndex = processed.IndexOf('{', startIndex)) != -1)
        {
            int endIndex = processed.IndexOf('}', startIndex);
            if (endIndex == -1) break;

            // Extract the action name (e.g., "Jump")
            string actionName = processed.Substring(startIndex + 1, endIndex - startIndex - 1);
            
            // Look up the current binding for that action
            var action = InputSystem.actions.FindAction(actionName);
            if (action != null)
            {
                // Get the readable name of the key (e.g., "Space" or "J")
                // This version picks the best key for the device the player is currently touching
            string bindingName = action.GetBindingDisplayString(
                InputBinding.DisplayStringOptions.DontIncludeInteractions);

            // If it still contains a '|', we just take the first part
            if (bindingName.Contains(" | ")) 
            {
                bindingName = bindingName.Split(" | ")[0];
            }
                
                // Replace {Jump} with "Space"
                processed = processed.Remove(startIndex, (endIndex - startIndex) + 1);
                processed = processed.Insert(startIndex, bindingName);
                
                startIndex += bindingName.Length;
            }
            else
            {
                startIndex = endIndex + 1;
            }
        }

        return processed;
    }
}