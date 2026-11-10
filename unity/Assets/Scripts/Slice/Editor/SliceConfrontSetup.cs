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

        // The Cult Forces' model, the owner's detective (owner, 2026-11-10; asset map §1).
        private const string OpponentModel = SlicePeopleModels.Folder + "/Detective.prefab";

        // Authoring default: a pale, muted line that reads on the setts and the grass by day and under gaslight, half
        // see-through since S3a.5's look check, where opaque lines over the 41 by 41 cells drowned the street.
        private static readonly Color GridColour = new Color(0.78f, 0.82f, 0.72f, 0.5f);
        private const string GridShader = "GameDev/Combat Grid Line";

        // S3a.5: Basic RPG Cursors (asset map §5) for FightCursor's shapes, in their order: the bronze arrow, the four
        // arrows, the sword, and the arrow and the sword with their red mark for what the AP do not reach. The pack's
        // shot cursor is a crossbow, which the setting has not, so a shot shows the sword too.
        private const string Cursors = "Assets/Cursors/Cursors 64/";
        private static readonly string[] FightCursors = { "Cursor_Basic2", "Cursor_Move1", "Cursor_Attack", "Cursor_Basic2_R", "Cursor_Attack_R" };

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
            var cursors = so.FindProperty("fightCursors");
            cursors.arraySize = FightCursors.Length;
            for (var i = 0; i < FightCursors.Length; i++)
            {
                cursors.GetArrayElementAtIndex(i).objectReferenceValue = Cursor(FightCursors[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[SliceConfrontSetup] the slice's settings carry the fight's numbers, the development kit, the opponents and the grid's material.");
        }

        private static Material GridMaterial()
        {
            var shader = Shader.Find(GridShader) ?? throw new System.InvalidOperationException($"[SliceConfrontSetup] missing shader {GridShader}");
            var material = AssetDatabase.LoadAssetAtPath<Material>(GridMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, GridMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", GridColour);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>A pack image imported as a cursor, which <c>Cursor.SetCursor</c> needs: the pack ships them as sprites.</summary>
        private static Texture2D Cursor(string name)
        {
            var path = $"{Cursors}{name}.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Cursor)
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return Load<Texture2D>(path);
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
