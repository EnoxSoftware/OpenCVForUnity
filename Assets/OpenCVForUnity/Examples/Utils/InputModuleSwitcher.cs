using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Input Module Switcher that automatically configures the appropriate Input Module based on the input system being used.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public class InputModuleSwitcher : MonoBehaviour
    {
        private void Awake()
        {
            SetupInputSystem();
        }

        private void SetupInputSystem()
        {
            var eventSystem = GetComponent<EventSystem>();
            if (eventSystem == null)
                return;

#if ENABLE_INPUT_SYSTEM
            // Remove old Input Module if it exists
            var oldInput = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldInput != null)
            {
                Destroy(oldInput);
            }

            // Add new Input Module if it doesn't exist
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#endif
        }
    }
}
