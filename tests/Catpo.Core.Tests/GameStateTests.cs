using System.Linq;
using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class GameStateTests
    {
        private GameState state;
        private Survivor jip, gun, ui, gil;

        [SetUp]
        public void SetUp()
        {
            state = GameState.NewTutorial();
            jip = state.Get("jipnyang");
            gun = state.Get("gunnyang");
            ui = state.Get("uinyang");
            gil = state.Get("gilnyang");
        }

        private void AdvanceTo(int hour)
        {
            int minutes = hour * 60 - state.Time.MinuteOfDay;
            if (minutes < 0) minutes += Balance.MinutesPerDay;
            state.Advance(minutes);
        }

        [Test]
        public void 튜토리얼은_봄_1일째_아침에_4명이_집에서_시작한다()
        {
            Assert.That(state.Time.Day, Is.EqualTo(1));
            Assert.That(state.Time.Season, Is.EqualTo(Season.Spring));
            Assert.That(state.Survivors.Count, Is.EqualTo(4));
            Assert.That(state.Survivors.All(s => s.Location == SurvivorLocation.Home), Is.True);
            Assert.That(state.Storage.Count(ItemCatalog.FirstAidKit.Id), Is.GreaterThan(0));
        }

        [Test]
        public void 집냥이는_외출할_수_없다()
        {
            AdvanceTo(9);
            Assert.That(state.TryDepart(jip, Direction.North), Is.EqualTo(DepartResult.CannotGoOutside));
        }

        [Test]
        public void 외출_가능_시간_전에는_나갈_수_없다()
        {
            Assert.That(state.TryDepart(gun, Direction.North), Is.EqualTo(DepartResult.OutsideWindowClosed));
            AdvanceTo(9);
            Assert.That(state.TryDepart(gun, Direction.North), Is.EqualTo(DepartResult.Ok));
        }

        [Test]
        public void 한_방향에는_한_명만_나가고_돌아오면_풀린다()
        {
            AdvanceTo(9);
            Assert.That(state.TryDepart(gun, Direction.North), Is.EqualTo(DepartResult.Ok));
            Assert.That(state.TryDepart(gil, Direction.North), Is.EqualTo(DepartResult.DirectionOccupied));
            Assert.That(state.TryDepart(gil, Direction.East), Is.EqualTo(DepartResult.Ok));

            state.Return(gun);
            Assert.That(state.Outings.IsOccupied(Direction.North), Is.False);
            Assert.That(state.TryDepart(ui, Direction.North), Is.EqualTo(DepartResult.Ok));
        }

        [Test]
        public void 귀환하면_소지품이_공용_창고로_들어간다()
        {
            AdvanceTo(9);
            int before = state.Storage.Count(ItemCatalog.CannedFood.Id);
            state.TryDepart(gun, Direction.North);
            Assert.That(gun.TryPickUp(ItemCatalog.CannedFood.Id, 2), Is.True);
            state.Return(gun);
            Assert.That(gun.Inventory.IsEmpty, Is.True);
            Assert.That(state.Storage.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(before + 2));
        }

        [Test]
        public void 무게_한도를_넘게_들려면_과적해야_한다()
        {
            for (int i = 0; i < 4; i++) Assert.That(gun.TryPickUp(ItemCatalog.BottledWater.Id), Is.True);
            Assert.That(gun.Inventory.TotalWeightKg, Is.EqualTo(Balance.BaseCarryLimitKg).Within(1e-9));
            Assert.That(gun.TryPickUp(ItemCatalog.BottledWater.Id), Is.False);

            // 사람은 과적해서 더 들 수 있다 (한도의 1.5배까지)
            Assert.That(gun.TryPickUp(ItemCatalog.BottledWater.Id, allowOverload: true), Is.True);
            Assert.That(gun.TryPickUp(ItemCatalog.BottledWater.Id, allowOverload: true), Is.True);
            Assert.That(gun.TryPickUp(ItemCatalog.BottledWater.Id, allowOverload: true), Is.False);
            Assert.That(gun.IsOverloaded, Is.True);
        }

        [Test]
        public void 과적하면_느려지고_스태미나가_빨리_닳고_공격이_느려진다()
        {
            for (int i = 0; i < 4; i++) gun.TryPickUp(ItemCatalog.BottledWater.Id); // 8kg, 한도
            Assert.That(gun.OverloadRatio, Is.EqualTo(0));
            Assert.That(gun.OverloadMoveSpeedMultiplier, Is.EqualTo(1));

            gun.TryPickUp(ItemCatalog.BottledWater.Id, allowOverload: true); // 10kg, 과적 절반
            Assert.That(gun.OverloadRatio, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(gun.OverloadMoveSpeedMultiplier, Is.EqualTo(0.8).Within(1e-9));
            Assert.That(gun.OverloadStaminaUseMultiplier, Is.EqualTo(1.3).Within(1e-9));
            Assert.That(gun.OverloadAttackSpeedMultiplier, Is.EqualTo(0.875).Within(1e-9));

            gun.TryPickUp(ItemCatalog.BottledWater.Id, allowOverload: true); // 12kg, 과적 한도
            Assert.That(gun.OverloadMoveSpeedMultiplier, Is.EqualTo(0.6).Within(1e-9));
            Assert.That(gun.OverloadStaminaUseMultiplier, Is.EqualTo(1.6).Within(1e-9));
            Assert.That(gun.OverloadAttackSpeedMultiplier, Is.EqualTo(0.75).Within(1e-9));
        }

        [Test]
        public void 나갈_때는_창고에서_무게_한도_안에서_꺼내_간다()
        {
            state.Storage.Add(ItemCatalog.Stone.Id, 30);
            Assert.That(state.TakeFromStorage(gun, ItemCatalog.Stone.Id, 16), Is.True);
            Assert.That(state.TakeFromStorage(gun, ItemCatalog.Stone.Id, 1), Is.False);
            Assert.That(state.Storage.Count(ItemCatalog.Stone.Id), Is.EqualTo(14));
        }

        [Test]
        public void 바깥에_있는_동안만_오염도가_오른다()
        {
            AdvanceTo(9);
            state.TryDepart(gun, Direction.North);
            state.Advance(10 * 60);
            Assert.That(gun.Contamination, Is.EqualTo(41).Within(1e-9));
            Assert.That(jip.Contamination, Is.EqualTo(1), "집 안에서는 오르지 않는다");
        }

        [Test]
        public void 늦게_돌아오면_다음_날_외출할_수_없다()
        {
            AdvanceTo(9);
            state.TryDepart(gun, Direction.North);
            state.Advance(10 * 60 + 10); // 오염도 약 41.7
            state.Return(gun);
            Assert.That(gun.RestDayPending, Is.True);

            Assert.That(state.SleepUntilMorning(), Is.True);
            AdvanceTo(9);
            Assert.That(state.TryDepart(gun, Direction.North), Is.EqualTo(DepartResult.Resting));

            Assert.That(state.SleepUntilMorning(), Is.True);
            AdvanceTo(9);
            Assert.That(state.TryDepart(gun, Direction.North), Is.EqualTo(DepartResult.Ok));
        }

        [Test]
        public void 밖에_누가_있으면_잘_수_없고_자면_아침_7시에_일어난다()
        {
            AdvanceTo(9);
            state.TryDepart(gun, Direction.North);
            Assert.That(state.SleepUntilMorning(), Is.False);
            state.Return(gun);

            double stressBefore = jip.Stress;
            Assert.That(state.SleepUntilMorning(), Is.True);
            Assert.That(state.Time.Day, Is.EqualTo(2));
            Assert.That(state.Time.Hour, Is.EqualTo(7));
            Assert.That(gun.WentOutToday, Is.False);
            Assert.That(jip.Stress, Is.GreaterThan(stressBefore));
        }

        /// <summary>태움 판정 난수를 차례로 돌려준다. 다 쓰면 fallback을 계속 돌려준다.</summary>
        private static System.Func<double> Rolls(double fallback, params double[] rolls)
        {
            var queue = new System.Collections.Generic.Queue<double>(rolls);
            return () => queue.Count > 0 ? queue.Dequeue() : fallback;
        }

        private const double Hit = 0.0;   // 태움 확률에 걸림 / 대상 목록의 첫 번째
        private const double Miss = 0.99; // 태움 확률에 걸리지 않음

        [Test]
        public void 태움은_집에_있는_사람_중_무작위_한_명에게만_발동한다()
        {
            // 1시간째: 발동, 대상 목록 [집냥이, 군냥이, 길냥이] 중 첫 번째. 그 뒤로는 발동하지 않음.
            state.HazingRoll = Rolls(Miss, Hit, 0.0);
            state.Advance(3 * 60);
            double timeStress = Balance.StressPerHomeHour * 3;
            Assert.That(jip.Stress, Is.EqualTo(timeStress + Balance.HazingStress).Within(1e-9));
            Assert.That(gil.Stress, Is.EqualTo(timeStress).Within(1e-9), "한 명에게만");
            Assert.That(gun.Stress, Is.EqualTo(timeStress * 0.5).Within(1e-9), "한 명에게만");
            Assert.That(ui.Stress, Is.EqualTo(timeStress).Within(1e-9), "의냥이 자신은 태움을 받지 않는다");
        }

        [Test]
        public void 태움은_상시도_매시간도_아니다()
        {
            state.HazingRoll = Rolls(Miss);
            state.Advance(6 * 60);
            double timeStress = Balance.StressPerHomeHour * 6;
            Assert.That(jip.Stress, Is.EqualTo(timeStress).Within(1e-9), "확률에 걸리지 않으면 아무 일도 없다");
            Assert.That(Balance.HazingChancePerHour, Is.LessThan(0.5));
        }

        [Test]
        public void 기본_난수로는_깨어_있는_동안_몇_번만_발동한다()
        {
            int hazed = 0;
            for (int hour = 0; hour < 15; hour++) // 07:00 → 22:00, 모두 집에 있음
            {
                double jb = jip.Stress, gb = gil.Stress, nb = gun.Stress;
                state.Advance(60);
                int thisHour = 0;
                if (jip.Stress - jb > Balance.StressPerHomeHour + 1e-9) thisHour++;
                if (gil.Stress - gb > Balance.StressPerHomeHour + 1e-9) thisHour++;
                if (gun.Stress - nb > Balance.StressPerHomeHour * 0.5 + 1e-9) thisHour++;
                Assert.That(thisHour, Is.LessThanOrEqualTo(1), "한 번에 한 명만");
                hazed += thisHour;
            }
            Assert.That(hazed, Is.InRange(1, 6), "깨어 있는 15시간 동안 몇 번뿐");
        }

        [Test]
        public void 군냥이는_얼차려로_태움을_거의_받지_않는다()
        {
            // 대상 목록 [집냥이, 군냥이, 길냥이]에서 두 번째
            state.HazingRoll = Rolls(Miss, Hit, 0.4);
            state.Advance(60);
            Assert.That(gun.Stress, Is.EqualTo((Balance.StressPerHomeHour + Balance.HazingStress) * 0.5).Within(1e-9));
            Assert.That(jip.Stress, Is.EqualTo(Balance.StressPerHomeHour).Within(1e-9));
        }

        [Test]
        public void 의냥이가_밖에_있으면_태움이_없다()
        {
            state.HazingRoll = Rolls(Hit);
            AdvanceTo(9);
            state.TryDepart(ui, Direction.South);
            double before = jip.Stress;
            state.Advance(3 * 60);
            Assert.That(jip.Stress - before, Is.EqualTo(Balance.StressPerHomeHour * 3).Within(1e-9));
        }

        [Test]
        public void 오늘_외출했다_돌아온_사람은_태움을_받지_않는다()
        {
            AdvanceTo(9);
            state.TryDepart(gil, Direction.East);
            state.Advance(60);
            state.Return(gil);
            // 매시간 발동하고 대상 목록의 마지막 사람을 골라도 길냥이는 목록에 없다
            state.HazingRoll = Rolls(Miss, Hit, 0.99, Hit, 0.99, Hit, 0.99);
            double before = gil.Stress;
            double multiplier = StressRules.GenerationMultiplier(gil); // 바깥 1시간 동안 오른 오염도만큼 스트레스가 더 쌓인다
            state.Advance(3 * 60);
            Assert.That(gil.Stress - before, Is.EqualTo(Balance.StressPerHomeHour * 3 * multiplier).Within(1e-9));
            Assert.That(gun.Stress, Is.GreaterThanOrEqualTo(Balance.HazingStress * 3 * 0.5), "다른 사람이 대신 받는다");
        }

        [Test]
        public void 자는_동안에는_태움이_없다()
        {
            var always = GameState.NewTutorial();
            var never = GameState.NewTutorial();
            foreach (var g in new[] { always, never })
            {
                g.HazingRoll = Rolls(Miss);
                g.Advance(15 * 60); // 22:00까지
            }
            always.HazingRoll = Rolls(Hit);
            double a0 = always.Get("jipnyang").Stress, n0 = never.Get("jipnyang").Stress;
            Assert.That(a0, Is.EqualTo(n0).Within(1e-9));

            Assert.That(always.SleepUntilMorning(), Is.True);
            Assert.That(never.SleepUntilMorning(), Is.True);
            Assert.That(always.Get("jipnyang").Stress - a0, Is.EqualTo(never.Get("jipnyang").Stress - n0).Within(1e-9));
        }

        [Test]
        public void 취미_물건으로_둘_이상이_함께_놀면_스트레스가_풀린다()
        {
            jip.Stress = 50;
            gil.Stress = 50;
            Assert.That(state.PlayHobby(new[] { jip, gil }, ItemCatalog.Hanafuda.Id, 1), Is.True);
            Assert.That(jip.Stress, Is.EqualTo(50 - Balance.ReliefHobbyPerHour).Within(1e-9));
            Assert.That(gil.Stress, Is.EqualTo(50 - Balance.ReliefHobbyPerHour).Within(1e-9));
            Assert.That(state.Storage.Count(ItemCatalog.Hanafuda.Id), Is.EqualTo(1), "화투는 닳지 않는다");
        }

        [Test]
        public void 혼자거나_밖에_있거나_취미_물건이_없으면_놀_수_없다()
        {
            jip.Stress = 50;
            Assert.That(state.PlayHobby(new[] { jip }, ItemCatalog.PlayingCards.Id, 1), Is.False, "혼자");
            Assert.That(state.PlayHobby(new[] { jip, jip }, ItemCatalog.PlayingCards.Id, 1), Is.False, "같은 사람 두 번");
            Assert.That(state.PlayHobby(new[] { jip, gil }, ItemCatalog.CannedFood.Id, 1), Is.False, "취미 물건이 아님");

            AdvanceTo(9);
            state.TryDepart(gil, Direction.East);
            Assert.That(state.PlayHobby(new[] { jip, gil }, ItemCatalog.PlayingCards.Id, 1), Is.False, "길냥이는 밖에 있음");
            state.Return(gil);

            state.Storage.Remove(ItemCatalog.PlayingCards.Id, 1);
            Assert.That(state.PlayHobby(new[] { jip, gil }, ItemCatalog.PlayingCards.Id, 1), Is.False, "트럼프 카드가 없음");
            Assert.That(state.PlayHobby(new[] { jip, gil }, ItemCatalog.Hanafuda.Id, 1), Is.True, "화투는 있음");
        }

        [Test]
        public void 동료가_죽으면_남은_사람들의_스트레스가_크게_쌓인다()
        {
            AdvanceTo(9);
            state.TryDepart(gun, Direction.North);
            gun.Die("테스트");
            double before = jip.Stress;
            state.Advance(1);
            Assert.That(jip.Stress - before, Is.GreaterThan(Balance.StressOnCompanionDeath - 1));
            Assert.That(state.Outings.IsOccupied(Direction.North), Is.False, "죽은 캐릭터의 방향은 풀린다");
            Assert.That(state.IsWorldOver, Is.False);
        }

        [Test]
        public void 집냥이가_죽으면_월드가_끝난다()
        {
            InfectionRules.Bite(jip); // 오염도 1 → 하루 +5, 20일 뒤 완전 변이
            state.Advance(20 * Balance.MinutesPerDay);
            Assert.That(jip.IsDead, Is.True);
            Assert.That(state.IsWorldOver, Is.True);
            Assert.That(state.WorldOverReason, Does.Contain("집냥이"));
        }
    }
}
