using System.Linq;
using GameDev.Slice.People;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// S5c.5: the dwellings' residents on <see cref="SliceSettings"/> (10.3 §6.1, asset map §1): the poor live in the
    /// working people's models, the middling in either, the rich in the gentleman's and the lady's, and a rich house's
    /// servants in the working woman's and the gentleman's; each named person at home wears its own look
    /// (<see cref="SlicePeopleModels.LookFor"/>, VS-HOURS (f)). Authoring defaults.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceResidentsSetup.Apply
    /// </summary>
    public static class SliceResidentsSetup
    {
        [MenuItem("GameDev/Slice/People/Set the Dwellings' Residents")]
        public static void Apply()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var so = new SerializedObject(settings);
            Set(so, "poorResidents", SlicePeopleModels.WorkingWoman, SlicePeopleModels.FactoryWorker, SlicePeopleModels.Dockworker, SlicePeopleModels.OldDockworker);
            Set(so, "middlingResidents", SlicePeopleModels.Gentleman, SlicePeopleModels.Lady, SlicePeopleModels.WorkingWoman, SlicePeopleModels.Tailor);
            Set(so, "richResidents", SlicePeopleModels.Gentleman, SlicePeopleModels.Lady);
            Set(so, "servants", SlicePeopleModels.WorkingWoman, SlicePeopleModels.Gentleman);

            var named = so.FindProperty("namedLooks");
            var homes = SliceHomes.Table.Where(h => SliceRoster.All.Any(e => e.Id == h.Person)).ToList();
            named.arraySize = homes.Count + 1;
            for (var i = 0; i < homes.Count; i++)
            {
                var entry = SliceRoster.All.First(e => e.Id == homes[i].Person);
                Named(named.GetArrayElementAtIndex(i), entry.Id, SlicePeopleModels.LookFor(entry.ObjectName, entry.Female, entry.Colour));
            }

            // The old market trader has no roster entry: the innkeeper's model, since S5-9 (SLICE_PEOPLE §1).
            Named(named.GetArrayElementAtIndex(homes.Count), SliceHours.MarketTrader, SlicePeopleModels.Innkeeper);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[SliceResidentsSetup] residents set; {homes.Count + 1} named people's looks at home.");
        }

        private static void Set(SerializedObject so, string field, params SlicePeopleModels.Look[] looks)
        {
            var list = so.FindProperty(field);
            list.arraySize = looks.Length;
            for (var i = 0; i < looks.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = Prefab(looks[i]);
            }
        }

        private static void Named(SerializedProperty element, string person, SlicePeopleModels.Look look)
        {
            element.FindPropertyRelative("person").stringValue = person;
            element.FindPropertyRelative("prefab").objectReferenceValue = Prefab(look);
        }

        private static GameObject Prefab(SlicePeopleModels.Look look)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing);
            if (prefab == null)
            {
                Debug.LogError($"[SliceResidentsSetup] no prefab at {look.Standing}.");
            }

            return prefab;
        }
    }
}
