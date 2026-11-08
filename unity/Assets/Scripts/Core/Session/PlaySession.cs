using System;
using System.Collections.Generic;
using System.Linq;

namespace Catpo.Core
{
    /// <summary>
    /// 화면 하나가 쓰는 게임 진행. Unity 화면은 이걸 그려 주고 버튼을 이걸로 연결하기만 한다.
    /// - 실제 시간이 흐르면 게임 시간이 흐른다 (게임 1시간 = 실제 3분 [제안]). 빠르게 돌려 볼 수 있다.
    /// - 집 안 일은 바로 결과가 나오고, 그 사람은 걸리는 시간 동안 다른 일을 못 한다.
    /// - 일어난 일을 글로 남긴다 (돌아옴, 창고에 들어옴, 비, 정화 끊김, 죽음 등).
    /// </summary>
    public sealed class PlaySession
    {
        public const int MaxLogLines = 100;

        /// <summary>시간 배속: 멈춤, 보통(게임 1시간 = 실제 3분), 10배, 60배(게임 1시간 = 실제 3초).</summary>
        public static readonly IReadOnlyList<double> Speeds = new double[] { 0, 1, 10, 60 };

        public GameState State { get; }
        public Survivor Player { get; private set; }
        public bool HasChosenPlayer => Player != null;
        public double Speed { get; set; } = 1;
        public IReadOnlyList<string> Log => log;

        private readonly List<string> log = new List<string>();
        private double pendingMinutes;

        public PlaySession(GameState state)
        {
            State = state;
        }

        public static PlaySession NewTutorial(int seed = 20261108) => new PlaySession(GameState.NewTutorial(seed));

        public long Now => State.Time.TotalMinutes;

        // ── 시작 ───────────────────────────────────────────────────────

        /// <summary>내가 맡을 캐릭터를 고른다. 나머지는 AI가 맡는다.</summary>
        public void ChoosePlayer(Survivor s)
        {
            foreach (var x in State.Survivors) x.ControlledByPlayer = x == s;
            Player = s;
            Say($"{Korean.Ro(s.Name)} 시작한다. {ClockText}. 나머지 셋은 AI가 맡는다.");
        }

        // ── 시간 ───────────────────────────────────────────────────────

        /// <summary>실제 시간이 흐름. 캐릭터를 고르기 전, 멈춤, 월드가 끝난 뒤에는 흐르지 않는다.</summary>
        public void Tick(double realSeconds)
        {
            if (!HasChosenPlayer || State.IsWorldOver || Speed <= 0 || realSeconds <= 0) return;
            pendingMinutes += realSeconds * Speed * 60.0 / Balance.RealSecondsPerGameHour;
            int whole = (int)pendingMinutes;
            if (whole <= 0) return;
            pendingMinutes -= whole;
            PassTime(whole);
        }

        /// <summary>게임 시간을 바로 넘긴다. 그동안 생긴 일을 글로 남긴다.</summary>
        public void PassTime(int minutes)
        {
            if (State.IsWorldOver || minutes < 0) return;
            var before = new Snapshot(this);
            State.Advance(minutes);
            Report(before);
        }

        /// <summary>모두 잔다. 밖에 나간 사람이 있으면 잘 수 없다 [확정].</summary>
        public bool Sleep()
        {
            if (State.IsWorldOver) return false;
            var before = new Snapshot(this);
            if (!State.SleepUntilMorning())
            {
                Say("밖에 나간 사람이 돌아와야 잘 수 있다.");
                return false;
            }
            pendingMinutes = 0;
            Say($"모두 잤다. {ClockText}.");
            Report(before);
            return true;
        }

        public string ClockText => $"{State.Time.Day}일째 {Korean.SeasonName(State.Time.Season)} {State.Time.Hour:00}:{State.Time.Minute:00}";

        /// <summary>오늘이면 "13:22", 다른 날이면 "2일째 13:22".</summary>
        public string TimeText(long totalMinute)
        {
            long day = totalMinute / Balance.MinutesPerDay + 1;
            long m = totalMinute % Balance.MinutesPerDay;
            string hhmm = $"{m / 60:00}:{m % 60:00}";
            return day == State.Time.Day ? hhmm : $"{day}일째 {hhmm}";
        }

        public string OutingWindowText
        {
            get
            {
                var (start, end) = GameTime.OutingWindow(State.Time.Season);
                return $"외출 {start:00}:00~{end:00}:00 ({(State.Time.IsWithinOutingWindow ? "지금 가능" : "지금 불가")})";
            }
        }

