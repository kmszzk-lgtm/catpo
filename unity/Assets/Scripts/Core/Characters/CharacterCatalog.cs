using System.Collections.Generic;

namespace Catpo.Core
{
    public enum Gender
    {
        Female,
        Male,
    }

    /// <summary>제작의 종류. 캐릭터마다 좋아하는 제작과 싫어하는 제작이 있다 (docs/08-stress.md).</summary>
    public enum CraftKind
    {
        Cooking,
        Medicine,
        WeaponCare,
        SmallTools,
        Housework,
        BigBuild,
    }

    /// <summary>
    /// 캐릭터의 고정 정보. 고유 특성은 바꿀 수 없다.
    /// </summary>
    public sealed class CharacterDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public Gender Gender { get; }
        public string Job { get; }
        public string StartFloor { get; }
        public IReadOnlyList<Trait> UniqueTraits { get; }

        /// <summary>시작 숙련도. 적지 않은 숙련도는 1.</summary>
        public IReadOnlyDictionary<Skill, int> StartingSkills { get; internal set; } = new Dictionary<Skill, int>();
        public CraftKind? LikedCraft { get; internal set; }
        public IReadOnlyList<CraftKind> DislikedCrafts { get; internal set; } = new CraftKind[0];

        public CharacterDefinition(string id, string name, Gender gender, string job, string startFloor, params Trait[] uniqueTraits)
        {
            Id = id;
            Name = name;
            Gender = gender;
            Job = job;
            StartFloor = startFloor;
            UniqueTraits = uniqueTraits;
        }

        public CraftTaste TasteFor(CraftKind kind)
        {
            if (LikedCraft == kind) return CraftTaste.Liked;
            foreach (var d in DislikedCrafts) if (d == kind) return CraftTaste.Disliked;
            return CraftTaste.Neutral;
        }
    }

    /// <summary>
    /// 기본 캐릭터 4명 (docs/04-characters.md). 이름, 성별, 직업, 고유 특성은 [확정].
    /// 시작 숙련도와 취향은 [제안] (docs/08-stress.md 취향 예시).
    /// </summary>
    public static class CharacterCatalog
    {
        public static readonly CharacterDefinition Jipnyang = new CharacterDefinition(
            "jipnyang", "집냥이", Gender.Female, "무직", "지하 1층",
            TraitCatalog.LifeSkill, TraitCatalog.HomeBody)
        {
            StartingSkills = new Dictionary<Skill, int> { { Skill.Crafting, 2 } },
            LikedCraft = CraftKind.Cooking,
            DislikedCrafts = new[] { CraftKind.WeaponCare },
        };

        public static readonly CharacterDefinition Gunnyang = new CharacterDefinition(
            "gunnyang", "군냥이", Gender.Male, "군인", "2층",
            TraitCatalog.Drill, TraitCatalog.Irregularity)
        {
            StartingSkills = new Dictionary<Skill, int> { { Skill.Melee, 3 }, { Skill.Stealth, 2 } },
            LikedCraft = CraftKind.WeaponCare,
            DislikedCrafts = new[] { CraftKind.Cooking, CraftKind.Housework },
        };

        public static readonly CharacterDefinition Uinyang = new CharacterDefinition(
            "uinyang", "의냥이", Gender.Female, "의사", "3층",
            TraitCatalog.Specialist, TraitCatalog.Hazing)
        {
            StartingSkills = new Dictionary<Skill, int> { { Skill.Medical, 5 } },
            LikedCraft = CraftKind.Medicine,
            DislikedCrafts = new[] { CraftKind.Housework },
        };

        public static readonly CharacterDefinition Gilnyang = new CharacterDefinition(
            "gilnyang", "길냥이", Gender.Female, "도둑", "옥탑방",
            TraitCatalog.SurvivalInstinct, TraitCatalog.Anxiety)
        {
            StartingSkills = new Dictionary<Skill, int> { { Skill.Search, 3 }, { Skill.Stealth, 3 } },
            LikedCraft = CraftKind.SmallTools,
            DislikedCrafts = new[] { CraftKind.BigBuild },
        };

        public static IReadOnlyList<CharacterDefinition> Starting { get; } = new[]
        {
            Jipnyang, Gunnyang, Uinyang, Gilnyang,
        };
    }
}
