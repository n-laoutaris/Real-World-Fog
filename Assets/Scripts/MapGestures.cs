using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// WHAT IT DOES:
// All map camera gestures in one place: one-finger pan, two-finger pinch zoom, and both at once
// (Google Maps style). In the editor: mouse drag pans, scroll wheel zooms at the cursor,
// arrow keys zoom around the screen center.
//
// THE ONE RULE: "the map point under your fingers stays under your fingers."
// Every frame we find the ground point that was under the fingers' center LAST frame (the anchor),
// apply the zoom, then move the camera so that the anchor sits under the fingers' center THIS frame.
// Pan, zoom and pan-while-zooming all fall out of that single calculation (see MoveCamera).
//
// WHY ONE SCRIPT: pan and zoom must read the same fingers in the same frame. When they were two
// separate scripts, pan followed only the first finger and zoom pivoted on the screen center,
// so they fought each other during a pinch.
//
// SETUP: sits on CameraRoot (a ground-level object). MapCamera is its child, looking straight down,
// orthographic. Moving CameraRoot pans; changing orthographicSize zooms.
public class MapGestures : MonoBehaviour
{
    [Tooltip("The camera that looks at the map (usually a child of this object)")]
    [SerializeField] private Camera mapCamera;

    [Header("Zoom Limits")]
    [Tooltip("Orthographic size = half the screen height, in Unity units. Deliberately narrow: " +
             "tiles only load within a fixed radius, so zooming out further shows empty map.")]
    public float minOrthoSize = 100f;
    public float maxOrthoSize = 600f;

    [Header("Editor Controls")]
    [Tooltip("Arrow keys: how fast the orthographic size changes, in units per second")]
    public float keyboardZoomSpeed = 200f;
    [Tooltip("Scroll wheel: fraction of zoom change per scroll notch (0.1 = 10%)")]
    public float scrollZoomStep = 0.1f;

    // An invisible mathematical plane at Y=0 (the ground), used to turn screen pixels into map positions
    private Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

    private void OnEnable()
    {
        // EnhancedTouch gives us Touch.activeTouches: every finger with its position AND its movement
        // (delta) since last frame. It must be switched on explicitly.
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        // Auto-grab the camera if we forgot to assign it in the inspector
        if (mapCamera == null)
            mapCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (Touch.activeTouches.Count > 0)
            HandleTouches();
        else
            HandleMouseAndKeyboard();
    }

    private void HandleTouches()
    {
        var touches = Touch.activeTouches;

        // 1. Center of all fingers, this frame and last frame.
        //    "Last frame" = current position minus this frame's movement (delta). Because we rebuild
        //    both from the SAME set of fingers every frame, adding or lifting a finger never makes
        //    the map jump (there's no stored "drag origin" that can go stale).
        Vector2 currCenter = Vector2.zero;
        Vector2 prevCenter = Vector2.zero;
        foreach (var touch in touches)
        {
            currCenter += touch.screenPosition;
            prevCenter += touch.screenPosition - touch.delta;
        }
        currCenter /= touches.Count;
        prevCenter /= touches.Count;

        // 2. Zoom ratio from the distance between the first two fingers.
        //    Proportional, not additive: fingers twice as far apart -> map twice as big
        //    (orthographic size halves). That's what makes the map feel glued to your fingers.
        float zoomRatio = 1f;
        if (touches.Count >= 2)
        {
            Vector2 curr0 = touches[0].screenPosition;
            Vector2 curr1 = touches[1].screenPosition;
            float prevSpread = Vector2.Distance(curr0 - touches[0].delta, curr1 - touches[1].delta);
            float currSpread = Vector2.Distance(curr0, curr1);
            if (currSpread > 0f)
                zoomRatio = prevSpread / currSpread;
        }

        MoveCamera(prevCenter, currCenter, zoomRatio);
    }

    private void HandleMouseAndKeyboard()
    {
        // MOUSE: drag = pan, scroll = zoom at the cursor (same math as a finger / a pinch)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 curr = mouse.position.ReadValue();
            Vector2 prev = curr - mouse.delta.ReadValue();

            // Skip the press frame itself: its delta is movement from BEFORE the button went down
            if (mouse.leftButton.isPressed && !mouse.leftButton.wasPressedThisFrame)
                MoveCamera(prev, curr, 1f);

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) MoveCamera(curr, curr, 1f - scrollZoomStep); // scroll up = zoom in
            if (scroll < 0f) MoveCamera(curr, curr, 1f + scrollZoomStep); // scroll down = zoom out
        }

        // KEYBOARD: arrow keys zoom around the screen center.
        // Zooming around the exact center never moves the camera, so this doesn't break "follow" mode.
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float sizeChange = 0f;
            if (keyboard.upArrowKey.isPressed) sizeChange -= keyboardZoomSpeed * Time.deltaTime;   // zoom IN
            if (keyboard.downArrowKey.isPressed) sizeChange += keyboardZoomSpeed * Time.deltaTime; // zoom OUT

            if (sizeChange != 0f)
            {
                Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                float size = mapCamera.orthographicSize;
                MoveCamera(screenCenter, screenCenter, (size + sizeChange) / size);
            }
        }
    }

    /// <summary>
    /// The core of every gesture. Keeps the map point that was under prevScreen (last frame)
    /// underneath currScreen (this frame), while scaling the zoom by zoomRatio.
    /// zoomRatio: 1 = no zoom, below 1 = zoom in, above 1 = zoom out.
    /// </summary>
    private void MoveCamera(Vector2 prevScreen, Vector2 currScreen, float zoomRatio)
    {
        // 1. Which map point was under the fingers? (measured with the camera as it was)
        Vector3 anchor = ScreenToGround(prevScreen);

        // 2. Zoom. Unity updates the camera's projection immediately, so the next
        //    ScreenToGround call already "sees" the new zoom level.
        float newSize = mapCamera.orthographicSize * zoomRatio;
        mapCamera.orthographicSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);

        // 3. Which map point is under the fingers NOW (new zoom, old camera position)?
        //    The difference is exactly how far the camera must move to put the anchor back under them.
        Vector3 offset = anchor - ScreenToGround(currScreen);
        transform.position += offset;

        // 4. If the camera actually moved, the user took control: tell CameraFollow to stop following.
        //    This fires every frame of a gesture, which is harmless (it just sets a state again),
        //    and a simple tap (no movement) doesn't count as taking control.
        if (offset != Vector3.zero)
            CameraEvents.OnManualPanBegan?.Invoke();
    }

    /// <summary>
    /// Converts a screen position (pixels) into a world position on the flat map plane (Y=0),
    /// by shooting a ray from the camera through that pixel and seeing where it hits the ground.
    /// </summary>
    private Vector3 ScreenToGround(Vector2 screenPos)
    {
        Ray ray = mapCamera.ScreenPointToRay(screenPos);
        _groundPlane.Raycast(ray, out float distance);
        return ray.GetPoint(distance);
    }
}
