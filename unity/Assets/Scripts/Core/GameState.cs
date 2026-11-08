using System;
using System.Collections.Generic;
using System.Linq;

namespace Catpo.Core
{
    /// <summary>
    /// 월드 하나의 전체 상태. 저장과 나중의 멀티 동기화는 이 상태를 기준으로 한다 (docs/06-tech.md).
    /// </summary>
    public sealed class GameState
    {
        public GameTime Time { get; }
        public IReadOnlyList<Survivor> Survivors => survivors;
        /// <summary>공용 창고. 집에 오면 소지품이 자동으로 들어온다 [확정].</summary>
        public Inventory Storage { get; } = new Inventory();
        public OutingBoard Outings { get; } = new OutingBoard();

        /// <summary>지역 보스가 있으면 하루 +5 [확정]. 첫 버전에서는 0.</summary>
        public double RegionExtraContaminationPerDay { get; set; }

        public bool IsWorldOver { get; private set; }
        public string WorldOverReason { get; private set; }

        private readonly List<Survivor> survivors;
        private readonly HashSet<Survivor> deathsHandled = new HashSet<Survivor>();
        private double hazingHours;

        public GameState(GameTime time, IEnumerable<Survivor> survivors)
        {
            Time = time;
            this.survivors = survivors.ToList();
        }

        /// <summary>튜토리얼 모드 새 월드: 1일째(봄) 아침, 기본 캐릭터 4명.</summary>
        public static GameState NewTutorial()
        {
            var state = new GameState(new GameTime(1, 7), CharacterCatalog.Starting.Select(d => new Survivor(d)));
            // 의냥이 시작 아이템 [확정]: 응급처치 도구 여러 개, 상비약, 진통제. 개수는 [제안].
            state.Storage.Add(ItemCatalog.FirstAidKit.Id, 3);
            state.Storage.Add(ItemCatalog.HouseholdMedicine.Id, 5);
            state.Storage.Add(ItemCatalog.Painkiller.Id, 5);
            return state;
        }

        public Survivor Get(string id) => survivors.First(s => s.Id == id);

        public IEnumerable<Survivor> Alive => survivors.Where(s => !s.IsDead);

        public IEnumerable<Survivor> AtHome => Alive.Where(s => s.Location == SurvivorLocation.Home);

        // ── 외출 ───────────────────────────────────────────────────────

        public DepartResult CanDepart(Survivor s, Direction direction)
        {
            if (s.IsDead) return DepartResult.Dead;
            if (!s.CanGoOutside) return DepartResult.CannotGoOutside;
            if (s.Location == SurvivorLocation.Outside) return DepartResult.AlreadyOutside;
            if (s.RestingToday) return DepartResult.Resting;
            if (!Time.IsWithinOutingWindow) return DepartResult.OutsideWindowClosed;
            if (Outings.IsOccupied(direction)) return DepartResult.DirectionOccupied;
            return DepartResult.Ok;
        }

        public DepartResult TryDepart(Survivor s, Direction direction)
        {
            var result = CanDepart(s, direction);
            if (result != DepartResult.Ok) return result;
            s.Location = SurvivorLocation.Outside;
            s.OutingDirection = direction;
            s.WentOutToday = true;
            Outings.Occupy(direction, s);
            return DepartResult.Ok;
        }

        /// <summary>
        /// 집으로 돌아온다. 언제든 자유 [확정]. 방향이 풀리고, 소지품은 공용 창고로 들어간다 [확정].
        /// 오염도가 기준을 넘었으면 다음 날 외출 불가.
        /// </summary>
        public void Return(Survivor s)
        {
            if (s.Location != SurvivorLocation.Outside) return;
            if (s.OutingDirection.HasValue) Outings.Release(s.OutingDirection.Value);
            s.Location = SurvivorLocation.Home;
            s.OutingDirection = null;
            s.Inventory.MoveAllTo(Storage);
            if (InfectionRules.NeedsRestAfterReturn(s)) s.RestDayPending = true;
        }

