using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S1.6: the player's map is the city plan C4 without the planner's notes, and gives no ritual site away.</summary>
    public sealed class CityMapPlanTests
    {
        [Test]
        public void TheMapNamesNoRitualSiteAndCarriesNoPlannersNotes()
        {
            var plan = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Slice/Map/VeilmouthC4Plan.xml");
            Assert.IsNotNull(plan, "the map's plan is a text asset");
            var drawn = System.Xml.Linq.XElement.Parse(plan.text).ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
            foreach (var word in new[] { "Ritual", "Candidate", "proposed", "crossroads", " m<" })
            {
                StringAssert.DoesNotContain(word, StripComments(drawn), $"the player's map shows no '{word.Trim()}'");
            }
        }

        private static string StripComments(string xml) => System.Text.RegularExpressions.Regex.Replace(xml, "<!--.*?-->", string.Empty);
    }
}
