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
            var st = state.Storage;

            // 1층 현관 앞 경비실의 도어락 마스터 키로 지하부터 옥상까지 다 열어서 모은 살림 [확정].
            st.Add(ItemCatalog.BottledWater.Id, 6);      // 생수는 집에 많이 두지 않음 [확정]
            st.Add(ItemCatalog.CannedFood.Id, 10);       // 보존식은 1인 기준 30일치 [확정] (개수 배분은 [제안])
            st.Add(ItemCatalog.InstantNoodle.Id, 12);
            st.Add(ItemCatalog.Chocolate.Id, 8);
            foreach (var seasoning in new[] { ItemCatalog.Salt, ItemCatalog.SoySauce, ItemCatalog.SesameOil, ItemCatalog.CookingOil, ItemCatalog.CanolaOil })
                st.Add(seasoning.Id, 4);                 // 조미료는 집집마다 한 통씩 [확정]
            st.Add(ItemCatalog.Butane.Id, 5);            // [확정]
            st.Add(ItemCatalog.VinylBag.Id, 20);         // 어지간한 집에 다 있음 [확정], 개수 [제안]
            st.Add(ItemCatalog.PetBottle.Id, 4);         // [제안]
            st.Add(ItemCatalog.Pot.Id, 3);               // [제안]
            st.Add(ItemCatalog.Paper.Id, 20);            // [제안]
            st.Add(ItemCatalog.WoodStick.Id, 4);         // [제안]
            st.Add(ItemCatalog.WoodBoard.Id, 2);         // [제안]
            st.Add(ItemCatalog.Tape.Id, 3);              // [제안]
            st.Add(ItemCatalog.Bleach.Id, 4);            // [제안] 집집마다 락스
            st.Add(ItemCatalog.LiquorBottle.Id, 3);      // [제안]
            st.Add(ItemCatalog.Disinfectant.Id, 2);      // [제안]
            // 의냥이 시작 아이템 [확정]: 응급처치 도구 여러 개, 상비약, 진통제. 개수는 [제안].
            st.Add(ItemCatalog.FirstAidKit.Id, 3);
            st.Add(ItemCatalog.HouseholdMedicine.Id, 5);
            st.Add(ItemCatalog.Painkiller.Id, 5);
            // 가스버너는 집에 있다 [확정]
            st.Add(ItemCatalog.GasBurner.Id, 1);
            state.Home.InstallGasBurner(st);
            // 튜토리얼에서 의냥이가 시설 전체를 소독했다 [확정]
            state.Home.HouseProtectedUntilMinute = state.Time.TotalMinutes + Balance.HouseProtectionHours * 60;
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
        /// 오염도가 기준을 넘었으면 다음 날 외출 불가. AI 파견 중이면 그때까지 주운 것만 가지고 돌아온다.
        /// </summary>
        public void Return(Survivor s)
        {
            if (s.Location != SurvivorLocation.Outside) return;
            if (s.IsDispatched)
            {
                CompleteDispatch(s, autoRepeat: false);
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
        /// AI가 맡은 캐릭터를 한 방향으로 보낸다 [확정]. 직접 수색하지 않고 결과만 가지고 돌아온다 [확정].
        /// 가까운 곳부터 흔한 물건 순으로 줍고, 가방이 차거나 시간이 다 되면 돌아온다.
        /// 물자가 많은 곳은 빨리 차서 빨리 오고, 적은 곳은 늦게 온다 [확정]. AI는 과적하지 않는다 [확정].
        /// </summary>
        public DepartResult Dispatch(Survivor s, Direction direction)
        {
            if (s.ControlledByPlayer) return DepartResult.PlayerControlled;
            if (CanDepart(s, direction) == DepartResult.Ok && ExposureBudgetMinutes(s) < Balance.MinDispatchExposureMinutes)
                return DepartResult.TooContaminated;
            var result = TryDepart(s, direction);
            if (result != DepartResult.Ok) return result;

            var plan = PlanDispatch(s, direction, out double returnAfterMinutes);
            s.IsDispatched = true;
            s.DispatchStartMinute = Time.TotalMinutes;
            s.DispatchPlan = plan;
            s.DispatchReturnAtMinute = Time.TotalMinutes + (long)Math.Ceiling(returnAfterMinutes - 1e-9);
            return DepartResult.Ok;
        }

        /// <summary>오늘 AI가 돌아와야 하는 마지막 시각까지 남은 분. 늦어도 해 지기 1시간 전 [제안].</summary>
        private double DispatchBudgetMinutes()
        {
            var (_, endHour) = GameTime.OutingWindow(Time.Season);
            long dayStart = Time.TotalMinutes - Time.MinuteOfDay;
            long deadline = dayStart + (endHour - Balance.DispatchReturnHoursBeforeEnd) * 60;
            return deadline - Time.TotalMinutes;
        }

        /// <summary>돌아왔을 때 오염도가 상한을 넘지 않도록 바깥에 있을 수 있는 시간 [확정: 50 이상이 되지 않게].</summary>
        private static double ExposureBudgetMinutes(Survivor s) =>
            (Balance.AiMaxContaminationOnReturn - s.Contamination) / Balance.ContaminationPerOutsideHour * 60;

        private List<DispatchPick> PlanDispatch(Survivor s, Direction d, out double returnAfterMinutes)
        {
            double budget = Math.Min(Math.Max(Balance.MinDispatchHours * 60, DispatchBudgetMinutes()), ExposureBudgetMinutes(s));
            var e = s.Effects;
            double searchSpeed = e.ActionSpeed * (1 + (s.Skills.Level(Skill.Search) - 1) * Balance.CraftSpeedPerLevel)
                * StressRules.ActionSpeedMultiplier(s.Stress);
            double step = Balance.DispatchTravelMinutesPerStep / e.MoveSpeed;
            double perItem = Balance.DispatchSearchMinutesPerItem / Math.Max(0.01, searchSpeed);

            var picks = new List<DispatchPick>();
            double weight = s.Inventory.TotalWeightKg;
            double t = 0;
            int position = -1; // -1 = 집
            bool stop = false;
            foreach (var spot in Field.Remaining(d).ToList())
            {
                double travel = step * (spot.Order - position);
                double back = step * (spot.Order + 1);
                if (t + travel + back > budget) break;
                t += travel;
                position = spot.Order;

                // 그 오브젝트에서 흔한 물건부터
                foreach (var kv in spot.Contents.Items.OrderByDescending(x => x.Value).ThenBy(x => x.Key).ToList())
                {
                    double w = ItemCatalog.Get(kv.Key).WeightKg;
                    for (int i = 0; i < kv.Value; i++)
                    {
                        if (weight + w > s.CarryLimitKg + 1e-9) break; // 이 물건은 더 못 듦
                        if (t + perItem + back > budget) { stop = true; break; }
                        t += perItem;
                        weight += w;
                        picks.Add(new DispatchPick { Spot = spot, ItemId = kv.Key, AtMinute = t });
                    }
                    if (stop || s.CarryLimitKg - weight < Balance.DispatchFullMarginKg) { stop = true; break; }
                }
                if (stop) break;
            }

            // 아무것도 못 찾으면 하루 종일 찾다가 마감 때 돌아온다
            returnAfterMinutes = picks.Count == 0 ? budget : t + step * (position + 1);
            return picks;
        }

        private void CompleteDispatch(Survivor s, bool autoRepeat)
        {
            double elapsed = Time.TotalMinutes - s.DispatchStartMinute;
            foreach (var pick in s.DispatchPlan)
            {
                if (pick.AtMinute > elapsed + 1e-9) break;
                if (pick.Spot.Contents.Count(pick.ItemId) == 0) continue;
                if (!s.TryPickUp(pick.ItemId)) continue;
                pick.Spot.Contents.Remove(pick.ItemId);
            }
            s.DispatchPlan = new List<DispatchPick>();
            s.IsDispatched = false;
            ArriveHome(s);
            if (autoRepeat) TryAutoDispatch(s);
        }

        /// <summary>
        /// 자동 파견 [확정: 수동으로 자주 보내는 건 귀찮으니 자동으로]. 정해 둔 방향으로, 외출 가능 시간 안이면 알아서 나간다.
        /// 오염도가 높으면 쉬게 하고, 남은 물자가 없거나 시간이 1시간도 안 남았으면 나가지 않는다 [제안].
        /// </summary>
        private bool TryAutoDispatch(Survivor s)
        {
            if (!s.AutoDispatchDirection.HasValue || s.ControlledByPlayer || s.IsDead) return false;
            if (s.Location != SurvivorLocation.Home) return false;
            if (s.Contamination >= Balance.AutoDispatchContaminationLimit) return false;
            if (!Field.Remaining(s.AutoDispatchDirection.Value).Any()) return false;
            if (DispatchBudgetMinutes() < Balance.MinDispatchHours * 60) return false;
            return Dispatch(s, s.AutoDispatchDirection.Value) == DepartResult.Ok;
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

        /// <summary>
        /// 시간을 흘린다. 자정마다 하루치 일을 처리하고, AI 파견은 정해진 시각에 돌아오며,
        /// 자동 파견은 외출 가능 시간이 되면 나간다.
        /// </summary>
        public void Advance(int minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            while (true)
            {
                ProcessDueEvents();
                if (minutes <= 0 || IsWorldOver) break;

                long now = Time.TotalMinutes;
                long limit = now + Math.Min(minutes, Balance.MinutesPerDay - Time.MinuteOfDay);
                void Consider(long at)
                {
                    if (at > now && at < limit) limit = at;
                }
                foreach (var s in Alive.Where(x => x.IsDispatched)) Consider(s.DispatchReturnAtMinute);
                var (startHour, _) = GameTime.OutingWindow(Time.Season);
                Consider(now - Time.MinuteOfDay + startHour * 60);
                Consider(Home.HouseProtectedUntilMinute);

                int chunk = (int)(limit - now);
                ApplyElapsed(chunk / 60.0);
                int midnights = Time.Advance(chunk);
                minutes -= chunk;
                if (midnights > 0) DailyTick();
                ResolveDeaths();
            }
        }

        private void ProcessDueEvents()
        {
            foreach (var s in Alive.Where(x => x.IsDispatched && x.DispatchReturnAtMinute <= Time.TotalMinutes).ToList())
                CompleteDispatch(s, autoRepeat: true);
            if (Time.IsWithinOutingWindow)
                foreach (var s in Alive.Where(x => x.AutoDispatchDirection.HasValue && x.Location == SurvivorLocation.Home).ToList())
                    TryAutoDispatch(s);
            ResolveDeaths();
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
            bool homeUnprotected = !Home.IsHouseProtected(Time.TotalMinutes);
            foreach (var s in Alive)
            {
                // 정화가 끊긴 집은 바깥에 있는 것처럼 오염도가 오른다 [확정]
                if (s.Location == SurvivorLocation.Outside || homeUnprotected) InfectionRules.ExposeOutside(s, hours);
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

            Home.DamageFrontDoor(Balance.FrontDoorDailyDamage);
            if (Home.FrontDoor <= 0) EndWorld("무너진 현관");

            Home.RainedToday = random.NextDouble() < Balance.RainChanceBySeason[(int)Time.Season];
            if (Home.RainedToday) CollectRain();
        }

        /// <summary>비 오는 날 빗물받이에 빗물이 모인다 [제안 수치].</summary>
        internal void CollectRain()
        {
            if (Home.RainCollectorCapacity > 0) Storage.Add(ItemCatalog.RainWater.Id, Home.RainCollectorCapacity);
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
                dead.DispatchPlan = new List<DispatchPick>();
                foreach (var other in Alive) StressRules.OnCompanionDeath(other);
                if (dead.Definition == CharacterCatalog.Jipnyang) EndWorld($"집냥이의 마지막 ({dead.DeathCause})");
            }
        }
    }
}
