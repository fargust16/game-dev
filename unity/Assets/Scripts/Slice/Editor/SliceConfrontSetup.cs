using System.Linq;
using GameDev.Confront.Rules;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S3a.4's wiring of the slice's settings: the fight's numbers, the development kit (10.2 §5d, owner 2026-11-12:
    /// firearms first: a pistol, leather, and a rifle), the development opponents, the Combat Grid's material, and the
    /// target's outline. Batch:
    /// -executeMethod GameDev.Slice.EditorTools.SliceConfrontSetup.Configure
    /// </summary>
    public static class SliceConfrontSetup
    {
        public const string SettingsPath = "Assets/Data/Slice/SliceSettings.asset";
        public const string GridMaterialPath = "Assets/Materials/Slice/Confront_Grid.mat";
        private const string Data = "Assets/Data/Confront";
        private const string TargetOutlineShader = "Assets/Shaders/Slice/TargetOutline.shader";

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

        // The fight hotbar's icons (owner's play test, 2026-11-11, after BG3's): the Modern GDR pack's bright atlas, which
        // the asset map names for every icon of the game, tinted by the HUD, one for each FightAction after None: the
        // skip for ending the turn, the clock for waiting, the shield for the Defensive Stance, the turning arrow for the
        // other Weapon Set, the full heart for First Aid, the figure in shadow for Stealth, the arrows running back for
        // Сбежать (owner, 2026-11-12), the way out for a Yield, the open heart for mercy, the struck lightning for Infusion
        // (CR4).
        private const string IconAtlas = "Assets/Modern GDR - Free icons pack/00_Atlas/BrightIcons.png";
        private static readonly string[] FightIcons =
        {
            "Skip_Bright", "Watch_Bright", "Shield_Bright", "Reload_Bright", "Heart02_Bright", "Network_Bright", "Rewind_Bright", "Exit_Bright",
            "Heart01_Bright", "ThunderStrike_Bright",
        };

        // CR4, 05.1 *Abilities of the slice*: every ability in the order the player's Prepared set is kept by, never to be
        // reordered, each with its icon from the same atlas (authoring defaults).
        private static readonly (string File, string Icon)[] Abilities =
        {
            ("KeenEar", "MusicNote01_Bright"), ("Suggestion", "Text_Bright"), ("Screen", "Circle_Bright"), ("Drowse", "Pause_Bright"),
            ("DreamTouch", "Quest01_Bright"), ("Double", "Diamond_Bright"), ("Veil", "ToggleOff_Bright"), ("MindSting", "Thunder_Bright"),
            ("Kinship", "Compass_Bright"), ("Unmoved", "Padlock01_Bright"), ("Dash", "Right_Bright"), ("Support", "Gear02_Bright"),
            ("Halt", "Cross_Bright"), ("Press", "Down02_Bright"), ("Pillar", "Up_Bright"), ("Crush", "Sword_Bright"),
        };

        public static void Configure()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SettingsPath);
            var so = new SerializedObject(settings);
            so.FindProperty("fightRules").objectReferenceValue = Load<FightRules>($"{Data}/FightRules.asset");
            so.FindProperty("stealthRules").objectReferenceValue = Load<GameDev.Confront.Stealth.StealthRules>($"{Data}/StealthRules.asset");
            so.FindProperty("beastRules").objectReferenceValue = Load<GameDev.Slice.Beasts.BeastRules>("Assets/Data/Slice/BeastRules.asset");
            so.FindProperty("peoplePistol").objectReferenceValue = Load<FightWeapon>($"{Data}/Weapons/Pistol.asset");
            so.FindProperty("wardensBaton").objectReferenceValue = Load<FightWeapon>($"{Data}/Weapons/WardensBaton.asset");
            so.FindProperty("kitRifle").objectReferenceValue = Load<FightWeapon>($"{Data}/Weapons/Rifle.asset");
            so.FindProperty("kitPouch").objectReferenceValue = Load<FightOffHand>($"{Data}/Weapons/CartridgePouch.asset");
            so.FindProperty("kitArmour").objectReferenceValue = Load<FightArmour>($"{Data}/Armour/Leather.asset");
            so.FindProperty("opponentModel").objectReferenceValue = Load<GameObject>(OpponentModel);
            so.FindProperty("gridMaterial").objectReferenceValue = GridMaterial();
            so.FindProperty("targetOutline").objectReferenceValue = Load<Shader>(TargetOutlineShader);
            var cursors = so.FindProperty("fightCursors");
            cursors.arraySize = FightCursors.Length;
            for (var i = 0; i < FightCursors.Length; i++)
            {
                cursors.GetArrayElementAtIndex(i).objectReferenceValue = Cursor(FightCursors[i]);
            }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(IconAtlas).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            var icons = so.FindProperty("fightIcons");
            icons.arraySize = FightIcons.Length;
            for (var i = 0; i < FightIcons.Length; i++)
            {
                icons.GetArrayElementAtIndex(i).objectReferenceValue = sprites.TryGetValue(FightIcons[i], out var sprite)
                    ? sprite
                    : throw new System.InvalidOperationException($"[SliceConfrontSetup] missing icon {FightIcons[i]} in {IconAtlas}");
            }

            var abilities = so.FindProperty("abilities");
            abilities.arraySize = Abilities.Length;
            for (var i = 0; i < Abilities.Length; i++)
            {
                var ability = Load<FightAbility>($"{Data}/Abilities/{Abilities[i].File}.asset");
                var own = new SerializedObject(ability);
                own.FindProperty("icon").objectReferenceValue = sprites.TryGetValue(Abilities[i].Icon, out var mark)
                    ? mark
                    : throw new System.InvalidOperationException($"[SliceConfrontSetup] missing icon {Abilities[i].Icon} in {IconAtlas}");
                own.ApplyModifiedPropertiesWithoutUndo();
                abilities.GetArrayElementAtIndex(i).objectReferenceValue = ability;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[SliceConfrontSetup] the slice's settings carry the fight's numbers, the development kit, the opponents, the grid's material and the abilities.");
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
