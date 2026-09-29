using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using UnityEngine;

// ORIGIN: Mapbox sample (LocationBasedGame/CharacterVisuals/Astronaut), adopted and modified.
//
// WHAT IT DOES:
// Moves the player's avatar (the astronaut) towards an invisible "Target" transform.
// The Target is teleported to every new GPS fix by SnapTransformToLocationProvider,
// so this script's job is purely visual: make the avatar smoothly walk to where the
// GPS says we are, instead of jumping around.
//
// MODIFICATIONS:
// - Teleport threshold: if the Target is very far away (e.g. after the phone was locked
//   and the app paused), snap there instantly instead of "moonwalking" across the map.
// - Speed is now per second (Time.deltaTime) instead of per frame, so the avatar moves
//   at the same speed on a 30fps phone and a 60fps editor.
// - Tunable distances are in real-world meters (see _unitsPerMeter below).
namespace Mapbox.Examples
{
	public class CharacterMovement : MonoBehaviour
	{
		public MapBehaviourCore MapBehaviour;
		private IMapInformation _mapInformation;
		public Transform Target;
		public Animator CharacterAnimator;

		[Tooltip("Walking speed of the avatar, in real meters per second")]
		public float SpeedMetersPerSecond = 10f;

		[Tooltip("If the GPS position is further away than this (real meters), snap instantly instead of walking")]
		public float TeleportDistanceMeters = 20f;

		// How many Unity units one real-world meter takes up on this map.
		// Unity units are Web Mercator meters divided by the map scale, and Web Mercator
		// stretches the world by 1 / cos(latitude). So: units = meters / (cos(lat) * scale).
		// In Athens with scale 1 this is ~1.27 (1 real meter = 1.27 Unity units).
		private float _unitsPerMeter;
		private bool _readyForUpdates = false;

		public bool SnapToTerrain = false;

		private void Start()
		{
			// The map isn't ready at Start, so wait for Mapbox to tell us it's initialized
			MapBehaviour.Initialized += map =>
			{
				_mapInformation = map.MapInformation;
				_unitsPerMeter = 1f / (_mapInformation.GetLatitudeCompensationForLocation * _mapInformation.Scale);
				_readyForUpdates = true;
			};
		}

		void Update()
		{
			if (!_readyForUpdates)
				return;

			// Direction to the target, flattened onto the ground (ignore any height difference)
			var direction = Vector3.ProjectOnPlane(Target.position - transform.position, Vector3.up);
			var distance = direction.magnitude;

			if (distance > TeleportDistanceMeters * _unitsPerMeter)
			{
				// Too far to walk believably (app was asleep, or GPS jumped): snap straight there.
				// Keep our own Y so the avatar doesn't change height.
				transform.position = new Vector3(Target.position.x, transform.position.y, Target.position.z);
			}
			else if (distance > 1 * _unitsPerMeter)
			{
				// Walk towards the target. Multiplying by Time.deltaTime (seconds since last frame)
				// turns "units per frame" into "units per second", making speed framerate-independent.
				// Mathf.Min stops us from overshooting the target on a slow frame.
				var step = SpeedMetersPerSecond * _unitsPerMeter * Time.deltaTime;
				transform.LookAt(transform.position + direction);
				transform.Translate(Vector3.forward * Mathf.Min(step, distance));
				if(CharacterAnimator) CharacterAnimator.SetBool("IsWalking", true);
			}
			else
			{
				// Within 1 meter of the target: close enough, stand still
				if(CharacterAnimator) CharacterAnimator.SetBool("IsWalking", false);
			}

			// Unused for now: we run the map with flat terrain. Kept from the Mapbox sample.
			if (SnapToTerrain)
			{
				var latlng = _mapInformation.ConvertPositionToLatLng(this.transform.position);
				var tileId = Conversions.LatitudeLongitudeToTileId(latlng, 16).Canonical;

				//changed this part and haven't tested... (original Mapbox comment)
				var tileSpace = Conversions.LatitudeLongitudeToInTile01(latlng, tileId);
				var elevation = _mapInformation.QueryElevation(tileId, tileSpace.x, tileSpace.y);
				transform.position = new Vector3(transform.position.x, elevation, transform.position.z);
			}
		}
	}
}
