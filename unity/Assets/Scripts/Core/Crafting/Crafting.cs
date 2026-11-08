using System.Collections.Generic;
using System.Linq;

namespace Catpo.Core
{
    public enum ActionFailure
    {
        None,
        Dead,
        NotAtHome,
        MissingSkill,
        MissingInputs,
        NoHeat,
        TooStressed,
        NothingLeft,
    }

    /// <summary>집에서 하는 행동(제작, 요리, 집안일, 치료)의 결과. 걸리는 시간은 부른 쪽에서 흘린다.</summary>
    public sealed class ActionResult
    {
        public bool Success => Failure == ActionFailure.None;
        public ActionFailure Failure { get; }
        /// <summary>게임 시간으로 걸리는 시간. 집냥이(생활력)는 짧고, 스트레스가 높으면 길다.</summary>
        public double Hours { get; }
        public HeatSource Heat { get; }
        /// <summary>집 안 가구로 만들 자재가 바닥나서, 밖에서도 가져와야 한다는 메시지를 띄울 때 [확정].</summary>
        public bool NeedMaterialsFromOutside { get; }

        public ActionResult(ActionFailure failure, double hours = 0, HeatSource heat = HeatSource.None, bool needMaterialsFromOutside = false)
        {
            Failure = failure;
            Hours = hours;
            Heat = heat;
            NeedMaterialsFromOutside = needMaterialsFromOutside;
        }

        public static ActionResult Fail(ActionFailure failure, bool needMaterialsFromOutside = false) =>
            new ActionResult(failure, needMaterialsFromOutside: needMaterialsFromOutside);
    }

    public sealed class Recipe
    {
        public string Id { get; }
        public string Name { get; }
        public CraftKind Kind { get; }
        public IReadOnlyDictionary<string, int> Inputs { get; }
        public IReadOnlyDictionary<string, int> Outputs { get; }
        /// <summary>불이 필요 없는 제작의 기본 시간. 불이 필요하면 불의 종류에 따라 정해진다.</summary>
        public double Hours { get; }
        public bool NeedsHeat { get; }
        public Skill XpSkill { get; }
        public double Xp { get; }
        /// <summary>필요 숙련도. 이 특성이 있으면 숙련도와 상관없이 만들 수 있다.</summary>
        public Skill? RequiredSkill { get; }
        public int RequiredLevel { get; }
        public Trait BypassTrait { get; }