        public string HouseProtectionText
        {
            get
            {
                long left = State.Home.HouseProtectedUntilMinute - Now;
                return left > 0 ? $"집 정화 {Korean.Duration(left / 60.0)} 남음" : "집 정화 끊김!";
            }
        }

        // ── 사람 상태 ──────────────────────────────────────────────────

        public bool IsBusy(Survivor s) => s.BusyUntilMinute > Now;

        public string LocationText(Survivor s)
        {
            if (s.IsDead) return "죽음";
            if (s.Location == SurvivorLocation.Home) return s.RestingToday ? "집 (오늘은 쉼)" : "집";
            string dir = s.OutingDirection.HasValue ? Korean.DirectionName(s.OutingDirection.Value) : "밖";
            return s.IsDispatched ? $"{dir} 파견 중, {TimeText(s.DispatchReturnAtMinute)}쯤 귀환" : $"{dir} 외출 중";
        }

        public string ActivityText(Survivor s)
        {
            if (s.IsDead || s.Location != SurvivorLocation.Home) return "";
            return IsBusy(s) ? $"{TimeText(s.BusyUntilMinute)}까지 일하는 중" : "할 일 기다리는 중";
        }

        // ── 행동 ───────────────────────────────────────────────────────

        public bool Eat(Survivor s, ItemDefinition item)
        {
            var r = State.Consume(s, item.Id);
            switch (r)
            {
                case ConsumeResult.Ok:
                    Say($"{Korean.IGa(s.Name)} {Korean.EulReul(item.Name)} {(item.Category == ItemCategory.Water ? "마셨다" : "먹었다")}.");
                    return true;
                case ConsumeResult.NeedsBoiling: Say("빗물은 끓여야 마실 수 있다."); return false;
                case ConsumeResult.NotAvailable: Say($"{Korean.IGa(item.Name)} 없다."); return false;
                case ConsumeResult.Dead: Say($"{Korean.IGa(s.Name)} 죽었다."); return false;
                default: Say($"{Korean.EunNeun(item.Name)} 먹을 수 없다."); return false;
            }
        }

        public bool Craft(Survivor s, Recipe r, HeatSource heat = HeatSource.None)
        {
            string what = r.NeedsHeat ? $"{r.Name} ({Korean.HeatName(heat)})" : r.Name;
            return HomeAction(s, what, () => CraftingRules.TryCraft(State, s, r, heat));
        }

        public bool BuildRainCollector(Survivor s, ItemDefinition container) =>
            HomeAction(s, $"빗물받이 만들기 ({container.Name})", () => HouseworkRules.BuildRainCollector(State, s, container.Id));

        public bool RepairFrontDoor(Survivor s, ItemDefinition material) =>
            HomeAction(s, $"현관 보수 ({material.Name})", () => HouseworkRules.RepairFrontDoor(State, s, material.Id));

        public bool PurifyHouse(Survivor s) =>
            HomeAction(s, "집 정화", () => HouseworkRules.PurifyHouse(State, s));

        public bool PurifyBody(Survivor helper, Survivor target) =>
            HomeAction(helper, helper == target ? "자기 몸 정화" : $"{target.Name} 몸 정화", () => HouseworkRules.PurifyBody(State, helper, target));

        /// <summary>둘이 함께 취미 물건으로 1시간 논다 [확정: 둘 이상].</summary>
        public bool PlayHobby(Survivor a, Survivor b, ItemDefinition hobby)
        {
            if (!Ready(a) || !Ready(b)) return false;
            if (!State.PlayHobby(new[] { a, b }, hobby.Id, 1))
            {
                Say($"{Korean.EulReul(hobby.Name)} 할 수 없다. 둘 다 집에 있어야 하고, {Korean.IGa(hobby.Name)} 창고에 있어야 한다.");
                return false;
            }
            MarkBusy(a, 1);
            MarkBusy(b, 1);
            Say($"{Korean.WaGwa(a.Name)} {Korean.IGa(b.Name)} {Korean.EulReul(hobby.Name)} 했다. 1시간, 스트레스가 풀렸다.");
            return true;
        }

        /// <summary>AI 파견 [확정]. 직접 맡은 캐릭터는 보낼 수 없다.</summary>
        public bool Dispatch(Survivor s, Direction d)
        {
            if (!Ready(s)) return false;
            var r = State.Dispatch(s, d);
            if (r != DepartResult.Ok)
            {
                Say(Korean.Depart(r, s));
                return false;
            }
            Say($"{Korean.IGa(s.Name)} {Korean.Ro(Korean.DirectionName(d))} 파견을 나갔다. {TimeText(s.DispatchReturnAtMinute)}쯤 돌아온다.");
            return true;
        }

