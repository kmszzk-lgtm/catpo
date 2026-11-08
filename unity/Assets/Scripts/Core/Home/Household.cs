using System;
using System.Collections.Generic;

namespace Catpo.Core
{
    public enum HeatSource
    {
        None,
        Campfire,
        GasBurner,
    }

    /// <summary>
    /// 집(옥탑방 주택)의 설비 상태 (docs/09-home-life.md).
    /// </summary>
    public sealed class Household
    {
        /// <summary>비 오는 날 모이는 빗물의 양 (빗물받이들의 합).</summary>
        public int RainCollectorCapacity { get; internal set; }

        private bool gasBurnerInstalled;
        /// <summary>가스버너가 고장 나기 전까지 더 쓸 수 있는 부탄가스 통 수. 100통 정도 쓰면 고장 [확정].</summary>
        public int GasBurnerCansLeft { get; internal set; }
        /// <summary>뜯어 쓰고 있는 부탄가스 1통에 남은 요리 횟수.</summary>
        public int ButaneCharges { get; internal set; }
        public bool HasWorkingGasBurner => gasBurnerInstalled && (GasBurnerCansLeft > 0 || ButaneCharges > 0);

        /// <summary>모닥불에 넣어 둔 연료. 1이면 요리 1번.</summary>
        public double CampfireFuel { get; internal set; }

        public double FrontDoor { get; internal set; } = Balance.FrontDoorMax;
        /// <summary>집 안에 남은, 분해할 수 있는 가구 수.</summary>
        public int FurnitureLeft { get; internal set; } = Balance.HomeFurnitureLeftAtStart;

        /// <summary>집 정화가 유지되는 시각(게임 시간, 분). 매일 정화제를 써야 한다 [확정].</summary>
        public long HouseProtectedUntilMinute { get; internal set; }
        public bool IsHouseProtected(long nowMinute) => nowMinute < HouseProtectedUntilMinute;

        public bool RainedToday { get; internal set; }

        /// <summary>창고의 가스버너를 설치한다. 쓰던 버너가 멀쩡하면 바꾸지 않는다.</summary>
        public bool InstallGasBurner(Inventory storage)
        {
            if (HasWorkingGasBurner) return false;
            if (!storage.Remove(ItemCatalog.GasBurner.Id)) return false;
            gasBurnerInstalled = true;
            GasBurnerCansLeft = Balance.GasBurnerCanLife;
            ButaneCharges = 0;
            return true;
        }

        /// <summary>지금 쓸 수 있는 불. 화면에서 플레이어가 고르게 보여준다 [확정: 불은 고를 수 있어야 함].</summary>
        public IReadOnlyList<HeatSource> AvailableHeat(Inventory storage)
        {
            var list = new List<HeatSource>();
            if (CanUse(storage, HeatSource.Campfire)) list.Add(HeatSource.Campfire);
            if (CanUse(storage, HeatSource.GasBurner)) list.Add(HeatSource.GasBurner);
            return list;
        }

        public bool CanUse(Inventory storage, HeatSource heat)
        {
            switch (heat)
            {
                case HeatSource.GasBurner:
                    return HasWorkingGasBurner &&
                           (ButaneCharges > 0 || (GasBurnerCansLeft > 0 && storage.Count(ItemCatalog.Butane.Id) > 0));
                case HeatSource.Campfire:
                    return CampfireFuel >= 1 || storage.Count(ItemCatalog.Paper.Id) > 0
                           || storage.Count(ItemCatalog.WoodStick.Id) > 0 || storage.Count(ItemCatalog.WoodBoard.Id) > 0;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 고른 불을 요리 1번만큼 쓴다.
        /// 가스버너: 부탄가스를 뜯을 때마다 버너 수명이 1통씩 준다.
        /// 모닥불: 옥탑방에서 종이, 나무 막대, 나무 판 순으로 태운다 (탈 것을 계속 가져와야 함 [확정]).
        /// </summary>
        internal bool UseHeat(Inventory storage, HeatSource heat)
        {
            if (!CanUse(storage, heat)) return false;
            if (heat == HeatSource.GasBurner)
            {
                if (ButaneCharges == 0)
                {
                    storage.Remove(ItemCatalog.Butane.Id);
                    ButaneCharges = Balance.ButaneChargesPerCan;
                    GasBurnerCansLeft--;
                }
                ButaneCharges--;
                if (GasBurnerCansLeft <= 0 && ButaneCharges == 0) gasBurnerInstalled = false; // 고장 [확정]
                return true;
            }

            if (CampfireFuel < 1)
            {
                if (storage.Remove(ItemCatalog.Paper.Id)) CampfireFuel += Balance.FuelUnitsPerPaper;
                else if (storage.Remove(ItemCatalog.WoodStick.Id)) CampfireFuel += Balance.FuelUnitsPerWoodStick;
                else if (storage.Remove(ItemCatalog.WoodBoard.Id)) CampfireFuel += Balance.FuelUnitsPerWoodBoard;
            }
            CampfireFuel -= 1;
            return true;
        }

        internal static double CookHours(HeatSource heat) =>
            heat == HeatSource.GasBurner ? Balance.GasBurnerCookHours : Balance.CampfireCookHours;

        internal void DamageFrontDoor(double amount) => FrontDoor = Math.Max(0, FrontDoor - amount);
    }
}
