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
    [Tooltip("Size of the clear circle in meters")]
    public float revealRadius = 25f;
    [Tooltip("Meters the GPS must move to drop a new point")]
    public float dropDistance = 5f;

    private MapboxMap _map;
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

        // 1. Convert the raw GPS into today's Unity World Space
        Vector3 currentWorldPos = _map.MapInformation.ConvertLatLngToPosition(location.LatitudeLongitude);

        // 2. Simple distance check against our last drop
        if (Vector3.Distance(currentWorldPos, _lastDropPosition) >= dropDistance)
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
        _fogMaterial.SetFloat("_RevealRadius", revealRadius);
    }
}
