using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Scene-resident holder for the current <see cref="WorldDefinition"/>.
    /// Editor menus and World Generator window read/write this instead of re-deriving plans.
    /// </summary>
    public sealed class OverworldWorldState : MonoBehaviour
    {
        public const string ObjectName = "OverworldWorldState";

        public WorldDefinition Definition { get; private set; }

        public static OverworldWorldState FindOrCreate()
        {
            var existing = Object.FindAnyObjectByType<OverworldWorldState>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(ObjectName);
            return go.AddComponent<OverworldWorldState>();
        }

        public static OverworldWorldState Find() =>
            Object.FindAnyObjectByType<OverworldWorldState>();

        public void SetDefinition(WorldDefinition definition)
        {
            Definition = definition;
        }

        public void Clear()
        {
            Definition = null;
        }
    }
}