        /// <summary>나갈 때 공용 창고에서 꺼내 간다 [확정]. 무게 한도를 넘으면 실패.</summary>
        public bool TakeFromStorage(Survivor s, string itemId, int amount = 1)
        {
            if (Storage.Count(itemId) < amount) return false;
            if (!s.TryPickUp(itemId, amount)) return false;
            Storage.Remove(itemId, amount);
            return true;
        }

        // ── 시간 ───────────────────────────────────────────────────────

        /// <summary>시간을 흘린다. 자정마다 하루치 감염 진행을 처리한다.</summary>
        public void Advance(int minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            while (minutes > 0 && !IsWorldOver)
            {
                int toMidnight = Balance.MinutesPerDay - Time.MinuteOfDay;
                int chunk = Math.Min(minutes, toMidnight);
                ApplyElapsed(chunk / 60.0);
                int midnights = Time.Advance(chunk);
                minutes -= chunk;
                if (midnights > 0) DailyTick();
                ResolveDeaths();
            }
        }

        /// <summary>
        /// 모두 잠자리에 들면 아침으로 넘어간다 [확정]. 혼자일 때도 AI 캐릭터를 재운 뒤 부른다.
        /// 밖에 나가 있는 캐릭터가 있으면 잘 수 없다.
        /// </summary>
        public bool SleepUntilMorning(int wakeHour = 7)
        {
            if (Alive.Any(s => s.Location == SurvivorLocation.Outside)) return false;
            int minutesToWake = (Balance.MinutesPerDay - Time.MinuteOfDay) + wakeHour * 60;
            if (minutesToWake >= Balance.MinutesPerDay) minutesToWake -= Balance.MinutesPerDay;
            if (minutesToWake == 0) minutesToWake = Balance.MinutesPerDay;
            Advance(minutesToWake);
            Wake();
            return true;
        }

        private void Wake()
        {
            foreach (var s in Alive)
            {
                StressRules.OnWake(s);
                s.WentOutToday = false;
                s.RestingToday = s.RestDayPending;
                s.RestDayPending = false;
            }
        }

        private void ApplyElapsed(double hours)
        {
            if (hours <= 0) return;
            foreach (var s in Alive)
            {
                if (s.Location == SurvivorLocation.Outside) InfectionRules.ExposeOutside(s, hours);
                StressRules.OnTimePassed(s, hours);
            }
            ApplyHazing(hours);
        }

        /// <summary>
        /// 태움: 의냥이가 집에 있으면, 외출하지 않고 집에 있는 사람에게 주기적으로 스트레스 [확정].
        /// 의냥이가 집 밖에 있으면 생기지 않는다.
        /// </summary>
        private void ApplyHazing(double hours)
        {
            var hazers = AtHome.Where(s => s.Effects.HazesStayHome).ToList();
            if (hazers.Count == 0)
            {
                hazingHours = 0;
                return;
            }
            hazingHours += hours;
            while (hazingHours >= Balance.HazingIntervalHours)
            {
                hazingHours -= Balance.HazingIntervalHours;
                foreach (var target in AtHome.Where(s => !hazers.Contains(s) && !s.WentOutToday))
                    StressRules.OnHazed(target);
            }
        }

        private void DailyTick()
        {
            foreach (var s in Alive.ToList()) InfectionRules.DailyTick(s, RegionExtraContaminationPerDay);
        }

        private void ResolveDeaths()
        {
            foreach (var dead in survivors.Where(s => s.IsDead && !deathsHandled.Contains(s)).ToList())
            {
                deathsHandled.Add(dead);
                if (dead.OutingDirection.HasValue)
                {
                    Outings.Release(dead.OutingDirection.Value);
                    dead.OutingDirection = null;
                }
                foreach (var other in Alive) StressRules.OnCompanionDeath(other);
                if (dead.Definition == CharacterCatalog.Jipnyang)
                {
                    IsWorldOver = true;
                    WorldOverReason = $"집냥이의 마지막 ({dead.DeathCause})";
                }
            }
        }
    }
}
