using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("The player or object to follow")]
    public Transform target;

    [Tooltip("How fast the camera glides back to the player")]
    public float smoothSpeed = 5f;

    [Tooltip("How close the camera needs to be before it locks on tightly")]
    public float snapThreshold = 0.1f;

    // We use a simple state machine to track what the camera is doing
    private enum FollowState { NotFollowing, Transitioning, Locked }
    private FollowState _currentState = FollowState.Locked; // Start locked on

    private void OnEnable()
    {
        CameraEvents.OnManualPanBegan += StopFollowing;
        CameraEvents.OnRecenterRequested += StartFollowing;
    }

    private void OnDisable()
    {
        CameraEvents.OnManualPanBegan -= StopFollowing;
        CameraEvents.OnRecenterRequested -= StartFollowing;
    }

    private void StopFollowing()
    {
        _currentState = FollowState.NotFollowing;
    }

    private void StartFollowing()
    {
        if (_currentState != FollowState.Locked)
        {
            _currentState = FollowState.Transitioning;
        }
    }

    // LateUpdate runs after the player has already moved this frame
    private void LateUpdate()
    {
        if (_currentState == FollowState.NotFollowing || target == null) return;

        if (_currentState == FollowState.Transitioning)
        {
            // 1. Smoothly glide towards the target
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * smoothSpeed);

            // 2. Check if we are close enough to lock on
            if (Vector3.Distance(transform.position, target.position) < snapThreshold)
            {
                _currentState = FollowState.Locked;
            }
        }
        else if (_currentState == FollowState.Locked)
        {
            // 3. Hard snap to keep up with fast movement perfectly
            transform.position = target.position;
        }
    }
}