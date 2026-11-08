using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class NeedsAndSkillsTests
    {
        [Test]
        public void 배고픔_목마름은_시간이_지나면_오르고_길냥이는_덜_오른다()
        {
            var jip = new Survivor(CharacterCatalog.Jipnyang);
            var gil = new Survivor(CharacterCatalog.Gilnyang);
            NeedsRules.OnTimePassed(jip, 10);
            NeedsRules.OnTimePassed(gil, 10);
            Assert.That(jip.Hunger, Is.EqualTo(12).Within(1e-9));
            Assert.That(jip.Thirst, Is.EqualTo(18).Within(1e-9));
            Assert.That(gil.Hunger, Is.EqualTo(9).Within(1e-9));
            Assert.That(gil.Thirst, Is.EqualTo(13.5).Within(1e-9));
        }

        [Test]
        public void 감염도가_높으면_배고픔_목마름이_빨리_온다()
        {
            var s = new Survivor(CharacterCatalog.Jipnyang) { Infection = 100 };
            NeedsRules.OnTimePassed(s, 10);
            Assert.That(s.Hunger, Is.EqualTo(24).Within(1e-9));
            Assert.That(s.Thirst, Is.EqualTo(36).Within(1e-9));
        }

        [Test]
        public void 배고픔이_한계에_가까우면_스트레스가_쌓인다()
        {
            var s = new Survivor(CharacterCatalog.Jipnyang) { Hunger = 80 };
            NeedsRules.OnTimePassed(s, 1);
            Assert.That(s.Stress, Is.EqualTo(Balance.NeedStressPerHour).Within(1e-9));
        }

        [Test]
        public void 오염도가_높으면_스트레스를_더_받는다()
        {
            var s = new Survivor(CharacterCatalog.Jipnyang) { Contamination = 51 };
            StressRules.OnWake(s);
            Assert.That(s.Stress, Is.EqualTo(Balance.StressOnWake * 1.5).Within(1e-9));
        }

        [Test]
        public void 감염도가_높으면_스트레스를_더_받는다()
        {
            var s = new Survivor(CharacterCatalog.Jipnyang) { Infection = 100, IsInfected = true };
            StressRules.OnWake(s);
            Assert.That(s.Stress, Is.EqualTo(Balance.StressOnWake * 2).Within(1e-9));
        }

        [Test]
        public void 먹으면_배고픔이_줄고_빗물은_끓여야_마신다()
        {
            var s = new Survivor(CharacterCatalog.Jipnyang) { Hunger = 50 };
            var inv = new Inventory();
            inv.Add(ItemCatalog.CannedFood.Id);
            inv.Add(ItemCatalog.RainWater.Id);

            Assert.That(NeedsRules.Consume(s, inv, ItemCatalog.CannedFood.Id), Is.EqualTo(ConsumeResult.Ok));
            Assert.That(s.Hunger, Is.EqualTo(15).Within(1e-9));
            Assert.That(NeedsRules.Consume(s, inv, ItemCatalog.RainWater.Id), Is.EqualTo(ConsumeResult.NeedsBoiling));
            Assert.That(inv.Count(ItemCatalog.RainWater.Id), Is.EqualTo(1));
            Assert.That(NeedsRules.Consume(s, inv, ItemCatalog.CannedFood.Id), Is.EqualTo(ConsumeResult.NotAvailable));
        }

        [Test]
        public void 길냥이는_충동적으로_더_먹는다()
        {
            var gil = new Survivor(CharacterCatalog.Gilnyang) { Hunger = 100 };
            var jip = new Survivor(CharacterCatalog.Jipnyang) { Hunger = 100 };
            var gilFood = new Inventory();
            var jipFood = new Inventory();
            gilFood.Add(ItemCatalog.CannedFood.Id, 10);
            jipFood.Add(ItemCatalog.CannedFood.Id, 10);

            for (int i = 0; i < 5; i++)
            {
                NeedsRules.Consume(gil, gilFood, ItemCatalog.CannedFood.Id);
                NeedsRules.Consume(jip, jipFood, ItemCatalog.CannedFood.Id);
            }
            Assert.That(gilFood.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(4), "5번 먹는 동안 1개를 더 먹는다");
            Assert.That(jipFood.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(5));
        }

        [Test]
        public void 경험치가_차면_레벨이_오른다()
        {
            var skills = new SkillSet();
            Assert.That(skills.Level(Skill.Crafting), Is.EqualTo(1));
            Assert.That(skills.AddXp(Skill.Crafting, SkillSet.XpToNext(1)), Is.EqualTo(1));
            Assert.That(skills.Level(Skill.Crafting), Is.EqualTo(2));
            Assert.That(skills.Xp(Skill.Crafting), Is.EqualTo(0));
        }

        [Test]
        public void 군냥이는_집_안_경험치가_적고_외부_경험치가_많다()
        {
            var home = new Survivor(CharacterCatalog.Gunnyang);
            var outside = new Survivor(CharacterCatalog.Gunnyang);
            home.GainXp(Skill.Crafting, 200, atHome: true);
            outside.GainXp(Skill.Crafting, 200, atHome: false);
            Assert.That(home.Skills.Level(Skill.Crafting), Is.EqualTo(1));
            Assert.That(home.Skills.Xp(Skill.Crafting), Is.EqualTo(100).Within(1e-9));
            Assert.That(outside.Skills.Level(Skill.Crafting), Is.EqualTo(2));
            Assert.That(outside.Skills.Xp(Skill.Crafting), Is.EqualTo(100).Within(1e-9));
        }

        [Test]
        public void 캐릭터는_시작_숙련도를_가지고_시작한다()
        {
            Assert.That(new Survivor(CharacterCatalog.Uinyang).Skills.Level(Skill.Medical), Is.EqualTo(5));
            Assert.That(new Survivor(CharacterCatalog.Jipnyang).Skills.Level(Skill.Medical), Is.EqualTo(1));
        }
    }
}