        public Recipe(string id, string name, CraftKind kind,
            IReadOnlyDictionary<string, int> inputs, IReadOnlyDictionary<string, int> outputs,
            double hours, bool needsHeat, Skill xpSkill, double xp,
            Skill? requiredSkill = null, int requiredLevel = 0, Trait bypassTrait = null)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Inputs = inputs;
            Outputs = outputs;
            Hours = hours;
            NeedsHeat = needsHeat;
            XpSkill = xpSkill;
            Xp = xp;
            RequiredSkill = requiredSkill;
            RequiredLevel = requiredLevel;
            BypassTrait = bypassTrait;
        }
    }

    /// <summary>
    /// 첫 버전 제작법. 정화제는 소독약·락스·술 같은 재료로 만들고, 의냥이 또는 의료 레벨이 높은 캐릭터만 만든다 [확정].
    /// 재료 조합과 수량은 [제안].
    /// </summary>
    public static class RecipeCatalog
    {
        public static readonly Recipe PurifierFromDisinfectant = Purifier("purifier_disinfectant", "정화제 (소독약)",
            new Dictionary<string, int> { { ItemCatalog.Disinfectant.Id, 1 } }, 2);

        public static readonly Recipe PurifierFromBleach = Purifier("purifier_bleach", "정화제 (락스)",
            new Dictionary<string, int> { { ItemCatalog.Bleach.Id, 1 }, { ItemCatalog.BoiledWater.Id, 1 } }, 2);

        public static readonly Recipe PurifierFromLiquor = Purifier("purifier_liquor", "정화제 (술)",
            new Dictionary<string, int> { { ItemCatalog.LiquorBottle.Id, 1 } }, 1);

        /// <summary>물 끓이기는 요리에 포함된다 [확정].</summary>
        public static readonly Recipe BoilWater = new Recipe("boil_water", "물 끓이기", CraftKind.Cooking,
            new Dictionary<string, int> { { ItemCatalog.RainWater.Id, 2 } },
            new Dictionary<string, int> { { ItemCatalog.BoiledWater.Id, 2 } },
            hours: 0, needsHeat: true, xpSkill: Skill.Crafting, xp: 5);

        public static IReadOnlyList<Recipe> All { get; } = new[]
        {
            PurifierFromDisinfectant, PurifierFromBleach, PurifierFromLiquor, BoilWater,
        };

        private static Recipe Purifier(string id, string name, Dictionary<string, int> inputs, int output) =>
            new Recipe(id, name, CraftKind.Medicine, inputs,
                new Dictionary<string, int> { { ItemCatalog.Purifier.Id, output } },
                hours: 1, needsHeat: false, xpSkill: Skill.Medical, xp: 20,
                requiredSkill: Skill.Medical, requiredLevel: Balance.PurifierMedicalLevel, bypassTrait: TraitCatalog.Specialist);
    }

    public static class CraftingRules
    {
        /// <summary>집 안 행동에 걸리는 시간. 생활력과 행동 속도가 빠르면 짧고, 스트레스가 높으면 길다 [확정].</summary>
        public static double HomeActionHours(Survivor s, double baseHours)
        {
            var e = s.Effects;
            double speed = e.HomeActionSpeed * e.ActionSpeed * StressRules.ActionSpeedMultiplier(s.Stress);
            return baseHours / speed;
        }

        public static bool MeetsRequirement(Survivor s, Recipe r)
        {
            if (!r.RequiredSkill.HasValue) return true;
            if (r.BypassTrait != null && s.Traits.Contains(r.BypassTrait)) return true;
            return s.Skills.Level(r.RequiredSkill.Value) >= r.RequiredLevel;
        }

        /// <summary>집에서 하는 행동이 가능한지 공통으로 확인한다.</summary>
        internal static ActionFailure CheckCanActAtHome(Survivor s)
        {
            if (s.IsDead) return ActionFailure.Dead;
            if (s.Location != SurvivorLocation.Home) return ActionFailure.NotAtHome;
            if (StressRules.ActionSpeedMultiplier(s.Stress) <= 0) return ActionFailure.TooStressed;
            return ActionFailure.None;
        }

        /// <summary>공용 창고의 재료로 만든다. 결과물은 공용 창고로 들어간다.</summary>
        public static ActionResult TryCraft(GameState state, Survivor s, Recipe r)
        {
            var failure = CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            if (!MeetsRequirement(s, r)) return ActionResult.Fail(ActionFailure.MissingSkill);
            if (r.Inputs.Any(kv => state.Storage.Count(kv.Key) < kv.Value)) return ActionResult.Fail(ActionFailure.MissingInputs);

            var heat = HeatSource.None;
            double baseHours = r.Hours;
            if (r.NeedsHeat)
            {
                heat = state.Home.TryUseHeat(state.Storage);
                if (heat == HeatSource.None) return ActionResult.Fail(ActionFailure.NoHeat);
                baseHours = Household.CookHours(heat);
            }

            foreach (var kv in r.Inputs) state.Storage.Remove(kv.Key, kv.Value);
            foreach (var kv in r.Outputs) state.Storage.Add(kv.Key, kv.Value);
            s.GainXp(r.XpSkill, r.Xp, atHome: true);
            StressRules.OnCraft(s, s.Definition.TasteFor(r.Kind));
            return new ActionResult(ActionFailure.None, HomeActionHours(s, baseHours), heat);
        }
    }
}
