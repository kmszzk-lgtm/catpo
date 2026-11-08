using System.Linq;
using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class PlaySessionTests
    {
        private PlaySession play;
        private GameState state;
        private Survivor jip, gun, ui, gil;

        [SetUp]
        public void SetUp()
        {
            play = PlaySession.NewTutorial();
            state = play.State;
            jip = state.Get("jipnyang");
            gun = state.Get("gunnyang");
            ui = state.Get("uinyang");
            gil = state.Get("gilnyang");
            state.HazingRoll = () => 0.99; // 태움 없이 계산을 단순하게
        }

        private string LastLog => play.Log.Last();

        // ── 조사 ───────────────────────────────────────────────────────

        [Test]
        public void 이름_받침에_맞춰_조사를_붙인다()
        {
            Assert.That(Korean.IGa("집냥이"), Is.EqualTo("집냥이가"));
            Assert.That(Korean.IGa("라면"), Is.EqualTo("라면이"));
            Assert.That(Korean.EunNeun("군냥이"), Is.EqualTo("군냥이는"));
            Assert.That(Korean.EulReul("화투"), Is.EqualTo("화투를"));
            Assert.That(Korean.EulReul("트럼프 카드"), Is.EqualTo("트럼프 카드를"));
            Assert.That(Korean.WaGwa("집냥이"), Is.EqualTo("집냥이와"));
            Assert.That(Korean.Ro("북쪽"), Is.EqualTo("북쪽으로"));
            Assert.That(Korean.Ro("길"), Is.EqualTo("길로"), "ㄹ 받침은 '로'");
            Assert.That(Korean.Duration(0.75), Is.EqualTo("45분"));
            Assert.That(Korean.Duration(1.5), Is.EqualTo("1시간 30분"));
        }

        // ── 시간 ───────────────────────────────────────────────────────

        [Test]
        public void 캐릭터를_고르기_전에는_시간이_흐르지_않는다()
        {
            play.Tick(Balance.RealSecondsPerGameHour);
            Assert.That(state.Time.Hour, Is.EqualTo(7));
            play.ChoosePlayer(jip);
            Assert.That(jip.ControlledByPlayer, Is.True);
            Assert.That(gun.ControlledByPlayer, Is.False);
            Assert.That(LastLog, Does.StartWith("집냥이로 시작한다"));
        }

        [Test]
        public void 실제_3분이_게임_1시간이고_시간을_바꾸는_기능은_없다()
        {
            play.ChoosePlayer(jip);
            play.Tick(Balance.RealSecondsPerGameHour);
            Assert.That(state.Time.Hour, Is.EqualTo(8));
            Assert.That(state.Time.Minute, Is.EqualTo(0));

            // 1분이 안 되는 짧은 시간도 모였다가 흐른다
            for (int i = 0; i < 60; i++) play.Tick(Balance.RealSecondsPerGameHour / 60.0 / 2);
            Assert.That(state.Time.MinuteOfDay, Is.EqualTo(8 * 60 + 30));

            // 멀티에서 호스트가 시간을 바꾸면 안 된다 [확정]: 배속·멈춤이 없다
            Assert.That(typeof(PlaySession).GetProperty("Speed"), Is.Null);
            Assert.That(typeof(PlaySession).GetField("Speeds"), Is.Null);
        }

        // ── 집 안 일 ───────────────────────────────────────────────────

        [Test]
        public void 집_안_일을_하면_걸리는_시간_동안_다른_일을_못_한다()
        {
            play.ChoosePlayer(jip);
            state.Storage.Add(ItemCatalog.RainWater.Id, 4);
            Assert.That(play.Craft(jip, RecipeCatalog.BoilWater, HeatSource.Campfire), Is.True);
            Assert.That(play.IsBusy(jip), Is.True);
            Assert.That(LastLog, Does.StartWith("집냥이: 물 끓이기 (모닥불)"));

            Assert.That(play.Craft(jip, RecipeCatalog.BoilWater, HeatSource.GasBurner), Is.False);
            Assert.That(LastLog, Does.Contain("다른 일을 하는 중"));
            Assert.That(play.Craft(gil, RecipeCatalog.BoilWater, HeatSource.GasBurner), Is.True, "다른 사람은 할 수 있다");

            play.PassTime((int)(jip.BusyUntilMinute - state.Time.TotalMinutes));
            Assert.That(play.IsBusy(jip), Is.False);
        }

        [Test]
        public void 실패하면_이유를_알려_주고_바빠지지_않는다()
        {
            play.ChoosePlayer(jip);
            Assert.That(play.Craft(jip, RecipeCatalog.PurifierFromDisinfectant), Is.False);
            Assert.That(LastLog, Does.Contain("할 줄 모른다"));
            Assert.That(play.IsBusy(jip), Is.False);

            Assert.That(play.Eat(jip, ItemCatalog.RainWater), Is.False);
            Assert.That(LastLog, Does.Contain("끓여야"));
        }

        [Test]
        public void 먹고_마신다()
        {
            play.ChoosePlayer(jip);
            play.PassTime(10 * 60);
            double hunger = jip.Hunger;
            Assert.That(play.Eat(jip, ItemCatalog.CannedFood), Is.True);
            Assert.That(jip.Hunger, Is.LessThan(hunger));
            Assert.That(LastLog, Is.EqualTo("집냥이가 통조림을 먹었다."));
            Assert.That(play.Eat(jip, ItemCatalog.BottledWater), Is.True);
            Assert.That(LastLog, Is.EqualTo("집냥이가 생수를 마셨다."));
        }

        [Test]
        public void 화투는_둘이_함께_하고_둘_다_1시간_바쁘다()
        {
            play.ChoosePlayer(jip);
            jip.Stress = gil.Stress = 30;
            Assert.That(play.PlayHobby(jip, gil, ItemCatalog.Hanafuda), Is.True);
            Assert.That(jip.Stress, Is.LessThan(30));
            Assert.That(play.IsBusy(jip) && play.IsBusy(gil), Is.True);
            Assert.That(LastLog, Does.StartWith("집냥이와 길냥이가 화투를 했다"));
            Assert.That(play.PlayHobby(jip, ui, ItemCatalog.PlayingCards), Is.False, "집냥이는 아직 화투 중");
        }

        // ── 파견과 일어난 일 ────────────────────────────────────────────

        [Test]
        public void 파견을_보내면_돌아올_때_창고에_들어온_것을_알려_준다()
        {
            play.ChoosePlayer(jip);
            play.PassTime(2 * 60); // 09:00
            Assert.That(play.Dispatch(gun, Direction.North), Is.True);
            Assert.That(LastLog, Does.StartWith("군냥이가 북쪽으로 파견을 나갔다"));
            Assert.That(play.LocationText(gun), Does.StartWith("북쪽 파견 중"));

            play.PassTime(6 * 60);
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Home));
            Assert.That(play.Log.Any(l => l == "군냥이가 북쪽에서 돌아왔다."), Is.True);
            Assert.That(play.Log.Any(l => l.StartsWith("창고에 들어옴: ") && l.Contains("라면 12")), Is.True);
        }

        [Test]
        public void 직접_맡은_캐릭터는_파견할_수_없고_이유를_알려_준다()
        {
            play.ChoosePlayer(gun);
            play.PassTime(2 * 60);
            Assert.That(play.Dispatch(gun, Direction.North), Is.False);
            Assert.That(LastLog, Does.Contain("직접 맡은 캐릭터"));
            Assert.That(play.Dispatch(jip, Direction.North), Is.False);
            Assert.That(LastLog, Is.EqualTo("집냥이는 밖에 나갈 수 없다."));
        }

        [Test]
        public void 자동_파견은_집_안_일을_마친_뒤에_나간다()
        {
            play.ChoosePlayer(jip);
            play.PassTime(2 * 60); // 09:00
            Assert.That(play.RepairFrontDoor(gun, ItemCatalog.WoodStick), Is.True);
            play.SetAutoDispatch(gun, Direction.North);
            Assert.That(gun.Location, Is.EqualTo(SurvivorLocation.Home), "아직 현관 보수 중");

            play.PassTime((int)(gun.BusyUntilMinute - state.Time.TotalMinutes));
            Assert.That(gun.IsDispatched, Is.True);
            Assert.That(LastLog, Does.StartWith("군냥이가 북쪽으로 파견을 나갔다"));
        }

        [Test]
        public void 밖에_사람이_있으면_잘_수_없고_모두_오면_아침까지_잔다()
        {
            play.ChoosePlayer(jip);
            play.PassTime(2 * 60);
            play.Dispatch(gun, Direction.North);
            Assert.That(play.Sleep(), Is.False);
            Assert.That(LastLog, Is.EqualTo("밖에 나간 사람이 돌아와야 잘 수 있다."));

            play.PassTime(10 * 60);
            Assert.That(play.Sleep(), Is.True);
            Assert.That(state.Time.Day, Is.EqualTo(2));
            Assert.That(play.Log.Any(l => l.StartsWith("모두 잤다. 2일째 봄 07:00")), Is.True);
        }

        [Test]
        public void 집_정화가_끊기거나_월드가_끝나면_알려_준다()
        {
            play.ChoosePlayer(jip);
            play.PassTime(24 * 60);
            Assert.That(play.Log.Any(l => l.StartsWith("집 정화가 끊겼다")), Is.True);
            Assert.That(play.HouseProtectionText, Is.EqualTo("집 정화 끊김!"));

            for (int i = 0; i < 40 && !state.IsWorldOver; i++) play.PassTime(24 * 60);
            Assert.That(state.IsWorldOver, Is.True);
            Assert.That(play.Log.Any(l => l.StartsWith("월드가 끝났다")), Is.True);
            int before = state.Time.Day;
            play.Tick(10000);
            Assert.That(state.Time.Day, Is.EqualTo(before), "월드가 끝나면 시간이 멈춘다");
        }

        [Test]
        public void 글은_최근_100줄만_남긴다()
        {
            for (int i = 0; i < 150; i++) play.Say($"줄 {i}");
            Assert.That(play.Log.Count, Is.EqualTo(PlaySession.MaxLogLines));
            Assert.That(play.Log.First(), Is.EqualTo("줄 50"));
        }
    }
}
