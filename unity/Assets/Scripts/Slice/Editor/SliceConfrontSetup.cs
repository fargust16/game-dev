using GameDev.Confront.Rules;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S3a.4's wiring of the slice's settings: the fight's numbers, the development kit until S5a's shops (owner,
    /// 2026-11-09: a sword, leather, and a rifle), the development opponents, and the Combat Grid's material. Batch:
    /// -executeMethod GameDev.Slice.EditorTools.SliceConfrontSetup.Configure
    /// </summary>
    public static class SliceConfrontSetup
    {
        public const string SettingsPath = "Assets/Data/Slice/SliceSettings.asset";
        public const string GridMaterialPath = "Assets/Materials/Slice/Confront_Grid.mat";
        private const string Data = "Assets/Data/Confront";

        // Authoring default: a red Human Character Dummy, as the slice's other stand-ins (asset map §1), reads as a
        // development opponent and nothing else.
        private const string OpponentModel = "Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M Red.prefab";

        // Authoring default: a pale, muted line that reads on the setts and the grass by day and under gaslight.
        private static readonly Color GridColour = new Color(0.78f, 0.82f, 0.72f);

        public static void Configure()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SettingsPath);
            var so = new SerializedObject(settings);
            so.FindProperty("fightRules").objectReferenceValue = Load<FightRules>($"{Data}/FightRules.asset");
            so.FindProperty("kitSword").objectReferenceValue = Load<FightWeapon>($"{Data}/Weapons/Sword.asset");
            so.FindProperty("kitRifle").objectReferenceValue = Load<FightWeapon>($"{Data}/Weapons/Rifle.asset");
            so.FindProperty("kitPouch").objectReferenceValue = Load<FightOffHand>($"{Data}/Weapons/CartridgePouch.asset");
            so.FindProperty("kitArmour").objectReferenceValue = Load<FightArmour>($"{Data}/Armour/Leather.asset");
            so.FindProperty("opponentModel").objectReferenceValue = Load<GameObject>(OpponentModel);
            so.FindProperty("opponentWeapon").objectReferenceValue = Load<FightWeapon>($"{Data}/Weapons/Dagger.asset");
            so.FindProperty("gridMaterial").objectReferenceValue = GridMaterial();
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[SliceConfrontSetup] the slice's settings carry the fight's numbers, the development kit, the opponents and the grid's material.");
        }

        private static Material GridMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GridMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, GridMaterialPath);
            }

            material.SetColor("_BaseColor", GridColour);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T Load<T>(string path)
            where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new System.InvalidOperationException($"[SliceConfrontSetup] missing {path}");
            }

            return asset;
        }
    }
}
