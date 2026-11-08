using System;

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
        public int RainCollectors { get; internal set; }

        private bool gasBurnerInstalled;
        public int GasBurnerDurability { get; internal set; }
        public bool HasWorkingGasBurner => gasBurnerInstalled && GasBurnerDurability > 0;
        /// <summary>뜯어 쓰고 있는 부탄가스 1통에 남은 요리 횟수.</summary>
        public int ButaneCharges { get; internal set; }
        /// <summary>모닥불에 넣어 둔 연료. 1이면 요리 1번.</summary>
        public double CampfireFuel { get; internal set; }

        public double FrontDoor { get; internal set; } = Balance.FrontDoorMax;
        public int FurnitureMaterialsLeft { get; internal set; } = Balance.HomeFurnitureMaterials;
        public double HousePurity { get; internal set; } = Balance.HousePurityMax;
        public bool RainedToday { get; internal set; }

        /// <summary>창고의 가스버너를 설치한다. 쓰던 버너가 멀쩡하면 바꾸지 않는다.</summary>
        public bool InstallGasBurner(Inventory storage)
        {
            if (HasWorkingGasBurner) return false;
            if (!storage.Remove(ItemCatalog.GasBurner.Id)) return false;
            gasBurnerInstalled = true;
            GasBurnerDurability = Balance.GasBurnerDurability;
            return true;
        }

        /// <summary>
        /// 요리할 불을 쓴다. 가스버너와 부탄가스가 있으면 먼저 쓴다(효율이 좋음) [확정].
        /// 없으면 옥탑방 모닥불에 종이, 나무 순으로 연료를 넣는다 [확정: 탈 것을 계속 가져와야 함].
        /// </summary>
        internal HeatSource TryUseHeat(Inventory storage)
        {
            if (HasWorkingGasBurner)
            {
                if (ButaneCharges == 0 && storage.Remove(ItemCatalog.Butane.Id)) ButaneCharges = Balance.ButaneChargesPerCan;
                if (ButaneCharges > 0)
                {
                    ButaneCharges--;
                    GasBurnerDurability--;
                    if (GasBurnerDurability <= 0) gasBurnerInstalled = false; // 고장 [확정]
                    return HeatSource.GasBurner;
                }
            }

            if (CampfireFuel < 1)
            {
                if (storage.Remove(ItemCatalog.Paper.Id)) CampfireFuel += Balance.FuelUnitsPerPaper;
                else if (storage.Remove(ItemCatalog.Wood.Id)) CampfireFuel += Balance.FuelUnitsPerWood;
            }
            if (CampfireFuel >= 1)
            {
                CampfireFuel -= 1;
                return HeatSource.Campfire;
            }
            return HeatSource.None;
        }

        internal static double CookHours(HeatSource heat) =>
            heat == HeatSource.GasBurner ? Balance.GasBurnerCookHours : Balance.CampfireCookHours;

        internal void DamageFrontDoor(double amount) => FrontDoor = Math.Max(0, FrontDoor - amount);

        internal void LosePurity(double amount) => HousePurity = Math.Max(0, HousePurity - amount);
    }
}
