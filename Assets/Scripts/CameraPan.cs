using UnityEngine;
using UnityEngine.InputSystem; 

public class CameraPan : MonoBehaviour
{
    [Tooltip("The camera that looks at the map (usually a child of this object)")]
    [SerializeField] private Camera mapCamera;

    private Vector3 _dragOrigin;
    private bool _isDragging;

    // An invisible mathematical plane at Y=0 (the ground) to calculate perfect 1:1 dragging
    private Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

    private void Start()
    {
        // Auto-grab the camera if we forgot to assign it in the inspector
        if (mapCamera == null)
            mapCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        // Ensure there is a mouse or touchscreen active
        if (Pointer.current == null) return;

        // 1. TOUCH BEGAN (or Mouse Click)
        if (Pointer.current.press.wasPressedThisFrame)
        {
            _dragOrigin = GetWorldPosition(Pointer.current.position.ReadValue());
            _isDragging = true;

            // BROADCAST: Tell the system the user took control!
            CameraEvents.OnManualPanBegan?.Invoke();
        }

        // 2. TOUCH MOVED (or Mouse Dragged)
        else if (Pointer.current.press.isPressed && _isDragging)
        {
            Vector3 currentWorldPos = GetWorldPosition(Pointer.current.position.ReadValue());

            // Calculate the difference between where we started dragging and where we are now
            Vector3 difference = _dragOrigin - currentWorldPos;

            // Move the Camera Root by that difference
            transform.position += difference;
        }

        // 3. TOUCH ENDED (or Mouse Released)
        else if (Pointer.current.press.wasReleasedThisFrame)
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// Converts a screen position (pixels) into a world position on the flat map plane (Y=0).
    /// </summary>
    private Vector3 GetWorldPosition(Vector2 screenPos)
    {
        Ray ray = mapCamera.ScreenPointToRay(screenPos);

        if (_groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero; // Fallback
    }
}