using UnityEngine;

// ORIGIN: Mapbox sample (LocationBasedGame/CharacterVisuals), adopted unmodified.
//
// WHAT IT DOES:
// Rotates the camera rig around the vertical axis when you drag horizontally (touch or mouse).
//
// STATUS: DISABLED in MainScene. It uses the OLD input API (UnityEngine.Input), but the project
// is set to "Input System Package (New)" only, so enabling it would throw errors every frame.
// Kept for the Phase 0.5 touch-controls work (port to the new Input System, or delete).
namespace Mapbox.Example.Scripts.MapInput
{
    public class CameraRotate : MonoBehaviour
    {
        private Vector2 _previousPosition;
        public float Speed = 1;
        private void Update()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    _previousPosition = touch.position;
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 currentPosition = touch.position;
                    var delta = currentPosition - _previousPosition;
                    transform.Rotate(Vector3.up, delta.x * Speed);
                    _previousPosition = currentPosition;
                }
            }
            else
            {
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    _previousPosition = UnityEngine.Input.mousePosition;
                }

                if (UnityEngine.Input.GetMouseButton(0))
                {
                    Vector2 currentPosition = UnityEngine.Input.mousePosition;
                    var delta = currentPosition - _previousPosition;
                    transform.Rotate(Vector3.up, delta.x * Speed);
                    _previousPosition = currentPosition;
                }
            }
        }
    }
}
