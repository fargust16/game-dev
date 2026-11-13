using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.Slice.Alchemy;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using Random = System.Random;

namespace GameDev.Slice.Trial
{
    /// <summary>
    /// The first-day trial and Faction Entry (S5c.4; SLICE_TRIAL §1; 11 *The fork*; 19 §6.2). The trial is told in the
    /// world's state: Ashgrave's, Lottie Wren's and Father Hale's conversations raise its keys, and this service answers
    /// each key the moment it is raised, with the notes, items and coins it brings. A load restores the keys without
    /// raising them, so nothing is given twice. Thomas Wren's door, the loose board under his bed and the page of the
    /// Faction Gift are scene parts that read it (<see cref="TrialDoor"/>, <see cref="TrialCache"/>, <see cref="TrialRun"/>).
    /// </summary>
    public sealed class SliceTrial
    {
        public const string Ashgrave = "ashgrave";
        public const string Lottie = "lottie";
        public const string Hale = "hale";

        /// <summary>The first note, the same from every door in (§1.1): Thomas Wren is missing, the Notice Board's own tip.</summary>
        public const string MissingTip = "board.wren";

        /// <summary>The note the cache leaves: what lay under the board.</summary>
        public const string CacheTip = "trial.cache";

        /// <summary>Father Hale's word, a note (§1.4).</summary>
        public const string VouchTip = "trial.vouch";

        /// <summary>Ashgrave offered the trial.</summary>
        public const string OfferedKey = "trial.offered";

        /// <summary>Lottie gave Thomas's key.</summary>
        public const string KeyGivenKey = "trial.key";

        /// <summary>Lottie's line about the boards, an Argument gained in this life: the cache is found without a roll (§1.2).</summary>
        public const string BoardsArgument = "arg.trial.boards";

        /// <summary>The loose board was noticed, by a Perception check or Lottie's line.</summary>
        public const string NoticedKey = "trial.cache.noticed";

        /// <summary>The cache was taken from under the board.</summary>
        public const string FoundKey = "trial.cache.found";

        /// <summary>The cache was handed in to Ashgrave: the trial's first end.</summary>
        public const string HandedKey = "trial.handed";

        /// <summary>Father Hale vouched for the character.</summary>
        public const string VouchedKey = "trial.vouched";

        /// <summary>Faction Entry: the character is a member of the faction.</summary>
        public const string MemberKey = SliceAlchemy.MemberKey;

        /// <summary>The Faction Gift was taken.</summary>
        public const string GiftKey = "faction.gift.taken";

        /// <summary>A member drank the Limit 9 potion of Жрица: Ashgrave has the next step for it.</summary>
        public const string PriestessNineKey = "trial.priestess.nine";

        /// <summary>Ashgrave gave the formula of Limit 8, Жрица.</summary>
        public const string NextFormulaKey = "trial.formula.eight";

        /// <summary>Thomas's door was unlocked, with his key or (S5c.5) picked.</summary>
        public const string DoorOpenKey = "door.missing-room.open";

        /// <summary>Thomas's door from Draught Lane.</summary>
        public const string DoorId = "missing-room.street";

        public const string ThomasKeyItem = "key_thomas";
        public const string LetterItem = "letter_introduction";

        /// <summary>§1.3: Thomas's savings, bronze.</summary>
        public const int Savings = 600;

        /// <summary>§1.4: the recruit's purse, bronze.</summary>
        public const int RecruitPurse = 1000;

        /// <summary>19 §6.2: three items offered, of which one is taken.</summary>
        public const int GiftCount = 3;

        /// <summary>§1.2: the Perception check's stage, and the reach within which it is rolled, m.</summary>
        public const int NoticeStage = 9;

        public const float NoticeReach = 2f;

        /// <summary>§1.3: the cache's two formulas, the path taken alone.</summary>
        public static readonly string[] CacheFormulas = { "formula_emperor_9", "formula_emperor_8" };

        public const string PriestessNine = "formula_priestess_9";
        public const string PriestessEight = "formula_priestess_8";

        /// <summary>
        /// 19 §6.2: the Faction Gift's bases, firearms and armour only while melee is withheld (10.2 §5d). A firearm comes
        /// with a plain Cartridge Pouch, without which it cannot shoot (agent's reading, 99 VS-TRIAL).
        /// </summary>
        public static readonly string[] GiftBases =
        {
            "pistol", "rifle",
            "cloth_head", "cloth_body", "cloth_shoulders", "cloth_gloves", "cloth_trousers", "cloth_boots",
            "leather_head", "leather_body", "leather_shoulders", "leather_gloves", "leather_trousers", "leather_boots",
            "chain_head", "chain_body", "chain_shoulders", "chain_gloves", "chain_trousers", "chain_boots",
            "plate_head", "plate_body", "plate_shoulders", "plate_gloves", "plate_trousers", "plate_boots",
        };

        public const string Pouch = "cartridge_pouch";

        private readonly IWorldState _state;
        private readonly Bag _bag;
        private readonly TipBook _tips;
        private readonly IWorldSeed _seed;
        private readonly ItemCatalogue _items;
        private readonly Func<long> _tick;

        public SliceTrial(IWorldState state, Bag bag, TipBook tips, IWorldSeed seed, ItemCatalogue items, Func<long> tick)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            _tips = tips;
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _tick = tick ?? (() => 0L);
            _state.Changed += OnChanged;
        }

