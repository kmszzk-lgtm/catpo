namespace Catpo.Core
{
    /// <summary>화면에 보여 줄 한국어 글. 이름 뒤에 붙는 조사(이/가, 은/는 등)를 받침에 맞춰 고른다.</summary>
    public static class Korean
    {
        public static bool HasFinalConsonant(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            char c = word[word.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return false;
            return (c - 0xAC00) % 28 != 0;
        }

        private static bool EndsWithRieul(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            char c = word[word.Length - 1];
            return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 == 8;
        }

        public static string IGa(string w) => w + (HasFinalConsonant(w) ? "이" : "가");
        public static string EunNeun(string w) => w + (HasFinalConsonant(w) ? "은" : "는");
        public static string EulReul(string w) => w + (HasFinalConsonant(w) ? "을" : "를");
        public static string WaGwa(string w) => w + (HasFinalConsonant(w) ? "과" : "와");
        public static string Ro(string w) => w + (HasFinalConsonant(w) && !EndsWithRieul(w) ? "으로" : "로");

        public static string SeasonName(Season s)
        {
            switch (s)
            {
                case Season.Spring: return "봄";
                case Season.Summer: return "여름";
                case Season.Autumn: return "가을";
                default: return "겨울";
            }
        }

        public static string DirectionName(Direction d)
        {
            switch (d)
            {
                case Direction.North: return "북쪽";
                case Direction.East: return "동쪽";
                case Direction.South: return "남쪽";
                default: return "서쪽";
            }
        }

        public static string HeatName(HeatSource h) => h == HeatSource.GasBurner ? "가스버너" : h == HeatSource.Campfire ? "모닥불" : "불 없음";

        public static string Duration(double hours)
        {
            int minutes = (int)System.Math.Ceiling(hours * 60 - 1e-9);
            if (minutes < 60) return $"{minutes}분";
            return minutes % 60 == 0 ? $"{minutes / 60}시간" : $"{minutes / 60}시간 {minutes % 60}분";
        }

        public static string Failure(ActionFailure f)
        {
            switch (f)
            {
                case ActionFailure.Dead: return "죽었다";
                case ActionFailure.NotAtHome: return "집에 없다";
                case ActionFailure.MissingSkill: return "할 줄 모른다 (실력이 모자람)";
                case ActionFailure.MissingInputs: return "재료가 모자란다";
                case ActionFailure.HeatNotChosen: return "불을 골라야 한다";
                case ActionFailure.NoHeat: return "그 불을 쓸 수 없다 (연료나 부탄가스가 없음)";
                case ActionFailure.TooStressed: return "스트레스가 너무 높아 아무것도 하기 싫어한다";
                case ActionFailure.NothingLeft: return "더 할 것이 없다";
                default: return "할 수 없다";
            }
        }

        public static string Depart(DepartResult r, Survivor s)
        {
            switch (r)
            {
                case DepartResult.Dead: return $"{IGa(s.Name)} 죽었다.";
                case DepartResult.CannotGoOutside: return $"{EunNeun(s.Name)} 밖에 나갈 수 없다.";
                case DepartResult.AlreadyOutside: return $"{EunNeun(s.Name)} 이미 밖에 있다.";
                case DepartResult.Resting: return $"{EunNeun(s.Name)} 오늘 쉬어야 한다 (어제 늦게 돌아옴).";
                case DepartResult.OutsideWindowClosed: return "지금은 외출할 수 있는 시간이 아니다.";
                case DepartResult.DirectionOccupied: return "그 방향에는 이미 누가 나가 있다.";
                case DepartResult.PlayerControlled: return "직접 맡은 캐릭터는 파견할 수 없다. 직접 외출은 다음 단계에서 만든다.";
                case DepartResult.TooContaminated: return $"{EunNeun(s.Name)} 오염도가 높아서 내보낼 수 없다.";
                default: return "나갈 수 없다.";
            }
        }
    }
}
