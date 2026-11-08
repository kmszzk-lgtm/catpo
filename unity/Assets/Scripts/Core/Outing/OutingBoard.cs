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
