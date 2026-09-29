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

        int startIndex = 0;
        while ((startIndex = processed.IndexOf('{', startIndex)) != -1)
        {
            int endIndex = processed.IndexOf('}', startIndex);
            if (endIndex == -1) break;

            string actionName = processed.Substring(startIndex + 1, endIndex - startIndex - 1);
            
            var action = InputSystem.actions.FindAction(actionName);
            if (action != null)
            {
            string bindingName = action.GetBindingDisplayString(
                InputBinding.DisplayStringOptions.DontIncludeInteractions);

            if (bindingName.Contains(" | ")) 
            {
                bindingName = bindingName.Split(" | ")[0];
            }
                
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
