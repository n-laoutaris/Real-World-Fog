using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using Mapbox.LocationModule;
using Mapbox.Utils;
using UnityEngine;

public class FogTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapBehaviourCore _mapCore;
    [SerializeField] private LocationProviderFactory _locationProvider;
    [SerializeField] private Material _fogMaterial;

    [Header("Fog Settings")]
    [Tooltip("Size of the clear circle, in real meters")]
    public float revealRadius = 25f;
    [Tooltip("Real meters the GPS must move to drop a new point")]
    public float dropDistance = 5f;
    [Tooltip("Ignore GPS fixes whose accuracy (in meters) is worse than this. Filters out indoor drift.")]
    public float maxAccuracy = 10f;

    private MapboxMap _map;

    // How many Unity units one real-world meter takes up on this map.
    // Unity units are Web Mercator meters divided by the map scale, and Web Mercator
    // stretches the world by 1 / cos(latitude). So: units = meters / (cos(lat) * scale).
    // In Athens with scale 1 this is ~1.27 (1 real meter = 1.27 Unity units).
    // The map center is fixed for the whole session, so we compute this once.
    private float _unitsPerMeter;
    private List<LatitudeLongitude> _savedPoints = new List<LatitudeLongitude>();

    // Track the last dropped position in World Space for a cheap, easy distance check
    private Vector3 _lastDropPosition = Vector3.positiveInfinity;
    private Vector4[] _shaderData = new Vector4[100];

    private void Start()
    {
        if (!enabled || _locationProvider == null || _mapCore == null) return;

        // Wait for Mapbox to initialize, exactly like the sample script
        _mapCore.Initialized += (map) =>
        {
            _map = map;
            _unitsPerMeter = 1f / (map.MapInformation.GetLatitudeCompensationForLocation * map.MapInformation.Scale);
            if (_locationProvider.DefaultLocationProvider != null)
            {
                _locationProvider.DefaultLocationProvider.OnLocationUpdated += OnLocationUpdated;
            }
        };
    }

    private void OnDestroy()
    {
        // Clean up our listener when the object is destroyed
        if (_locationProvider != null && _locationProvider.DefaultLocationProvider != null)
        {
            _locationProvider.DefaultLocationProvider.OnLocationUpdated -= OnLocationUpdated;
        }
    }

    private void OnLocationUpdated(Location location)
    {
        if (_map == null || _map.Status < InitializationStatus.ReadyForUpdates) return;

        // 0. Drop inaccurate fixes. Accuracy is the radius (meters) of the circle the phone is
        //    ~68% sure we're inside. Indoors (signal bouncing off walls, or Wi-Fi/cell fallback)
        //    it grows to 20-100m and the position wanders, which would clear fog we never walked.
        //    Note: the editor's WASD provider reports 0, so it always passes.
        if (location.Accuracy > maxAccuracy) return;

        // 1. Convert the raw GPS into today's Unity World Space
        Vector3 currentWorldPos = _map.MapInformation.ConvertLatLngToPosition(location.LatitudeLongitude);

        // 2. Simple distance check against our last drop
        //    (world-space distance is fine: the map center never moves during a session)
        if (Vector3.Distance(currentWorldPos, _lastDropPosition) >= dropDistance * _unitsPerMeter)
        {
            // 3. Save the pure GPS coordinate for future serialization
            _savedPoints.Add(location.LatitudeLongitude);
            _lastDropPosition = currentWorldPos;

            // 4. Update the visual fog
            UpdateShader();
        }
    }

    private void UpdateShader()
    {
        // Cap the loop at our GPU array limit
        int count = Mathf.Min(_savedPoints.Count, 100);
        int startIndex = _savedPoints.Count - count;

        for (int i = 0; i < count; i++)
        {
            // Translate the saved history back to today's local Unity coordinates
            Vector3 pos = _map.MapInformation.ConvertLatLngToPosition(_savedPoints[startIndex + i]);
            _shaderData[i] = new Vector4(pos.x, pos.y, pos.z, 0);
        }

        // Push to the GPU material
        _fogMaterial.SetVectorArray("_RevealedPoints", _shaderData);
        _fogMaterial.SetInt("_PointCount", count);
        // The shader works in Unity units, so convert the radius from meters
        _fogMaterial.SetFloat("_RevealRadius", revealRadius * _unitsPerMeter);
    }
}
