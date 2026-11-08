using System;
using System.Linq;
using Catpo.Game;
using CatpoStubControl;

/// <summary>
/// Unity 없이 Unity 쪽 코드를 돌려 본다.
/// 1. 에디터 메뉴 "좀비냥 > 규칙 코드 확인"
/// 2. 옥탑방 화면을 사람이 누르는 순서대로 눌러 보고, 나와야 할 글이 나오는지 확인
/// 실패가 하나라도 있으면 종료 코드 1.
/// </summary>
public static class Program
{
    private static int failures;
    private static HomeScreen screen;

    public static int Main()
    {
        Console.WriteLine("== 1. 에디터 메뉴: 좀비냥 > 규칙 코드 확인");
        Catpo.EditorTools.CoreCheckMenu.Run();
        Expect(Ui.Errors == 0, "에디터 메뉴가 오류 없이 끝까지 돈다");

        Console.WriteLine();
        Console.WriteLine("== 2. 옥탑방 화면 (▶ Play)");
        try
        {
            HomeScreenWalkthrough();
        }
        catch (Exception e)
        {
            failures++;
            Console.WriteLine("  실패: 화면을 그리다 예외가 났다\n" + e);
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "모두 통과" : $"실패 {failures}개");
        return failures == 0 ? 0 : 1;
    }

    private static void HomeScreenWalkthrough()
    {
        screen = new HomeScreen();
        Frame();
        Expect(Shows("누구로 할까요"), "처음에는 캐릭터 고르기 화면");

        Click("집냥이");
        Expect(Shows("★ 집냥이"), "집냥이를 고르면 옥탑방 화면이 나오고 내 캐릭터에 ★");
        Expect(Shows("집냥이로 시작한다"), "아래 글 창에 시작 글");
        Expect(Shows("생수  11"), "공용 창고에 시작 살림");

        Click("물 끓이기: 모닥불");
        Expect(Shows("재료가 모자란다"), "빗물이 없으면 물을 못 끓이고 이유가 나온다");

        Click("통조림");
        Expect(Shows("집냥이가 통조림을 먹었다."), "먹기 버튼");

        Click("군냥이");
        Click("옥상");
        Click("북쪽");
        Expect(Shows("지금은 외출할 수 있는 시간이 아니다."), "아침 7시에는 파견을 못 보낸다");

        Click("×60");
        RunSeconds(6); // 게임 2시간
        Expect(Shows("1일째 봄 09:00"), "×60이면 실제 6초에 게임 2시간");

        Click("북쪽");
        Expect(Shows("군냥이가 북쪽으로 파견을 나갔다"), "파견 출발");
        Expect(Shows("북쪽 (군냥이 나가 있음)"), "나가 있는 방향 표시");

        Click("모두 잠자기");
        Expect(Shows("밖에 나간 사람이 돌아와야 잘 수 있다."), "밖에 사람이 있으면 못 잔다");

        RunSeconds(20);
        Expect(Shows("군냥이가 북쪽에서 돌아왔다."), "파견에서 돌아온다");
        Expect(Ui.Drawn.Any(t => t.StartsWith("창고에 들어옴: ", StringComparison.Ordinal)), "가져온 물건이 글로 나온다");

        Click("의냥이");
        Click("3층");
        Click("소독약");
        Expect(Shows("의냥이: 정화제 (소독약)"), "3층에서 정화제 만들기");
        Click("몸 정화해 주기");
        Expect(Shows("누구 몸을 정화할까?"), "몸 정화 대상 고르기");
        Click("→ 군냥이");
        Expect(Shows("다른 일을 하는 중"), "정화제를 만드는 동안은 다른 일을 못 한다");
        RunSeconds(2);
        Click("몸 정화해 주기");
        Click("→ 군냥이");
        Expect(Shows("의냥이: 군냥이 몸 정화"), "일을 마치면 몸 정화");

        Click("길냥이");
        Click("옥탑방");
        Click("화투 치기");
        Expect(Shows("누구와 함께할까?"), "함께 놀 사람 고르기");
        Click("→ 집냥이");
        Expect(Shows("길냥이와 집냥이가 화투를 했다"), "둘이 화투");

        Click("1층");
        Expect(Shows("현관 100/100"), "1층에 현관 상태");

        Click("모두 잠자기");
        Expect(Shows("모두 잤다. 2일째 봄 07:00"), "모두 집에 있으면 아침까지 잔다");

        for (int i = 0; i < 60 && !Shows("월드가 끝났습니다"); i++) Click("모두 잠자기");
        Expect(Shows("월드가 끝났습니다"), "그냥 자기만 하면 결국 월드가 끝난다");
        Click("처음부터 다시");
        Expect(Shows("누구로 할까요"), "처음부터 다시");

        // 휴대폰 세로 화면처럼 비율이 달라도 그릴 수 있다
        Ui.ScreenWidth = 1080;
        Ui.ScreenHeight = 2400;
        Frame();
        Click("길냥이");
        Expect(Shows("★ 길냥이"), "세로 화면에서도 그려진다");
        Ui.ScreenWidth = 1920;
        Ui.ScreenHeight = 1080;
    }

    private static void Frame(float deltaTime = 0.02f)
    {
        Ui.BeginFrame();
        screen.OnGUI();
        Ui.EndFrame();
        screen.Update(deltaTime);
    }

    /// <summary>버튼을 누르고(그 프레임 끝에 처리됨), 한 번 더 그린다.</summary>
    private static void Click(string labelStart)
    {
        Ui.Clicks.Clear();
        Ui.Clicks.Enqueue(labelStart);
        Frame();
        if (Ui.Clicks.Count > 0)
        {
            failures++;
            Console.WriteLine($"  실패: '{labelStart}'(으)로 시작하는 버튼이 화면에 없다");
            Ui.Clicks.Clear();
        }
        Frame();
    }

    private static void RunSeconds(float seconds)
    {
        while (seconds > 1e-6f)
        {
            float dt = Math.Min(1f, seconds);
            Frame(dt);
            seconds -= dt;
        }
        Frame();
    }

    private static bool Shows(string fragment) => Ui.Drawn.Any(t => t.Contains(fragment));

    private static void Expect(bool ok, string what)
    {
        Console.WriteLine((ok ? "  통과: " : "  실패: ") + what);
        if (!ok) failures++;
    }
}
