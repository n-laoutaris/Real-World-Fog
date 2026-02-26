using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements; 


[RequireComponent(typeof(UIDocument))]
public class UIController : MonoBehaviour
{
    private Button _recenterButton;

    private void OnEnable()
    {
        // 1. Grab the root of the UI Toolkit document
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 2. Find the button by its exact Name in the UI Builder
        _recenterButton = root.Q<Button>("RecenterButton");

        // 3. Subscribe the click event to our Radio Tower
        if (_recenterButton != null)
        {
            _recenterButton.clicked += OnRecenterClicked;
        }
        else
        {
            Debug.LogWarning("RecenterButton not found in the UI Document!");
        }
    }

    private void OnDisable()
    {
        // Always unhook events when disabled to prevent memory leaks!
        if (_recenterButton != null)
        {
            _recenterButton.clicked -= OnRecenterClicked;
        }
    }

    private void OnRecenterClicked()
    {
        // Shout to the camera controller
        CameraEvents.OnRecenterRequested?.Invoke();
    }
}
