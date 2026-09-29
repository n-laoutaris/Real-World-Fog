using Mapbox.BaseModule.Map;
using Mapbox.Example.Scripts.Map;
using Mapbox.LocationModule;
using UnityEngine;

// ORIGIN: Mapbox sample (LocationBasedGame/Scripts), adopted unmodified. Lives on the Map prefab.
//
// WHAT IT DOES:
// Waits for the first GPS fix, then initializes the Mapbox map centered on that coordinate.
// That center becomes Unity's world origin (0,0,0) for the whole session.
//
// IMPORTANT: ContinueAfterInitialization is OFF in our prefab, so the map center never moves
// after startup. That's why other scripts (FogTracker, CharacterMovement) can safely compare
// positions in Unity world space during a session.
namespace Mapbox.Example.Scripts.LocationBehaviours
{
    public class SnapMapToLocationProvider : MonoBehaviour
    {
        public bool InitializeMap = true;
        public bool ContinueAfterInitialization = false;
        [SerializeField]
        private LocationProviderFactory _locationProvider;

        [SerializeField] private MapboxMapBehaviour _map;
        private bool _initializeStarted = false;
        
        private void Start()
        {
        
            if(_locationProvider == null)
                Debug.Log("_locationProvider null");
        
            if(_locationProvider.DefaultLocationProvider == null)
                Debug.Log("DefaultLocationProvider null");
        
            if(!enabled || _locationProvider == null || _locationProvider.DefaultLocationProvider == null)
                return;
        
            UnityEngine.Input.location.Start();
            _locationProvider.DefaultLocationProvider.OnLocationUpdated += (s) =>
            {
                if (_map.InitializationStatus == InitializationStatus.WaitingForInitialization && InitializeMap && !_initializeStarted)
                {
                    _initializeStarted = true;
                    _map.MapInformation.Initialize(s.LatitudeLongitude);
                    _map.Initialize();
                }
                if (ContinueAfterInitialization)
                {
                    _map.MapInformation.SetInformation(s.LatitudeLongitude);
                }
                
            };
        }
    }
}
