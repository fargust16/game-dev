using System.Collections.Generic;
using GameDev.Core.Save;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    /// <summary>17 X.3, S5c.1: a section several systems share, each its own part, and the keyed list that carries them.</summary>
    public sealed class SavePartsTests
    {
        [Test]
        public void AKeyedListCarriesAnyTextAndNestsWithoutEscaping()
        {
            var inner = SaveText.Join(new[] { new KeyValuePair<string, string>("a:b", "1:2;3"), new KeyValuePair<string, string>(string.Empty, string.Empty) });
            var outer = SaveText.Join(new[] { new KeyValuePair<string, string>("inner", inner), new KeyValuePair<string, string>("json", "{\"x\":[1,2]}") });

            var back = SaveText.Split(outer);
            Assert.AreEqual(2, back.Count);
            Assert.AreEqual("inner", back[0].Key);
            Assert.AreEqual("{\"x\":[1,2]}", back[1].Value);
            var nested = SaveText.Split(back[0].Value);
            Assert.AreEqual("a:b", nested[0].Key);
            Assert.AreEqual("1:2;3", nested[0].Value);
            Assert.AreEqual(string.Empty, nested[1].Key);
            Assert.AreEqual(0, SaveText.Split(string.Empty).Count);
        }

        [Test]
        public void ABrokenListIsRefusedNotHalfRead()
        {
            Assert.Throws<SaveVersionException>(() => SaveText.Split("5:abc"));
            Assert.Throws<SaveVersionException>(() => SaveText.Split("x:abc"));
        }

        [Test]
        public void EachPartComesBackToItsOwnSystem()
        {
            var sheet = "s1";
            var stamina = "t1";
            var section = new SavePartsSection(SaveSchema.PlayerCharacter, 1, new ISavePart[]
            {
                new SavePart("sheet", () => sheet, p => sheet = p),
                new SavePart("stamina", () => stamina, p => stamina = p),
            });

            var payload = section.CapturePayload();
            sheet = "changed";
            stamina = "changed";
            section.RestorePayload(1, payload);
            Assert.AreEqual("s1", sheet);
            Assert.AreEqual("t1", stamina);
        }

        [Test]
        public void APartTheSaveLacksIsLeftAsItIsAndOneThisBuildLacksIsSkipped()
        {
            var old = new SavePartsSection(SaveSchema.PlayerCharacter, 1, new ISavePart[] { new SavePart("gone", () => "g", _ => { }) });
            var payload = old.CapturePayload();

            var kept = "mine";
            var now = new SavePartsSection(SaveSchema.PlayerCharacter, 1, new ISavePart[] { new SavePart("new", () => kept, p => kept = p) });
            now.RestorePayload(1, payload);
            Assert.AreEqual("mine", kept);
        }

        [Test]
        public void TheAssemblerWritesAndReadsAPartsSection()
        {
            var value = "v";
            var contributors = new ISaveContributor[] { new SavePartsSection(SaveSchema.WorldPeople, 1, new ISavePart[] { new SavePart("p", () => value, p => value = p) }) };
            var file = SaveAssembler.Capture(contributors);
            value = "other";
            var missing = SaveAssembler.Restore(file, contributors);
            Assert.AreEqual(0, missing.Count);
            Assert.AreEqual("v", value);
        }
    }
}
