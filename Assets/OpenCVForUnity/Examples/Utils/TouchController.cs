using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace OpenCVForUnityExample
{
    public class TouchController : MonoBehaviour
    {
        public GameObject Cube;
        public float Speed = 0.1f;

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }
#endif

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // New Input System
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            // Touch input for mobile platforms
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 1)
            {
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];

                // Ignore touch on UI elements
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.finger.index))
                    return;

                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        float xAngle = touch.delta.y * Speed;
                        float yAngle = -touch.delta.x * Speed;
                        float zAngle = 0f;

                        Cube.transform.Rotate(xAngle, yAngle, zAngle, Space.World);
                        break;
                }
            }
#else
            // Mouse input for non-mobile platforms
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                var delta = mouse.delta.ReadValue();
                float xAngle = delta.y * Speed;
                float yAngle = -delta.x * Speed;
                float zAngle = 0;

                Cube.transform.Rotate(xAngle, yAngle, zAngle, Space.World);
            }
#endif
#else
            // Old Input System
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            // Touch input for mobile platforms
            int touchCount = Input.touchCount;

            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if (EventSystem.current.IsPointerOverGameObject(t.fingerId))
                    return;

                switch (t.phase)
                {
                case TouchPhase.Moved:
                    float xAngle = t.deltaPosition.y * Speed;
                    float yAngle = -t.deltaPosition.x * Speed;
                    float zAngle = 0;

                    Cube.transform.Rotate(xAngle, yAngle, zAngle, Space.World);
                    break;
                }
            }
#else
            // Mouse input for non-mobile platforms
            if (Input.GetMouseButton(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                float xAngle = Input.GetAxis("Mouse Y") * Speed * 80;
                float yAngle = -Input.GetAxis("Mouse X") * Speed * 80;
                float zAngle = 0;

                Cube.transform.Rotate(xAngle, yAngle, zAngle, Space.World);
            }
#endif
#endif
        }
    }
}