        /// <summary>Faction Entry happened: the page of the Faction Gift is due.</summary>
        public event Action Entered;

        public bool IsMember => _state.IsSet(MemberKey);

        public bool GiftTaken => _state.IsSet(GiftKey);

        /// <summary>Whether the gift is still to be taken.</summary>
        public bool GiftDue => IsMember && !GiftTaken;

        public bool DoorOpen => _state.IsSet(DoorOpenKey);

        public bool HasThomasKey => _bag.Count(ThomasKeyItem) > 0;

        public bool CacheFound => _state.IsSet(FoundKey);

        public bool CacheNoticed => _state.IsSet(NoticedKey) || _state.IsSet(BoardsArgument);

        public void Detach() => _state.Changed -= OnChanged;

        private void OnChanged(string key, int before, int after)
        {
            if (before != 0 || after == 0)
            {
                return;
            }

            switch (key)
            {
                case OfferedKey:
                    Hear(MissingTip);
                    break;
                case KeyGivenKey:
                    Hear(MissingTip);
                    _bag.Add(ThomasKeyItem);
                    break;
                case HandedKey:
                    // Ashgrave takes the two formulas and the letter; the coins stay the character's (§1.4).
                    foreach (var formula in CacheFormulas.Append(LetterItem))
                    {
                        _bag.Remove(formula, _bag.Count(formula));
                    }

                    break;
                case VouchedKey:
                    Hear(VouchTip);
                    break;
                case MemberKey:
                    _bag.Add(PriestessNine);
                    _bag.Earn(RecruitPurse);
                    Entered?.Invoke();
                    break;
                case NextFormulaKey:
                    _bag.Add(PriestessEight);
                    break;
            }
        }

        private void Hear(string tip) => _tips?.Tell(tip, _tick());

        /// <summary>
        /// The cache taken from under the board (§1.3): both Император formulas, the letter, and Thomas's savings, and a
        /// note of what lay there. False when it is not noticed yet or already taken.
        /// </summary>
        public bool TakeCache()
        {
            if (CacheFound || !CacheNoticed)
            {
                return false;
            }

            foreach (var formula in CacheFormulas)
            {
                _bag.Add(formula);
            }

            _bag.Add(LetterItem);
            _bag.Earn(Savings);
            _state.Raise(FoundKey);
            Hear(CacheTip);
            return true;
        }

        /// <summary>A Limit potion settled: the fork closes with the first (§1.4), and a member's Жрица 9 opens Ashgrave's next step.</summary>
        public void Advanced(GameDev.Player.Progression.Arcana arcana, int limit)
        {
            if (arcana == GameDev.Player.Progression.Arcana.Priestess && limit == 9 && IsMember)
            {
                _state.Raise(PriestessNineKey);
            }
        }

        // ------------------------------------------------------------------------------------------- the Faction Gift

        /// <summary>
        /// 19 §6.2: the three items offered at Faction Entry, set by the World Seed: three different bases of
        /// <see cref="GiftBases"/>, each Magic in 70 % of draws (one or two modifiers), Rare in 28 % (three) and Epic in
        /// 2 % (four), at stage 9. The same seed offers the same three, so a load shows what was shown.
        /// </summary>
        public IReadOnlyList<ItemPiece> GiftOffer() => Offer(_seed, _items);

        public static IReadOnlyList<ItemPiece> Offer(IWorldSeed seed, ItemCatalogue items)
        {
            var rolls = seed.Fork(SliceSeedStreams.FactionGift);
            var bases = GiftBases.Where(b => items.Find(b) != null).ToList();
            var offer = new List<ItemPiece>();
            for (var i = 0; i < GiftCount && bases.Count > 0; i++)
            {
                var pick = bases[rolls.Next(bases.Count)];
                bases.Remove(pick);
                offer.Add(ItemMaker.MakeWith(items.Find(pick), 9, Count(rolls.NextDouble(), rolls.NextDouble()), rolls));
            }

            return offer;
        }

        /// <summary>19 §6.2: the modifiers of a gift by its quality's draw, and within Magic one or two at even odds.</summary>
        public static int Count(double quality, double within) => quality < 0.70d ? (within < 0.5d ? 1 : 2) : quality < 0.98d ? 3 : 4;

        /// <summary>Takes the offered item <paramref name="index"/>; a firearm comes with a plain pouch. False once taken or out of range.</summary>
        public bool TakeGift(int index)
        {
            var offer = GiftOffer();
            if (!GiftDue || index < 0 || index >= offer.Count)
            {
                return false;
            }

            var piece = offer[index];
            _bag.AddPiece(piece);
            if (IsFirearm(piece.item) && _items.Find(Pouch) != null)
            {
                _bag.AddPiece(new ItemPiece(Pouch));
            }

            _state.Raise(GiftKey);
            return true;
        }

        public static bool IsFirearm(string item) => item == "pistol" || item == "rifle";

        // ------------------------------------------------------------------------------------------- the loose board

        /// <summary>
        /// §1.2: whether a Perception check at stage 9 notices the board, one draw from <paramref name="rolls"/> at
        /// <paramref name="chance"/>: rolled once each time the character comes within 2 m of the bed.
        /// </summary>
        public static bool Notices(double chance, Random rolls) => rolls.NextDouble() < chance;
    }
}
