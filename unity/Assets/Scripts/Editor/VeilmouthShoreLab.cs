using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The water's edge in the island's lab (Documentation/Terrain/TERRAIN_POLISH_REPORT.md §4): the same shores seen
/// with the water's material as it is and with its shore waves and foam turned down or off, to tell what of the
/// stepped lines at the waterline is the ground's and what is the water's own. WaterWorks is not changed: each trial
/// is a copy of the project's material in memory, and the scene is not saved.
/// <para>Batch, with graphics: -executeMethod VeilmouthShoreLab.Run</para>
/// </summary>
public static class VeilmouthShoreLab
{
    private const string ScenePath = "Assets/Scenes/Slice/Lab/IslandLab.unity";
    private const string Out = "Logs/ShoreLab";
    private const float SeaLevel = 24f;

    /// <summary>A trial's name and the material's values it sets (WaterWorks' SSR_Water; the rest stays the project's).</summary>
    private static readonly (string Name, (string Property, float Value)[] Set)[] Trials =
    {
        ("A_as_it_is", new (string, float)[0]),
        ("B_no_foam", new[] { ("_UseFoam", 0f) }),
        ("C_waves_short_and_slow", new[] { ("_WaveDist", 1.5f), ("_WaveFrequency", 1.2f) }),
        ("D_no_foam_no_screen_reflections", new[] { ("_UseFoam", 0f), ("_ScreenSpaceReflections", 0f) }),
    };

    /// <summary>A view by plan metres and metres over the sea; an eye of 0 stands on the ground.</summary>
    private static readonly (string Name, Vector3 Eye, Vector3 Look, float Fov)[] Views =
    {
        ("beach_eye", new Vector3(214f, 372f, 0f), new Vector3(150f, 560f, 0.5f), 60f),
        ("beach_from_30m", new Vector3(250f, 330f, 30f), new Vector3(190f, 420f, 0.5f), 55f),
        ("beach_straight_down", new Vector3(196f, 400f, 45f), new Vector3(197f, 403f, 0.1f), 50f),
        ("cliff_foot", new Vector3(990f, 400f, 14f), new Vector3(905f, 430f, 2f), 55f),
        ("creek_bank_eye", new Vector3(352f, 318f, 0f), new Vector3(300f, 250f, 0.5f), 60f),
        ("estuary_from_60m", new Vector3(770f, 900f, 60f), new Vector3(700f, 780f, 0.5f), 55f),
    };

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var terrain = Object.FindFirstObjectByType<Terrain>();
        var water = GameObject.Find("Water").GetComponent<MeshRenderer>();
        var original = water.sharedMaterial;
        Directory.CreateDirectory(Out);

        var rig = new GameObject("Lab Camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = rig.AddComponent<Camera>();
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 4000f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
        var small = new RenderTexture(960, 540, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
        var shot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var sheets = new Dictionary<string, Texture2D>();

        Vector3 World(Vector3 plan, float standing)
        {
            var x = plan.x - 500f;
            var z = 500f - plan.y;
            var ground = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
            return new Vector3(x, plan.z > 0f ? Mathf.Max(SeaLevel + plan.z, ground + 0.3f) : Mathf.Max(ground, SeaLevel) + standing, z);
        }

        for (var t = 0; t < Trials.Length; t++)
        {
            var material = new Material(original) { hideFlags = HideFlags.HideAndDontSave };
            foreach (var (property, value) in Trials[t].Set)
            {
                if (!material.HasProperty(property))
                {
                    Debug.LogError($"[ShoreLab] the water's material has no {property}");
                }

                material.SetFloat(property, value);
            }

            water.sharedMaterial = material;
            foreach (var (name, eye, look, fov) in Views)
            {
                var from = World(eye, 1.7f);
                camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(World(look, 0f) - from));
                camera.fieldOfView = fov;
                camera.targetTexture = target;
                camera.Render();
                camera.Render(); // the second frame has the first's depth and opaque textures for the water
                RenderTexture.active = target;
                shot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                shot.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"{Out}/{name}_{Trials[t].Name}.jpg", shot.EncodeToJPG(90));

                if (!sheets.TryGetValue(name, out var sheet))
                {
                    sheets[name] = sheet = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                }

                camera.targetTexture = small;
                camera.Render();
                camera.Render();
                RenderTexture.active = small;
                sheet.ReadPixels(new Rect(0, 0, 960, 540), (t % 2) * 960, t < 2 ? 540 : 0);
                RenderTexture.active = null;
            }

            water.sharedMaterial = original;
            Object.DestroyImmediate(material);
        }

        foreach (var pair in sheets)
        {
            pair.Value.Apply();
            File.WriteAllBytes($"{Out}/sheet_{pair.Key}.jpg", pair.Value.EncodeToJPG(90));
        }

        camera.targetTexture = null;
        Object.DestroyImmediate(rig);
        target.Release();
        small.Release();
        Debug.Log($"[ShoreLab] {Trials.Length} trials of {Views.Length} views in {Out}");
    }
}
