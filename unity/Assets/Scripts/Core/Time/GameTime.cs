using System;

namespace Catpo.Core
{
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }

    /// <summary>
    /// 게임 속 날짜와 시각. 1일째 00:00부터 흐른 분(minute)으로 저장한다.
    /// 계절은 봄부터 30일씩 바뀐다 (docs/07-time-and-seasons.md).
    /// </summary>
    public sealed class GameTime
    {
        public long TotalMinutes { get; private set; }

        public GameTime(int day, int hour, int minute = 0)
        {
            if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
            if (hour < 0 || hour > 23) throw new ArgumentOutOfRangeException(nameof(hour));
            if (minute < 0 || minute > 59) throw new ArgumentOutOfRangeException(nameof(minute));
            TotalMinutes = (long)(day - 1) * Balance.MinutesPerDay + hour * 60 + minute;
        }

        /// <summary>1부터 시작하는 날짜 (1일째, 2일째 ...).</summary>
        public int Day => (int)(TotalMinutes / Balance.MinutesPerDay) + 1;
        public int MinuteOfDay => (int)(TotalMinutes % Balance.MinutesPerDay);
        public int Hour => MinuteOfDay / 60;
        public int Minute => MinuteOfDay % 60;

        /// <summary>1년 = 120일. 1부터 시작.</summary>
        public int Year => (Day - 1) / (Balance.DaysPerSeason * Balance.SeasonsPerYear) + 1;
        public int DayOfYear => (Day - 1) % (Balance.DaysPerSeason * Balance.SeasonsPerYear) + 1;
        public Season Season => (Season)((DayOfYear - 1) / Balance.DaysPerSeason);

        /// <summary>시간을 앞으로 돌린다. 지난 자정의 수를 돌려준다.</summary>
        public int Advance(int minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            int before = Day;
            TotalMinutes += minutes;
            return Day - before;
        }

        /// <summary>계절별 외출 가능 시간 [시작 시각, 끝 시각). [확정]</summary>
        public static (int startHour, int endHour) OutingWindow(Season season)
        {
            switch (season)
            {
                case Season.Summer: return (8, 20);
                case Season.Winter: return (10, 16);
                default: return (9, 18); // 봄·가을은 기본값으로 이해 (Q33)
            }
        }

        public bool IsWithinOutingWindow
        {
            get
            {
                var (start, end) = OutingWindow(Season);
                return MinuteOfDay >= start * 60 && MinuteOfDay < end * 60;
            }
        }

        /// <summary>
        /// 시간대별 좀비 속도 배율. 12:00에 가장 느리고 00:00에 가장 빠르다 [확정].
        /// 정오와 자정을 양 끝으로 하는 코사인 곡선 [제안].
        /// </summary>
        public static double ZombieSpeedAt(int minuteOfDay)
        {
            double angle = 2 * Math.PI * minuteOfDay / Balance.MinutesPerDay; // 0시 = 0, 12시 = π
            double t = (1 + Math.Cos(angle)) / 2; // 자정 1, 정오 0
            return Balance.ZombieSpeedAtNoon + (Balance.ZombieSpeedAtMidnight - Balance.ZombieSpeedAtNoon) * t;
        }

        public double ZombieSpeed => ZombieSpeedAt(MinuteOfDay);

        public override string ToString() => $"{Day}일째 {Hour:00}:{Minute:00} ({Season})";
    }
}
