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

        private int Stored(ItemDefinition item) => state.Storage.Count(item.Id);

        // ── 직접 수색 ──────────────────────────────────────────────────

        [Test]
        public void 사람은_직접_수색할_때_과적까지_줍는다()
        {
            var store = Spot("north_conv_a");
            state.TryDepart(gun, Direction.North);
            state.TryDepart(gil, Direction.East);

            Assert.That(state.Field.Take(gil, store, ItemCatalog.CannedFood.Id, 1), Is.EqualTo(0), "다른 방향의 오브젝트");
            Assert.That(state.Field.Take(gun, store, ItemCatalog.CannedFood.Id, 8), Is.EqualTo(8));     // 3.2kg
            Assert.That(state.Field.Take(gun, store, ItemCatalog.BottledWater.Id, 8), Is.EqualTo(4));   // 11.2kg, 과적 한도 12kg
            Assert.That(gun.IsOverloaded, Is.True);
            Assert.That(store.Contents.Count(ItemCatalog.BottledWater.Id), Is.EqualTo(4));
        }

        [Test]
        public void 수거한_물자는_다시_생기지_않는다()
        {
            var store = Spot("north_conv_a");
            state.TryDepart(gun, Direction.North);
            state.Field.Take(gun, store, ItemCatalog.CannedFood.Id, 8);
            state.Return(gun);
            for (int i = 0; i < 5; i++) state.SleepUntilMorning();
            Assert.That(store.Contents.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(0));
        }

        // ── AI 파견 ────────────────────────────────────────────────────

        [Test]
        public void 집냥이와_사람이_맡은_캐릭터는_파견할_수_없다()
        {
            Assert.That(state.Dispatch(jip, Direction.North), Is.EqualTo(DepartResult.CannotGoOutside));
            gun.ControlledByPlayer = true;
            Assert.That(state.Dispatch(gun, Direction.North), Is.EqualTo(DepartResult.PlayerControlled));
        }

        [Test]
        public void AI는_과적하지_않고_가방이_차면_돌아오며_못_가져온_물자는_남는다()
        {
            Assert.That(state.Dispatch(gun, Direction.North), Is.EqualTo(DepartResult.Ok));
            Assert.That(state.TryDepart(ui, Direction.North), Is.EqualTo(DepartResult.DirectionOccupied));

            // 편의점 A는 물자가 많아서 가방이 금방 찬다 → 13:22에 돌아옴 (계산은 docs/02 참고)
            state.Advance(4 * 60 + 21); // 13:21
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Outside));
            state.Advance(1); // 13:22
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(state.Outings.IsOccupied(Direction.North), Is.False);

            var store = Spot("north_conv_a");
            Assert.That(store.IsEmpty, Is.False, "못 가져온 물자는 사라지지 않는다");
            Assert.That(store.Contents.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(5));
            Assert.That(store.Contents.Count(ItemCatalog.BottledWater.Id), Is.EqualTo(6));
            Assert.That(store.Contents.Count(ItemCatalog.InstantNoodle.Id), Is.EqualTo(0));
        }

        [Test]
        public void 물자가_많은_곳은_빨리_오고_적은_곳은_늦게_온다()
        {
            var field = new FieldMap();
            field.AddSpot("rich", "가득 찬 창고", Direction.North, 0, (ItemCatalog.CannedFood.Id, 40));
            field.AddSpot("sparse_0", "빈집", Direction.South, 0, (ItemCatalog.CannedFood.Id, 1));
            field.AddSpot("sparse_3", "빈집", Direction.South, 3, (ItemCatalog.CannedFood.Id, 1));
            field.AddSpot("sparse_6", "빈집", Direction.South, 6, (ItemCatalog.CannedFood.Id, 1));
            var a = new Survivor(CharacterCatalog.Gunnyang);
            var b = new Survivor(CharacterCatalog.Uinyang);
            var s = new GameState(new GameTime(1, 9), new[] { a, b }, field);

            s.Dispatch(a, Direction.North);
            s.Dispatch(b, Direction.South);
            long start = s.Time.TotalMinutes;
            Assert.That(a.DispatchReturnAtMinute - start, Is.EqualTo(20 + 20 * 6 + 20), "통조림 20개(8kg)로 가방이 참");
            Assert.That(b.DispatchReturnAtMinute - start, Is.EqualTo(158 + 140), "멀리까지 찾으러 감");

            s.Advance(8 * 60);
            Assert.That(s.Storage.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(20 + 3));
            Assert.That(field.Get("rich").Contents.Count(ItemCatalog.CannedFood.Id), Is.EqualTo(20));
        }

        [Test]
        public void 남은_물자가_없으면_하루_종일_찾다가_빈손으로_돌아온다()
        {
            foreach (var spot in state.Field.All.Where(x => x.Direction == Direction.West)) spot.Contents.MoveAllTo(new Inventory());
            int before = state.Storage.Items.Values.Sum();
            state.Dispatch(ui, Direction.West);
            Assert.That(ui.DispatchReturnAtMinute, Is.EqualTo(state.Time.TotalMinutes + 8 * 60), "17:00");
            state.Advance(8 * 60);
            Assert.That(ui.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(state.Storage.Items.Values.Sum(), Is.EqualTo(before));
        }

        [Test]
        public void 파견을_도중에_불러들이면_그때까지_주운_것만_가져온다()
        {
            int noodles = Stored(ItemCatalog.InstantNoodle);
            state.Dispatch(gun, Direction.North);
            state.Advance(60);
            state.Return(gun);
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(Stored(ItemCatalog.InstantNoodle) - noodles, Is.EqualTo(6), "이동 20분 + 6분마다 1개");
            Assert.That(Spot("north_conv_a").Contents.Count(ItemCatalog.InstantNoodle.Id), Is.EqualTo(6));
        }

        // ── 자동 파견 ──────────────────────────────────────────────────

        [Test]
        public void 자동_파견은_돌아오면_다시_나간다()
        {
            int water = Stored(ItemCatalog.BottledWater);
            gun.AutoDispatchDirection = Direction.North;
            state.Advance(0);
            Assert.That(gun.IsDispatched, Is.True, "외출 가능 시간이면 바로 나감");

            state.Advance(8 * 60); // 17:00
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(Stored(ItemCatalog.BottledWater) - water, Is.GreaterThanOrEqualTo(6), "여러 번 오가며 생수를 날라 옴");
            Assert.That(gun.Contamination, Is.GreaterThan(1 + 4 * Balance.ContaminationPerOutsideHour), "자주 나갔다 온 만큼 오염됨");
        }

        [Test]
        public void 자동_파견은_다음_날_외출_가능_시간이_되면_나간다()
        {
            state.Advance(9 * 60); // 18:00, 외출 가능 시간이 끝남
            gil.AutoDispatchDirection = Direction.North;
            state.Advance(0);
            Assert.That(gil.IsDispatched, Is.False, "외출 가능 시간이 아니면 나가지 않음");

            state.SleepUntilMorning(); // 다음 날 07:00
            state.Advance(119);
            Assert.That(gil.Location, Is.EqualTo(SurvivorLocation.Home), "08:59에는 아직");
            state.Advance(1);
            Assert.That(gil.IsDispatched, Is.True, "09:00에 자동으로 나감");
        }

        [Test]
        public void 자동_파견은_오염도가_높으면_쉬게_한다()
        {
            InfectionRules.AddContamination(gun, Balance.AutoDispatchContaminationLimit);
            gun.AutoDispatchDirection = Direction.North;
            state.Advance(0);
            Assert.That(gun.IsDispatched, Is.False);
        }
    }
}
