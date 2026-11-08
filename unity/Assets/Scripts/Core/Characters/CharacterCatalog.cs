using System.Collections.Generic;

namespace Catpo.Core
{
    public enum Gender
    {
        Female,
        Male,
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

        public CharacterDefinition(string id, string name, Gender gender, string job, string startFloor, params Trait[] uniqueTraits)
        {
            Id = id;
            Name = name;
            Gender = gender;
            Job = job;
            StartFloor = startFloor;
            UniqueTraits = uniqueTraits;
        }
    }

    /// <summary>기본 캐릭터 4명 (docs/04-characters.md). [확정]</summary>
    public static class CharacterCatalog
    {
        public static readonly CharacterDefinition Jipnyang = new CharacterDefinition(
            "jipnyang", "집냥이", Gender.Female, "무직", "지하 1층",
            TraitCatalog.LifeSkill, TraitCatalog.HomeBody);

        public static readonly CharacterDefinition Gunnyang = new CharacterDefinition(
            "gunnyang", "군냥이", Gender.Male, "군인", "2층",
            TraitCatalog.Drill, TraitCatalog.Irregularity);

        public static readonly CharacterDefinition Uinyang = new CharacterDefinition(
            "uinyang", "의냥이", Gender.Female, "의사", "3층",
            TraitCatalog.Specialist, TraitCatalog.Hazing);

        public static readonly CharacterDefinition Gilnyang = new CharacterDefinition(
            "gilnyang", "길냥이", Gender.Female, "도둑", "옥탑방",
            TraitCatalog.SurvivalInstinct, TraitCatalog.Anxiety);

        public static IReadOnlyList<CharacterDefinition> Starting { get; } = new[]
        {
            Jipnyang, Gunnyang, Uinyang, Gilnyang,
        };
    }
}
