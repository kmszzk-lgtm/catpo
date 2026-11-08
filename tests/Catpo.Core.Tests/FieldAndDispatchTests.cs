using System.Linq;
using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class FieldAndDispatchTests
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
            state.Advance(2 * 60); // 09:00
        }

        private LootSpot Spot(string id) => state.Field.Get(id);

        // ── 직접 수색 ──────────────────────────────────────────────────

        [Test]
        public void 직접_수색은_그_방향에_나가_있어야_하고_무게_한도_안에서_줍는다()
        {
            var store = Spot("north_conv_a");
            state.TryDepart(gun, Direction.North);
            state.TryDepart(gil, Direction.East);

            Assert.That(state.Field.Take(gil, store, ItemCatalog.CannedFood.Id, 1), Is.EqualTo(0), "다른 방향의 오브젝트");
            Assert.That(state.Field.Take(gun, store, ItemCatalog.CannedFood.Id, 6), Is.EqualTo(6));
            Assert.That(state.Field.Take(gun, store, ItemCatalog.BottledWater.Id, 6), Is.EqualTo(4), "12kg 한도");
            Assert.That(store.Contents.Count(ItemCatalog.BottledWater.Id), Is.EqualTo(2));
        }

        [Test]
        public void 수거한_물자는_다시_생기지_않는다()
        {
            var store = Spot("north_conv_a");
            state.TryDepart(gun, Direction.North);
            state.Field.Take(gun, store, ItemCatalog.CannedFood.Id, 6);
            state.Return(gun);
            for (int i = 0; i < 5; i++) state.SleepUntilMorning();
            Assert.That(store.Contents.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(0));
        }

        // ── AI 파견 ────────────────────────────────────────────────────

        [Test]
        public void 집냥이는_파견할_수_없다()
        {
            Assert.That(state.Dispatch(jip, Direction.North), Is.EqualTo(DepartResult.CannotGoOutside));
        }

        [Test]
        public void AI_파견은_방향을_차지하고_해_지기_1시간_전에_돌아온다()
        {
            Assert.That(state.Dispatch(gun, Direction.North), Is.EqualTo(DepartResult.Ok));
            Assert.That(state.TryDepart(ui, Direction.North), Is.EqualTo(DepartResult.DirectionOccupied));

            state.Advance(8 * 60 - 1); // 16:59
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Outside));
            state.Advance(1); // 17:00 (봄 18:00 - 1시간)
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(gun.IsDispatched, Is.False);
            Assert.That(state.Outings.IsOccupied(Direction.North), Is.False);
            Assert.That(gun.Contamination, Is.EqualTo(1 + 8 * Balance.ContaminationPerOutsideHour).Within(1e-9));
        }

        [Test]
        public void 길냥이는_한_시간_일찍_돌아온다()
        {
            state.Dispatch(gil, Direction.East);
            state.Advance(7 * 60 - 1); // 15:59
            Assert.That(gil.Location, Is.EqualTo(SurvivorLocation.Outside));
            state.Advance(1); // 16:00
            Assert.That(gil.Location, Is.EqualTo(SurvivorLocation.Home));
        }

        [Test]
        public void AI는_가까운_오브젝트를_없애고_잔해물을_가져온다()
        {
            int canned = state.Storage.Count(ItemCatalog.CannedFood.Id);
            int noodle = state.Storage.Count(ItemCatalog.InstantNoodle.Id);
            int water = state.Storage.Count(ItemCatalog.BottledWater.Id);
            int stone = state.Storage.Count(ItemCatalog.Stone.Id);

            state.Dispatch(gun, Direction.North);
            state.Advance(8 * 60); // 8시간 → 오브젝트 2곳

            Assert.That(Spot("north_conv_a").IsRemoved, Is.True);
            Assert.That(Spot("north_car").IsRemoved, Is.True);
            Assert.That(Spot("north_conv_b").IsRemoved, Is.False);
            Assert.That(state.Field.Remaining(Direction.North).First().Id, Is.EqualTo("north_conv_b"));

            // 편의점 A: 통조림 6 → 2, 라면 8 → 3, 생수 6 → 2 / 길가 차량: 돌 4 → 2 (40%, 반올림)
            Assert.That(state.Storage.Count(ItemCatalog.CannedFood.Id) - canned, Is.EqualTo(2));
            Assert.That(state.Storage.Count(ItemCatalog.InstantNoodle.Id) - noodle, Is.EqualTo(3));
            Assert.That(state.Storage.Count(ItemCatalog.BottledWater.Id) - water, Is.EqualTo(2));
            Assert.That(state.Storage.Count(ItemCatalog.Stone.Id) - stone, Is.EqualTo(2));
        }

        [Test]
        public void 다음_파견은_더_멀리_간다()
        {
            state.Dispatch(gun, Direction.North);
            state.Advance(8 * 60);
            state.SleepUntilMorning();
            state.Advance(2 * 60); // 다음 날 09:00
            state.Dispatch(gun, Direction.North);
            state.Advance(8 * 60);
            Assert.That(Spot("north_conv_b").IsRemoved, Is.True);
            Assert.That(Spot("north_hardware").IsRemoved, Is.True);
            Assert.That(Spot("north_house_1").IsRemoved, Is.False);
        }

        [Test]
        public void 남은_오브젝트가_없으면_빈손으로_돌아온다()
        {
            foreach (var spot in state.Field.Remaining(Direction.West).ToList()) spot.IsRemoved = true;
            int before = state.Storage.Items.Values.Sum();
            state.Dispatch(ui, Direction.West);
            state.Advance(8 * 60);
            Assert.That(ui.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(state.Storage.Items.Values.Sum(), Is.EqualTo(before));
        }

        [Test]
        public void 파견을_도중에_불러들이면_그때까지의_결과만_가져온다()
        {
            state.Dispatch(gun, Direction.North);
            state.Advance(2 * 60);
            state.Return(gun);
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(Spot("north_conv_a").IsRemoved, Is.True);
            Assert.That(Spot("north_car").IsRemoved, Is.False);
        }
    }
}
