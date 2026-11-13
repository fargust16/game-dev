using UnityEngine;

namespace GameDev.Confront.Grid
{
    /// <summary>
    /// 05.1 Пелена in a fight (CR5): a cloud of haze over a square of cells, a Partial Obstacle that Obscures a shot
    /// through it or at one in it and, as a bush, spares a shot from inside it (10.1 *Shot Line*; owner, COMBAT-12 (d)).
    /// It is a trigger on the NoGrid layer, so the grid, walkers and cover never read it; the fight raises it and clears it.
    /// </summary>
    public static class Haze
    {
        /// <summary>Authoring default, m: the cloud's height over the ground, above a standing person's head point.</summary>
        public const float Height = 2.4f;

        /// <summary>A cloud <paramref name="width"/> m square standing on <paramref name="ground"/>.</summary>
        public static GameObject Raise(Vector3 ground, float width)
        {
            var haze = new GameObject("Haze") { layer = ShotTrace.NoGridLayer };
            haze.transform.position = ground + (Vector3.up * (Height * 0.5f));
            var box = haze.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, Height, width);
            haze.AddComponent<PartialObstacle>();
            Physics.SyncTransforms();
            return haze;
        }

        /// <summary>The cloud is gone at once for every line, and its object with whatever the scene hung on it.</summary>
        public static void Clear(GameObject haze)
        {
            if (haze == null)
            {
                return;
            }

            if (haze.TryGetComponent<Collider>(out var box))
            {
                box.enabled = false;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(haze);
            }
            else
            {
                Object.DestroyImmediate(haze);
            }

            Physics.SyncTransforms();
        }
    }
}
