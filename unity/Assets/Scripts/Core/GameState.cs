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
        public Household Home { get; } = new Household();
        public FieldMap Field { get; }

        /// <summary>지역 보스가 있으면 하루 +5 [확정]. 첫 버전에서는 0.</summary>
        public double RegionExtraContaminationPerDay { get; set; }

        public bool IsWorldOver { get; private set; }
        public string WorldOverReason { get; private set; }

        private readonly List<Survivor> survivors;
        private readonly HashSet<Survivor> deathsHandled = new HashSet<Survivor>();
        private readonly Random random;
        private double hazingHours;

        public GameState(GameTime time, IEnumerable<Survivor> survivors, FieldMap field = null, int seed = 20261108)
        {
            Time = time;
            this.survivors = survivors.ToList();
            Field = field ?? new FieldMap();
            random = new Random(seed);
        }

        /// <summary>튜토리얼 모드 새 월드: 1일째(봄) 아침, 기본 캐릭터 4명.</summary>
        public static GameState NewTutorial(int seed = 20261108)
        {
            var state = new GameState(new GameTime(1, 7), CharacterCatalog.Starting.Select(d => new Survivor(d)),
                FieldMap.CreateTutorial(), seed);

            // 의냥이 시작 아이템 [확정]: 응급처치 도구 여러 개, 상비약, 진통제. 개수는 [제안].
            state.Storage.Add(ItemCatalog.FirstAidKit.Id, 3);
            state.Storage.Add(ItemCatalog.HouseholdMedicine.Id, 5);
            state.Storage.Add(ItemCatalog.Painkiller.Id, 5);

            // 그동안 버텨 온 집의 살림 [제안]. 가스버너는 집에 있다 [확정], 부탄가스는 많지 않다 [확정].
            state.Storage.Add(ItemCatalog.BottledWater.Id, 2);
            state.Storage.Add(ItemCatalog.CannedFood.Id, 2);
            state.Storage.Add(ItemCatalog.Paper.Id, 4);
            state.Storage.Add(ItemCatalog.Wood.Id, 1);
            state.Storage.Add(ItemCatalog.Butane.Id, 1);
            state.Storage.Add(ItemCatalog.Tape.Id, 2);
            // 튜토리얼에서 빗물받이 하나를 만들 재료
            state.Storage.Add(ItemCatalog.PlasticContainer.Id, 1);
            state.Storage.Add(ItemCatalog.Vinyl.Id, 1);
            state.Storage.Add(ItemCatalog.GasBurner.Id, 1);
            state.Home.InstallGasBurner(state.Storage);
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
        /// 오염도가 기준을 넘었으면 다음 날 외출 불가. AI 파견 중이면 그때까지의 결과를 가지고 돌아온다.
        /// </summary>
        public void Return(Survivor s)
        {
            if (s.Location != SurvivorLocation.Outside) return;
            if (s.IsDispatched)
            {
                CompleteDispatch(s);
                return;
            }
            ArriveHome(s);
        }

        /// <summary>나갈 때 공용 창고에서 꺼내 간다 [확정]. 무게 한도를 넘으면 실패.</summary>
        public bool TakeFromStorage(Survivor s, string itemId, int amount = 1)
        {
            if (Storage.Count(itemId) < amount) return false;
            if (!s.TryPickUp(itemId, amount)) return false;
            Storage.Remove(itemId, amount);
            return true;
        }

        /// <summary>먹고 마신다. 집에서는 공용 창고, 밖에서는 자기 인벤토리에서 꺼낸다.</summary>
        public ConsumeResult Consume(Survivor s, string itemId)
        {
            var source = s.Location == SurvivorLocation.Home ? Storage : s.Inventory;
            return NeedsRules.Consume(s, source, itemId);
        }

        // ── AI 파견 ────────────────────────────────────────────────────

        /// <summary>
        /// AI가 맡은 캐릭터를 한 방향으로 보낸다 [확정]. 직접 수색하지 않고, 저녁 전에 물자를 조금 가지고 돌아온다 [확정].
        /// 이동이 빠른 캐릭터(길냥이)는 더 일찍 돌아온다 [확정: 특성에 따라 귀환 시각이 다름].
        /// </summary>
        public DepartResult Dispatch(Survivor s, Direction direction)
        {
            var result = TryDepart(s, direction);
            if (result != DepartResult.Ok) return result;

            var (_, endHour) = GameTime.OutingWindow(Time.Season);
            int returnHour = endHour - Balance.DispatchReturnHoursBeforeEnd
                - (s.Effects.MoveSpeed > 1 ? Balance.FastDispatchHoursEarlier : 0);
            long dayStart = Time.TotalMinutes - Time.MinuteOfDay;
            long earliest = Time.TotalMinutes + Balance.MinDispatchHours * 60;

            s.IsDispatched = true;
            s.DispatchStartMinute = Time.TotalMinutes;
            s.DispatchReturnAtMinute = Math.Max(dayStart + returnHour * 60, earliest);
            return DepartResult.Ok;
        }

        /// <summary>
        /// AI가 상호작용한 오브젝트는 필드에서 제거되고, AI는 그때 나온 잔해물을 가져온다 [확정].
        /// 바깥 3시간마다 가까운 오브젝트 1곳, 각 물자의 40% 정도를 무게 한도 안에서 가져온다 [제안].
        /// </summary>
        private void CompleteDispatch(Survivor s)
        {
            double hoursOut = (Time.TotalMinutes - s.DispatchStartMinute) / 60.0;
            int spotCount = Math.Max(1, (int)Math.Floor(hoursOut / Balance.DispatchHoursPerSpot));
            if (s.OutingDirection.HasValue)
            {
                foreach (var spot in Field.Remaining(s.OutingDirection.Value).Take(spotCount).ToList())
                {
                    foreach (var kv in spot.Contents.Items.ToList())
                    {
                        int share = (int)Math.Round(kv.Value * Balance.DispatchLootShare, MidpointRounding.AwayFromZero);
                        for (int i = 0; i < share; i++)
                            if (!s.TryPickUp(kv.Key)) break;
                    }
                    spot.IsRemoved = true;
                }
            }
            s.IsDispatched = false;
            ArriveHome(s);
        }

        private void ArriveHome(Survivor s)
        {
            if (s.OutingDirection.HasValue) Outings.Release(s.OutingDirection.Value);
            s.Location = SurvivorLocation.Home;
            s.OutingDirection = null;
            s.Inventory.MoveAllTo(Storage);
            if (InfectionRules.NeedsRestAfterReturn(s)) s.RestDayPending = true;
        }

        // ── 시간 ───────────────────────────────────────────────────────

        /// <summary>시간을 흘린다. 자정마다 하루치 일을 처리하고, AI 파견은 정해진 시각에 돌아온다.</summary>
        public void Advance(int minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            while (minutes > 0 && !IsWorldOver)
            {
                int chunk = Math.Min(minutes, Balance.MinutesPerDay - Time.MinuteOfDay);
                long? nextReturn = Alive.Where(s => s.IsDispatched).Select(s => (long?)s.DispatchReturnAtMinute).Min();
                if (nextReturn.HasValue)
                {
                    long until = nextReturn.Value - Time.TotalMinutes;
                    if (until >= 0 && until < chunk) chunk = (int)until;
                }

                if (chunk > 0)
                {
                    ApplyElapsed(chunk / 60.0);
                    int midnights = Time.Advance(chunk);
                    minutes -= chunk;
                    if (midnights > 0) DailyTick();
                }

                foreach (var s in Alive.Where(x => x.IsDispatched && x.DispatchReturnAtMinute <= Time.TotalMinutes).ToList())
                    CompleteDispatch(s);
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
            bool homeUnpurified = Home.HousePurity <= 0;
            foreach (var s in Alive)
            {
                if (s.Location == SurvivorLocation.Outside) InfectionRules.ExposeOutside(s, hours);
                else if (homeUnpurified) InfectionRules.AddContamination(s, Balance.UnpurifiedHomeContaminationPerHour * hours);
                NeedsRules.OnTimePassed(s, hours);
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

            Home.LosePurity(Balance.HousePurityDailyLoss);

            Home.DamageFrontDoor(Balance.FrontDoorDailyDamage);
            if (Home.FrontDoor <= 0) EndWorld("무너진 현관");

            Home.RainedToday = random.NextDouble() < Balance.RainChanceBySeason[(int)Time.Season];
            if (Home.RainedToday) CollectRain();
        }

        /// <summary>비 오는 날 빗물받이마다 빗물이 모인다 [제안].</summary>
        internal void CollectRain()
        {
            int amount = Home.RainCollectors * Balance.RainWaterPerCollector;
            if (amount > 0) Storage.Add(ItemCatalog.RainWater.Id, amount);
        }

        private void EndWorld(string reason)
        {
            if (IsWorldOver) return;
            IsWorldOver = true;
            WorldOverReason = reason;
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
                dead.IsDispatched = false;
                foreach (var other in Alive) StressRules.OnCompanionDeath(other);
                if (dead.Definition == CharacterCatalog.Jipnyang) EndWorld($"집냥이의 마지막 ({dead.DeathCause})");
            }
        }
    }
}
