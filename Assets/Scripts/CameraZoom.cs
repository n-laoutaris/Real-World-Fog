using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private Camera mapCamera;

    [Header("Zoom Limits")]
    public float minOrthoSize = 300f;
    public float maxOrthoSize = 600f;

    [Header("Zoom Speeds")]
    [Tooltip("How fast the arrow keys zoom in the Editor")]
    public float keyboardZoomSpeed = 200f;
    [Tooltip("How fast pinching zooms on the phone")]
    public float touchPinchSpeed = 0.2f;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        if (mapCamera == null)
            mapCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (mapCamera == null) return;

        float currentSize = mapCamera.orthographicSize;

        // 1. EDITOR CONTROLS: Arrow Keys 
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed)
            {
                // Zoom IN (subtract from size)
                currentSize -= keyboardZoomSpeed * Time.deltaTime;
            }
            else if (Keyboard.current.downArrowKey.isPressed)
            {
                // Zoom OUT (add to size)
                currentSize += keyboardZoomSpeed * Time.deltaTime;
            }
        }

        // 2. MOBILE CONTROLS: Pinch to Zoom
        if (Touch.activeTouches.Count == 2)
        {
            var touch0 = Touch.activeTouches[0];
            var touch1 = Touch.activeTouches[1];

            Vector2 touch0PrevPos = touch0.screenPosition - touch0.delta;
            Vector2 touch1PrevPos = touch1.screenPosition - touch1.delta;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.screenPosition - touch1.screenPosition).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            currentSize -= difference * touchPinchSpeed;
        }

        // 3. Apply the new size, locked between your set limits
        mapCamera.orthographicSize = Mathf.Clamp(currentSize, minOrthoSize, maxOrthoSize);
    }
}