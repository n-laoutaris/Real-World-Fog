using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.InputSystem; // The New Input System!

namespace Mapbox.LocationModule
{
    public class WASDLocationProvider : AbstractEditorLocationProvider
    {
        [Header("Starting Location")]
        [Tooltip("Format: Latitude, Longitude")]
        [SerializeField] private string _startLatitudeLongitude;

        [Header("WASD Settings")]
        [Tooltip("Movement speed in roughly real-world meters per second")]
        [SerializeField] private float _moveSpeed = 10f;

        private LatitudeLongitude _latLng;

#if UNITY_EDITOR
        protected override void Awake()
        {
            base.Awake();

            // Convert the starting string into usable coordinates just once at the start
            _latLng = Conversions.StringToLatLon(_startLatitudeLongitude);
        }

        private void Update()
        {
            // Failsafe in case no keyboard is connected
            if (Keyboard.current == null) return;

            // 1 meter is roughly 0.000009 degrees of Latitude (1 degree of latitude is ~111 km everywhere)
            double latDegreesPerSecond = (_moveSpeed * 0.000009) * Time.deltaTime;

            // Longitude lines converge towards the poles, so 1 degree of longitude is only
            // 111 km * cos(latitude) long (~88 km in Athens). To cover the same real meters
            // east-west as north-south, we need MORE degrees: divide by cos(latitude).
            // Without this, WASD moved ~21% slower horizontally than vertically.
            double lngDegreesPerSecond = latDegreesPerSecond / Math.Cos(_latLng.Latitude * Math.PI / 180.0);

            // Modify the mathematical coordinates directly based on key presses
            if (Keyboard.current.wKey.isPressed) _latLng.Latitude += latDegreesPerSecond;
            if (Keyboard.current.sKey.isPressed) _latLng.Latitude -= latDegreesPerSecond;

            if (Keyboard.current.dKey.isPressed) _latLng.Longitude += lngDegreesPerSecond;
            if (Keyboard.current.aKey.isPressed) _latLng.Longitude -= lngDegreesPerSecond;
        }
#endif

        protected override void SetLocation()
        {
            // This is called by the parent's coroutine metronome
            _currentLocation.UserHeading = transform.eulerAngles.y;
            _currentLocation.LatitudeLongitude = _latLng;
            _currentLocation.Accuracy = _accuracy;
            _currentLocation.Timestamp = UnixTimestampUtils.To(DateTime.UtcNow);

            _currentLocation.IsLocationUpdated = true;
            _currentLocation.IsUserHeadingUpdated = true;
            _currentLocation.IsLocationServiceEnabled = true;
        }
    }
}