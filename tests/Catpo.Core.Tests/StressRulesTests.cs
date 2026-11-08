using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class StressRulesTests
    {
        [Test]
        public void 자고_일어나면_쌓이고_길냥이는_두_배_군냥이는_절반이다()
        {
            var jip = new Survivor(CharacterCatalog.Jipnyang);
            var gil = new Survivor(CharacterCatalog.Gilnyang);
            var gun = new Survivor(CharacterCatalog.Gunnyang);
            StressRules.OnWake(jip);
            StressRules.OnWake(gil);
            StressRules.OnWake(gun);
            Assert.That(jip.Stress, Is.EqualTo(Balance.StressOnWake));
            Assert.That(gil.Stress, Is.EqualTo(Balance.StressOnWake * 2));
            Assert.That(gun.Stress, Is.EqualTo(Balance.StressOnWake * 0.5));
        }

        [Test]
        public void 집_밖에서는_두_배로_쌓인다()
        {
            var home = new Survivor(CharacterCatalog.Gilnyang);
            var outside = new Survivor(CharacterCatalog.Gilnyang) { Location = SurvivorLocation.Outside };
            StressRules.OnTimePassed(home, 4);
            StressRules.OnTimePassed(outside, 4);
            Assert.That(outside.Stress, Is.EqualTo(home.Stress * 2).Within(1e-9));
        }

        [Test]
        public void 취미는_둘_이상이어야_풀린다()
        {
            var a = new Survivor(CharacterCatalog.Jipnyang) { Stress = 50 };
            var b = new Survivor(CharacterCatalog.Gilnyang) { Stress = 50 };

            Assert.That(StressRules.PlayHobby(new[] { a }, 1), Is.False);
            Assert.That(a.Stress, Is.EqualTo(50));

            Assert.That(StressRules.PlayHobby(new[] { a, b }, 1), Is.True);
            Assert.That(a.Stress, Is.EqualTo(50 - Balance.ReliefHobbyPerHour));
            Assert.That(b.Stress, Is.EqualTo(50 - Balance.ReliefHobbyPerHour));
        }

        [Test]
        public void 취향에_맞는_제작은_풀리고_안_맞는_제작은_쌓인다()
        {
            var s = new Survivor(CharacterCatalog.Uinyang) { Stress = 20 };
            StressRules.OnCraft(s, CraftTaste.Liked);
            Assert.That(s.Stress, Is.EqualTo(20 - Balance.ReliefLikedCraft));
            StressRules.OnCraft(s, CraftTaste.Disliked);
            Assert.That(s.Stress, Is.EqualTo(20));
        }

        [Test]
        public void 부족한_자원을_가져오면_집에_있는_모두가_풀린다()
        {
            var a = new Survivor(CharacterCatalog.Jipnyang) { Stress = 30 };
            var b = new Survivor(CharacterCatalog.Uinyang) { Stress = 30 };
            StressRules.OnDelivery(new[] { a, b }, highValueItem: false, scarceResource: true);
            Assert.That(a.Stress, Is.EqualTo(30 - Balance.ReliefScarceResource));
            Assert.That(b.Stress, Is.EqualTo(30 - Balance.ReliefScarceResource));
        }

        [TestCase(0, StressLevel.Normal)]
        [TestCase(40, StressLevel.Sensitive)]
        [TestCase(70, StressLevel.Limit)]
        [TestCase(100, StressLevel.Breakdown)]
        public void 스트레스_단계(double stress, StressLevel level)
        {
            Assert.That(StressRules.LevelOf(stress), Is.EqualTo(level));
        }

        [Test]
        public void 스트레스가_높으면_느려지고_더_먹고_더_피곤해진다()
        {
            Assert.That(StressRules.ActionSpeedMultiplier(75), Is.LessThan(1));
            Assert.That(StressRules.FoodConsumptionMultiplier(75), Is.GreaterThan(1));
            Assert.That(StressRules.FatigueMultiplier(75), Is.GreaterThan(1));
        }
    }
}
