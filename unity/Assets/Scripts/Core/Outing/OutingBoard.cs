using System.Collections.Generic;

namespace Catpo.Core
{
    /// <summary>집 앞 사거리에서 나가는 4방향 [확정].</summary>
    public enum Direction
    {
        North,
        East,
        South,
        West,
    }

    public enum DepartResult
    {
        Ok,
        Dead,
        CannotGoOutside,
        AlreadyOutside,
        Resting,
        OutsideWindowClosed,
        DirectionOccupied,
        /// <summary>사람이 맡은 캐릭터는 AI 파견을 보낼 수 없다.</summary>
        PlayerControlled,
        /// <summary>오염도가 높아서 AI를 내보내면 돌아올 때 50을 넘을 수 있다.</summary>
        TooContaminated,
    }

    /// <summary>AI 파견 계획의 한 줄: 출발 뒤 몇 분째에 어느 오브젝트에서 무엇을 줍는지.</summary>
    internal sealed class DispatchPick
    {
        public LootSpot Spot;
        public string ItemId;
        public double AtMinute;
    }

    /// <summary>
    /// 어느 방향에 누가 나가 있는지 기록한다.
    /// 한 방향에 누가 나가 있는 동안 다른 캐릭터(사람이든 AI든)는 그 방향으로 갈 수 없고, 돌아오면 바로 풀린다 [확정].
    /// </summary>
    public sealed class OutingBoard
    {
        private readonly Dictionary<Direction, Survivor> occupants = new Dictionary<Direction, Survivor>();

        public bool IsOccupied(Direction d) => occupants.ContainsKey(d);

        public Survivor OccupantOf(Direction d) => occupants.TryGetValue(d, out var s) ? s : null;

        internal void Occupy(Direction d, Survivor s) => occupants[d] = s;

        internal void Release(Direction d) => occupants.Remove(d);
    }
}
