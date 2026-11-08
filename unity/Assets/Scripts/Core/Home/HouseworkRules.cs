using System;

namespace Catpo.Core
{
    /// <summary>
    /// 집안일과 치료 (docs/09-home-life.md). 모두 집에 있는 캐릭터가 공용 창고의 물건으로 한다.
    /// 걸리는 시간과 수치는 [제안].
    /// </summary>
    public static class HouseworkRules
    {
        /// <summary>
        /// 빗물받이 [확정]: 비닐봉투 + 나무 막대 + 담을 수 있는 용기(1.5L 페트병, 냄비)로 만든다.
        /// 냄비는 그냥 비 오는 곳에 두기만 해도 된다 [확정]. 모이는 양은 [제안].
        /// </summary>
        public static ActionResult BuildRainCollector(GameState state, Survivor s, string containerItemId)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            bool isContainer = containerItemId == ItemCatalog.PetBottle.Id || containerItemId == ItemCatalog.Pot.Id;
            if (!isContainer || state.Storage.Count(containerItemId) == 0) return ActionResult.Fail(ActionFailure.MissingInputs);

            bool hasFunnel = state.Storage.Count(ItemCatalog.VinylBag.Id) > 0 && state.Storage.Count(ItemCatalog.WoodStick.Id) > 0;
            if (hasFunnel)
            {
                state.Storage.Remove(ItemCatalog.VinylBag.Id);
                state.Storage.Remove(ItemCatalog.WoodStick.Id);
                state.Storage.Remove(containerItemId);
                state.Home.RainCollectorCapacity += Balance.RainWaterPerFunnelCollector;
            }
            else if (containerItemId == ItemCatalog.Pot.Id)
            {
                state.Storage.Remove(containerItemId);
                state.Home.RainCollectorCapacity += Balance.RainWaterPerPot;
            }
            else
            {
                return ActionResult.Fail(ActionFailure.MissingInputs);
            }
            return Done(s, CraftKind.Housework, Skill.Crafting, 10, Balance.BuildRainCollectorHours);
        }

        /// <summary>
        /// 집 안 가구(침대, 매트리스, 테이블 등)를 분해해 나무 판과 나무 막대로 만든다 [확정].
        /// 가구째로는 도르래로 올리고 내리기 힘들어서, 밖에서도 분해해서 가져온다 [확정].
        /// 집 안 가구가 바닥나면 밖에서도 가져와야 한다는 메시지를 띄운다 [확정].
        /// </summary>
        public static ActionResult BreakFurniture(GameState state, Survivor s)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            if (state.Home.FurnitureLeft <= 0)
                return ActionResult.Fail(ActionFailure.NothingLeft, needMaterialsFromOutside: true);

            state.Home.FurnitureLeft--;
            state.Storage.Add(ItemCatalog.WoodBoard.Id, Balance.BoardsPerFurniture);
            state.Storage.Add(ItemCatalog.WoodStick.Id, Balance.SticksPerFurniture);
            bool ranOut = state.Home.FurnitureLeft == 0;
            var result = Done(s, CraftKind.Housework, Skill.Crafting, 5, Balance.BreakFurnitureHours);
            return new ActionResult(ActionFailure.None, result.Hours, needMaterialsFromOutside: ranOut);
        }

        /// <summary>현관 보수: 나무 판, 나무 막대, 테이프로 막는다 [확정]. 현관이 뚫리면 게임 오버 [확정].</summary>
        public static ActionResult RepairFrontDoor(GameState state, Survivor s, string itemId)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);

            double amount;
            if (itemId == ItemCatalog.WoodBoard.Id) amount = Balance.DoorRepairPerBoard;
            else if (itemId == ItemCatalog.WoodStick.Id) amount = Balance.DoorRepairPerStick;
            else if (itemId == ItemCatalog.Tape.Id) amount = Balance.DoorRepairPerTape;
            else return ActionResult.Fail(ActionFailure.MissingInputs);
            if (!state.Storage.Remove(itemId)) return ActionResult.Fail(ActionFailure.MissingInputs);

            state.Home.FrontDoor = Math.Min(Balance.FrontDoorMax, state.Home.FrontDoor + amount);
            return Done(s, CraftKind.Housework, Skill.Crafting, 5, Balance.DoorRepairHours);
        }

        /// <summary>
        /// 집 정화: 정화제 1개로 24시간 동안 집 안 오염도가 오르지 않는다 [확정: 매일 써야 함].
        /// 정화가 끊기면 집 안에서도 바깥처럼 오염도가 오른다 [확정].
        /// </summary>
        public static ActionResult PurifyHouse(GameState state, Survivor s)
        {
            var failure = CraftingRules.CheckCanActAtHome(s);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            if (!state.Storage.Remove(ItemCatalog.Purifier.Id)) return ActionResult.Fail(ActionFailure.MissingInputs);

            state.Home.HouseProtectedUntilMinute = state.Time.TotalMinutes + Balance.HouseProtectionHours * 60;
            return Done(s, CraftKind.Medicine, Skill.Medical, 5, 1);
        }

        /// <summary>
        /// 몸 정화: 외출하고 돌아온 캐릭터의 전신을 소독하고 항생제를 먹여 오염도를 낮춘다 [확정].
        /// 정화제는 소모품이라, 누가 해 주든 효과는 같다 [확정]. 감염 상태는 그대로다 [확정].
        /// </summary>
        public static ActionResult PurifyBody(GameState state, Survivor helper, Survivor target)
        {
            var failure = CraftingRules.CheckCanActAtHome(helper);
            if (failure != ActionFailure.None) return ActionResult.Fail(failure);
            if (target.IsDead) return ActionResult.Fail(ActionFailure.Dead);
            if (target.Location != SurvivorLocation.Home) return ActionResult.Fail(ActionFailure.NotAtHome);
            if (!state.Storage.Remove(ItemCatalog.Purifier.Id)) return ActionResult.Fail(ActionFailure.MissingInputs);

            InfectionRules.ApplyPurifier(target);
            return Done(helper, CraftKind.Medicine, Skill.Medical, 5, Balance.BodyPurifyHours);
        }

        private static ActionResult Done(Survivor s, CraftKind kind, Skill skill, double xp, double baseHours)
        {
            s.GainXp(skill, xp, atHome: true);
            StressRules.OnCraft(s, s.Definition.TasteFor(kind));
            return new ActionResult(ActionFailure.None, CraftingRules.HomeActionHours(s, baseHours));
        }
    }
}
