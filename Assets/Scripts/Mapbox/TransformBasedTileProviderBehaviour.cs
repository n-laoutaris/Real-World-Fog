using Mapbox.UnityMapService.TileProviders;

// ORIGIN: Mapbox sample (LocationBasedGame/Scripts), adopted unmodified. Lives on the Map prefab.
//
// WHAT IT DOES:
// Tells Mapbox WHICH map tiles to load: the ones around a given Transform (the player).
// As the player moves, tiles near them load and far ones unload. If no Transform is assigned,
// it uses the GameObject this script sits on.
namespace Mapbox.Example.Scripts.TileProviderBehaviours
{
    public class TransformBasedTileProviderBehaviour : TileProviderBehaviour
    {
        public TransformBasedTileProvider TileProvider;
        public override TileProvider Core
        {
            get
            {
                if (TileProvider.Transform == null)
                    TileProvider.Transform = transform;
                return TileProvider;
            }
        }
    }
}