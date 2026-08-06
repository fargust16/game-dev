using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Legacy entry point — forwards to <see cref="CityGenerator"/>.
    /// Prefer menu GameDev/City/Generate From Seed.
    /// </summary>
    public sealed class LowerVeilGreyboxBuilder : MonoBehaviour
    {
        public void Build()
        {
            var city = GetComponent<CityGenerator>();
            if (city == null)
            {
                city = gameObject.AddComponent<CityGenerator>();
            }

            city.SetSeed(WorldSeed.DefaultPrototypeSeed);
            city.Generate();
            Debug.Log("[S0] LowerVeilGreyboxBuilder forwards to CityGenerator. Use GameDev/City/Generate From Seed.");
        }
    }
}
