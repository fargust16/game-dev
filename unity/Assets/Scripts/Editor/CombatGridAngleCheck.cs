using System.Collections.Generic;
using System.IO;
using System.Text;
using GameDev.Grid;
using GameDev.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Does the Combat Grid read a street at 45° as well as one at 0°? (Veilmouth C1 §12, owner 2026-11-05: some streets
/// run at 45° once the grid is checked.) Builds a straight walled street of each width at 0° and at 45° in an empty
/// scene and compares what the grid makes of it: the share of the street's cells a person can stand on, whether one end
/// reaches the other, the moves that takes against the straight distance, and how many cells along the walls get cover.
/// Writes Logs/GridAngle/report.txt. Batch: -executeMethod CombatGridAngleCheck.Run
/// </summary>
public static class CombatGridAngleCheck
{
    private const float FloorY = 30f;
    private const float Length = 28f;
    private const float WallHeight = 3f;
    private const float WallThickness = 0.5f;
    private static readonly float[] Widths = { 2f, 3f, 4f, 6f, 12f };
    private static readonly float[] Angles = { 0f, 45f, 30f };

    [MenuItem("GameDev/Grid/Check Streets at 45°")]
    public static void Run()
    {
        var text = new StringBuilder();
        text.AppendLine("Combat Grid on a walled street, 1 m cells, person radius " + WorldScale.CharacterRadiusMeters + " m");
        text.AppendLine("width  angle  place  street cells  standable  ends joined  moves/straight  wall cells  with cover");
        foreach (var width in Widths)
        {
            foreach (var angle in Angles)
            {
                // On the kit's grid: at 0° the walls' faces lie on 1 m grid lines (kit standard §1); at 45° and 30°
                // the street's middle line passes through a grid node. Then the same street off the grid.
                var onGrid = new Vector3(angle == 0f && width % 2f != 0f ? 0.5f : 0f, FloorY, 0f);
                text.AppendLine(Measure(width, angle, onGrid, "grid"));
                text.AppendLine(Measure(width, angle, new Vector3(0.37f, FloorY, 0.61f), "off"));
            }
        }

        Directory.CreateDirectory("Logs/GridAngle");
        File.WriteAllText("Logs/GridAngle/report.txt", text.ToString());
        Debug.Log("GRIDANGLE\n" + text);
    }

    private static string Measure(float width, float angle, Vector3 centre, string place)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rotation = Quaternion.Euler(0f, angle, 0f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(0f, FloorY - 0.5f, 0f);
        floor.transform.localScale = new Vector3(80f, 1f, 80f);
        for (var side = -1; side <= 1; side += 2)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetPositionAndRotation(
                centre + rotation * new Vector3(side * (width + WallThickness) * 0.5f, WallHeight * 0.5f, 0f), rotation);
            wall.transform.localScale = new Vector3(WallThickness, WallHeight, Length);
        }

        Physics.SyncTransforms();
        var reader = new CombatGridReader(1f);
        reader.TryFindStart(centre + Vector3.up * 0.1f, out var cx, out var cz, out var level);
        var window = reader.Build(cx, cz, level);

        var inverse = Quaternion.Inverse(rotation);
        var radius = WorldScale.CharacterRadiusMeters;
        var half = width * 0.5f;
        var street = 0;
        var standable = 0;
        var wallCells = 0;
        var covered = 0;
        var nearEnd = new List<(int, int)>();
        var farEnd = new List<(int, int)>();
        for (var dz = -CombatGridReader.Reach; dz <= CombatGridReader.Reach; dz++)
        {
            for (var dx = -CombatGridReader.Reach; dx <= CombatGridReader.Reach; dx++)
            {
                var ix = cx + dx;
                var iz = cz + dz;
                var local = inverse * (reader.Center(ix, iz, FloorY) - centre);
                if (Mathf.Abs(local.x) > half - radius || Mathf.Abs(local.z) > Length * 0.5f - 2f)
                {
                    continue;
                }

                street++;
                var column = reader.Column(ix, iz);
                var floorLevel = -1;
                for (var l = 0; l < column.Length; l++)
                {
                    if (Mathf.Abs(column[l].Y - FloorY) < 0.05f && column[l].Standable)
                    {
                        floorLevel = l;
                    }
                }

                if (floorLevel < 0)
                {
                    continue;
                }

                standable++;
                var reached = window.Reachable.Contains(CombatGridReader.Pack(ix, iz, floorLevel));
                if (reached && local.z < -Length * 0.5f + 4f)
                {
                    nearEnd.Add((ix, iz));
                }

                if (reached && local.z > Length * 0.5f - 4f)
                {
                    farEnd.Add((ix, iz));
                }

                // Beside the wall: a side neighbour lies outside the street, in or beyond the wall.
                var beside = false;
                for (var d = 0; d < 4; d++)
                {
                    var n = inverse * (reader.Center(ix + (d == 0 ? 1 : d == 1 ? -1 : 0), iz + (d == 2 ? 1 : d == 3 ? -1 : 0), FloorY) - centre);
                    beside |= Mathf.Abs(n.x) > half - radius;
                }

                if (beside)
                {
                    wallCells++;
                    var cover = reader.Cover(ix, iz, floorLevel);
                    covered += cover.Low != 0 || cover.High != 0 ? 1 : 0;
                }
            }
        }

        var joined = nearEnd.Count > 0 && farEnd.Count > 0;
        var ratio = joined ? Moves(reader, window, nearEnd, farEnd, FloorY) : 0f;
        return $"{width,5:0}  {angle,5:0}  {place,5}  {street,12}  {Share(standable, street),9}  {(joined ? "yes" : "NO"),11}  {(joined ? ratio.ToString("0.00") : "-"),14}  {wallCells,10}  {Share(covered, wallCells),10}";
    }

    /// <summary>Fewest grid moves (four sides and diagonals) from one end to the other, over the straight distance.</summary>
    private static float Moves(CombatGridReader reader, CombatGridWindow window, List<(int, int)> from, List<(int, int)> to, float y)
    {
        var targets = new HashSet<(int, int)>(to);
        var steps = new Dictionary<(int, int), int>();
        var queue = new Queue<(int, int)>();
        foreach (var cell in from)
        {
            steps[cell] = 0;
            queue.Enqueue(cell);
        }

        while (queue.Count > 0)
        {
            var (x, z) = queue.Dequeue();
            if (targets.Contains((x, z)))
            {
                var a = reader.Center(from[0].Item1, from[0].Item2, y);
                var b = reader.Center(x, z, y);
                return steps[(x, z)] / Mathf.Max(1f, Vector3.Distance(a, b));
            }

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dz = -1; dz <= 1; dz++)
                {
                    var next = (x + dx, z + dz);
                    if ((dx == 0 && dz == 0) || steps.ContainsKey(next) || !Reached(window, reader, next.Item1, next.Item2, y))
                    {
                        continue;
                    }

                    steps[next] = steps[(x, z)] + 1;
                    queue.Enqueue(next);
                }
            }
        }

        return 0f;
    }

    private static bool Reached(CombatGridWindow window, CombatGridReader reader, int ix, int iz, float y)
    {
        var column = reader.Column(ix, iz);
        for (var l = 0; l < column.Length; l++)
        {
            if (Mathf.Abs(column[l].Y - y) < 0.05f && window.Reachable.Contains(CombatGridReader.Pack(ix, iz, l)))
            {
                return true;
            }
        }

        return false;
    }

    private static string Share(int part, int whole) => whole == 0 ? "-" : $"{100f * part / whole:0}%";
}
