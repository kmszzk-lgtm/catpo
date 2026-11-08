using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class InfectionRulesTests
    {
        private static Survivor NewSurvivor() => new Survivor(CharacterCatalog.Gunnyang);

        [Test]
        public void 처음에는_오염도_1_감염도_0이다()
        {
            var s = NewSurvivor();
            Assert.That(s.Contamination, Is.EqualTo(1));
            Assert.That(s.Infection, Is.EqualTo(0));
            Assert.That(s.IsInfected, Is.False);
        }

        [Test]
        public void 바깥_10시간은_안전하고_오염도_50에서_감염된다()
        {
            var s = NewSurvivor();
            InfectionRules.ExposeOutside(s, 10);
            Assert.That(s.Contamination, Is.EqualTo(41).Within(1e-9));
            InfectionRules.ExposeOutside(s, 2);
            Assert.That(s.Contamination, Is.EqualTo(49).Within(1e-9));
            Assert.That(s.IsInfected, Is.False);
            InfectionRules.ExposeOutside(s, 0.25);
            Assert.That(s.Contamination, Is.EqualTo(50).Within(1e-9));
            Assert.That(s.IsInfected, Is.True);
        }

        [Test]
        public void 정화제는_오염도를_1_아래로_내리지_않는다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 9);
            InfectionRules.ApplyPurifier(s);
            Assert.That(s.Contamination, Is.EqualTo(1));
        }

        [Test]
        public void 정화제는_감염된_뒤에도_오염도를_낮추지만_감염은_남는다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 59); // 60
            Assert.That(s.IsInfected, Is.True);
            InfectionRules.ApplyPurifier(s);
            Assert.That(s.Contamination, Is.EqualTo(40));
            Assert.That(s.IsInfected, Is.True);
        }

        [Test]
        public void 물리면_바로_감염된다()
        {
            var s = NewSurvivor();
            InfectionRules.Bite(s);
            Assert.That(s.IsInfected, Is.True);
            Assert.That(s.Contamination, Is.EqualTo(1));
        }

        [Test]
        public void 감염도는_하루에_기본_5와_오염도_10마다_1씩_오르고_100이면_완전_변이한다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 49); // 50, 감염
            for (int day = 0; day < 9; day++) InfectionRules.DailyTick(s, 0);
            Assert.That(s.Infection, Is.EqualTo(90));
            Assert.That(s.IsDead, Is.False);
            InfectionRules.DailyTick(s, 0);
            Assert.That(s.Infection, Is.EqualTo(100));
            Assert.That(s.IsDead, Is.True);
            Assert.That(s.DeathCause, Is.EqualTo("완전 변이"));
        }

        [Test]
        public void 오염도가_낮을_때_물리면_천천히_오른다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 9); // 10
            InfectionRules.Bite(s);
            InfectionRules.DailyTick(s, 0);
            Assert.That(s.Infection, Is.EqualTo(6));
        }

        [Test]
        public void 그날_새로_감염되면_다음_날부터_오른다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 45); // 46
            InfectionRules.DailyTick(s, Balance.RegionBossContaminationPerDay); // 51, 감염
            Assert.That(s.IsInfected, Is.True);
            Assert.That(s.Infection, Is.EqualTo(0));
            InfectionRules.DailyTick(s, Balance.RegionBossContaminationPerDay); // 56 → +10
            Assert.That(s.Infection, Is.EqualTo(10));
        }

        [Test]
        public void 억제제는_감염도를_10_낮추고_3일_동안_막는다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 79); // 80, 감염 (하루 +13)
            InfectionRules.DailyTick(s, 0);
            InfectionRules.DailyTick(s, 0);
            Assert.That(s.Infection, Is.EqualTo(26));

            InfectionRules.ApplySuppressant(s);
            Assert.That(s.Infection, Is.EqualTo(16));
            for (int day = 0; day < 3; day++) InfectionRules.DailyTick(s, 0);
            Assert.That(s.Infection, Is.EqualTo(16), "오염도가 높아도 3일 동안은 오르지 않는다");
            InfectionRules.DailyTick(s, 0);
            Assert.That(s.Infection, Is.EqualTo(29));
        }

        [Test]
        public void 억제제로_감염도를_0까지_내려도_감염_상태는_남는다()
        {
            var s = NewSurvivor();
            InfectionRules.Bite(s);
            InfectionRules.DailyTick(s, 0); // 5
            InfectionRules.ApplySuppressant(s);
            Assert.That(s.Infection, Is.EqualTo(0));
            Assert.That(s.IsInfected, Is.True);
        }

        [Test]
        public void 완치제는_감염을_없애고_오염도를_10까지_낮춘다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 79); // 80
            InfectionRules.DailyTick(s, 0);
            InfectionRules.ApplyCure(s);
            Assert.That(s.IsInfected, Is.False);
            Assert.That(s.Infection, Is.EqualTo(0));
            Assert.That(s.Contamination, Is.EqualTo(10));
        }

        [Test]
        public void 완치제는_오염도가_10_이하면_그대로_둔다()
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, 4); // 5
            InfectionRules.Bite(s);
            InfectionRules.ApplyCure(s);
            Assert.That(s.Contamination, Is.EqualTo(5));
        }

        [Test]
        public void 지역_보스가_있으면_집에서도_하루_5씩_오른다()
        {
            var s = NewSurvivor();
            InfectionRules.DailyTick(s, Balance.RegionBossContaminationPerDay);
            Assert.That(s.Contamination, Is.EqualTo(6));
        }

        [TestCase(41.0, false)]
        [TestCase(41.5, true)]
        public void 귀환_때_오염도가_기준을_넘으면_다음_날_쉰다(double contamination, bool expected)
        {
            var s = NewSurvivor();
            InfectionRules.AddContamination(s, contamination - 1);
            Assert.That(InfectionRules.NeedsRestAfterReturn(s), Is.EqualTo(expected));
        }
    }
}