        /// <summary>자동 파견 방향을 정한다 (null이면 끔). 조건이 맞으면 바로 나간다.</summary>
        public void SetAutoDispatch(Survivor s, Direction? d)
        {
            s.AutoDispatchDirection = d;
            Say(d.HasValue
                ? $"{Korean.EunNeun(s.Name)} 이제 {Korean.Ro(Korean.DirectionName(d.Value))} 알아서 파견을 나간다."
                : $"{Korean.EunNeun(s.Name)} 이제 알아서 나가지 않는다.");
            PassTime(0);
        }

        private bool Ready(Survivor s)
        {
            if (State.IsWorldOver) return false;
            if (s.IsDead) { Say($"{Korean.IGa(s.Name)} 죽었다."); return false; }
            if (s.Location != SurvivorLocation.Home) { Say($"{Korean.EunNeun(s.Name)} 집에 없다."); return false; }
            if (IsBusy(s)) { Say($"{Korean.EunNeun(s.Name)} {TimeText(s.BusyUntilMinute)}까지 다른 일을 하는 중이다."); return false; }
            return true;
        }

        private void MarkBusy(Survivor s, double hours)
        {
            s.BusyUntilMinute = Now + (long)Math.Ceiling(hours * 60 - 1e-9);
        }

        private bool HomeAction(Survivor s, string what, Func<ActionResult> act)
        {
            if (!Ready(s)) return false;
            var r = act();
            if (!r.Success)
            {
                Say($"{s.Name}: {what} 못 함. {Korean.Failure(r.Failure)}.");
                if (r.NeedMaterialsFromOutside) Say("집 안에는 더 없다. 밖에서도 가져와야 한다.");
                return false;
            }
            MarkBusy(s, r.Hours);
            Say($"{s.Name}: {what}, {Korean.Duration(r.Hours)}.");
            if (r.NeedMaterialsFromOutside) Say("집 안에는 더 없다. 밖에서도 가져와야 한다.");
            return true;
        }

        // ── 일어난 일 글로 남기기 ──────────────────────────────────────

        public void Say(string line)
        {
            log.Add(line);
            if (log.Count > MaxLogLines) log.RemoveRange(0, log.Count - MaxLogLines);
        }

        private sealed class Snapshot
        {
            public readonly Dictionary<string, int> Storage;
            public readonly Dictionary<Survivor, (SurvivorLocation Location, Direction? Dir, bool Dead)> People;
            public readonly int Day;
            public readonly bool Protected;

            public Snapshot(PlaySession p)
            {
                Storage = p.State.Storage.Items.ToDictionary(kv => kv.Key, kv => kv.Value);
                People = p.State.Survivors.ToDictionary(s => s, s => (s.Location, s.OutingDirection, s.IsDead));
                Day = p.State.Time.Day;
                Protected = p.State.Home.IsHouseProtected(p.Now);
            }
        }

        private void Report(Snapshot before)
        {
            foreach (var s in State.Survivors)
            {
                var was = before.People[s];
                if (!was.Dead && s.IsDead)
                {
                    Say($"{Korean.IGa(s.Name)} 죽었다.{(string.IsNullOrEmpty(s.DeathCause) ? "" : $" ({s.DeathCause})")}");
                    continue;
                }
                if (was.Location == SurvivorLocation.Outside && s.Location == SurvivorLocation.Home)
                    Say($"{Korean.IGa(s.Name)} {(was.Dir.HasValue ? Korean.DirectionName(was.Dir.Value) + "에서 " : "")}돌아왔다.");
                if (was.Location == SurvivorLocation.Home && s.Location == SurvivorLocation.Outside && s.IsDispatched && s.OutingDirection.HasValue)
                    Say($"{Korean.IGa(s.Name)} {Korean.Ro(Korean.DirectionName(s.OutingDirection.Value))} 파견을 나갔다. {TimeText(s.DispatchReturnAtMinute)}쯤 돌아온다.");
            }

            var gained = new List<string>();
            foreach (var kv in State.Storage.Items)
            {
                before.Storage.TryGetValue(kv.Key, out int old);
                if (kv.Value > old) gained.Add($"{ItemCatalog.Get(kv.Key).Name} {kv.Value - old}");
            }
            if (gained.Count > 0) Say("창고에 들어옴: " + string.Join(", ", gained));

            if (State.Time.Day != before.Day && State.Home.RainedToday) Say($"{State.Time.Day}일째, 비가 온다.");
            if (before.Protected && !State.Home.IsHouseProtected(Now)) Say("집 정화가 끊겼다! 집 안에서도 오염도가 오른다.");
            if (State.IsWorldOver) Say($"월드가 끝났다: {State.WorldOverReason}");
        }
    }
}
