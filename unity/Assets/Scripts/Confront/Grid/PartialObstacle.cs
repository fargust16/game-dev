using UnityEngine;

namespace GameDev.Confront.Grid
{
    /// <summary>
    /// 10.1 *Shot Line*: marks a collider as a Partial Obstacle, a bush, a hedge, haze, smoke or Пелена: a targeted attack
    /// whose line crosses it, or whose target stands in it, is Obscured, and it never stops a line. The collider is a
    /// trigger on the NoGrid layer, so the grid, walkers and cover never read it (<see cref="ShotTrace"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartialObstacle : MonoBehaviour
    {
    }
}
