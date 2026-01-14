using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// DragRotationController
    /// A component that rotates a target GameObject based on pointer drag input.
    /// The rotation is calculated from the pointer delta movement, allowing intuitive drag-to-rotate interaction.
    /// Supports both mouse and touch input through Unity's Pointer Events system.
    ///
    /// This component uses Pointer Events (IPointerDownHandler, IDragHandler, etc.) to handle input.
    /// For Pointer Events to work correctly, the following setup is required:
    /// - EventSystem must exist in the scene
    /// - Input Module (StandaloneInputModule or InputSystemUIInputModule) must be attached to EventSystem
    /// - GameObject must have a Collider component for raycast detection
    /// - Camera must have PhysicsRaycaster component for 3D object interaction
    /// </summary>
    public class DragRotationController : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        ICancelHandler
    {
        // Public Fields
        public GameObject TargetObject;
        public float Speed = 0.1f;

        // Private Fields
        private bool _isDragging;
        private Vector2 _lastPointerPosition;

        /// <summary>
        /// Handle pointer down to start drag rotation.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsValidPointer(eventData))
            {
                return;
            }

            _isDragging = true;
            _lastPointerPosition = eventData.position;
        }

        /// <summary>
        /// Handle drag to rotate the target object based on pointer delta.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || !IsValidPointer(eventData))
            {
                return;
            }

            Vector2 delta = eventData.delta;

            // Fallback to manual delta if event delta is zero (safety for some platforms)
            if (delta == Vector2.zero)
            {
                delta = eventData.position - _lastPointerPosition;
            }

            float xAngle = delta.y * Speed;
            float yAngle = -delta.x * Speed;
            const float zAngle = 0f;

            if (TargetObject != null)
            {
                TargetObject.transform.Rotate(xAngle, yAngle, zAngle, Space.World);
            }

            _lastPointerPosition = eventData.position;
        }

        /// <summary>
        /// Handle pointer up to end drag rotation.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
        }

        /// <summary>
        /// Handle cancellation (e.g., pointer lost).
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnCancel(BaseEventData eventData)
        {
            _isDragging = false;
        }

        /// <summary>
        /// Validate pointer event.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        /// <returns>True when the pointer should be processed.</returns>
        private bool IsValidPointer(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return false;
            }

            return true;
        }
    }
}
