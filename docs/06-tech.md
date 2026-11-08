# 기술 구조

## 엔진 (확정)

- **Unity** (2026-11-07 결정). 버전은 **Unity 6.6 (6000.6.8f1)** (확정, 2026-11-08).
- 설치할 때 Android Build Support 모듈도 함께 설치.

## 작업 방식 (제안)

클라우드 작업 환경(Claude Code)에서는 Unity 에디터를 실행할 수 없다. 그래서 코드를 두 층으로 나눈다.

| 층 | 내용 | 어디서 확인 |
|---|---|---|
| 규칙 코드 (Core) | 자원 계산, 수색 결과, 귀환·감염 판정, 이벤트, 낮밤 흐름, 저장 데이터. `UnityEngine`을 참조하지 않는 순수 C# | 클라우드에서 .NET + NUnit으로 자동 테스트 |
| Unity 코드 | 장면, UI, 입력, 그래픽, 사운드, 네트워크. Core를 호출해서 화면에 보여줌 | 기획자 PC의 Unity 에디터, 휴대폰 빌드 |

이렇게 나누면 저장과 멀티 동기화도 Core의 상태 데이터만 다루면 된다.

## 폴더 구조 (확정, 2026-11-08 만듦)

```
catpo/
├─ docs/                              기획 문서
├─ unity/                             Unity 프로젝트 폴더 (지금은 Assets만 있음)
│  └─ Assets/Scripts/Core/            규칙 코드. Catpo.Core.asmdef (엔진 참조 없음)
│     ├─ Balance.cs                   밸런스 수치. [확정]/[제안] 표시
│     ├─ GameState.cs                 월드 전체 상태: 시간, 캐릭터, 공용 창고, 외출, AI 파견, 잠자기, 하루 처리
│     ├─ Time/GameTime.cs             날짜·시각, 계절, 외출 가능 시간, 시간대별 좀비 속도
│     ├─ Characters/                  특성, 기본 캐릭터 4명, 숙련도, 캐릭터 상태(Survivor)
│     ├─ Health/InfectionRules.cs     오염도·감염도, 정화제·억제제·완치제
│     ├─ Stress/StressRules.cs        스트레스 쌓임·풀림·단계
│     ├─ Needs/NeedsRules.cs          배고픔·목마름, 먹고 마시기
│     ├─ Crafting/Crafting.cs         제작법(정화제, 물 끓이기)과 제작 규칙
│     ├─ Home/                        집 설비(불, 빗물받이, 현관, 집 정화)와 집안일·치료
│     ├─ Field/FieldMap.cs            바깥 필드의 수색 대상과 고갈
│     ├─ Items/Items.cs               아이템, 인벤토리, 무게
│     └─ Outing/OutingBoard.cs        사거리 4방향 외출 기록
└─ tests/
   └─ Catpo.Core.Tests/               .NET 테스트. Core 소스를 링크해서 컴파일
```

- 규칙 코드 원본은 Unity 프로젝트 안에 한 벌만 둔다. 테스트 프로젝트는 그 파일을 링크해서 쓴다.
- Unity Test Framework도 NUnit 3 기반이라 같은 테스트를 Unity 안에서도 돌릴 수 있다.
- C# 문법은 Unity가 지원하는 범위(C# 9)에 맞춘다. 테스트 프로젝트도 `LangVersion 9.0`으로 묶어 두었다.

## 테스트 실행

```
dotnet test tests/Catpo.Core.Tests
```

- .NET SDK가 없으면 `apt-get install -y dotnet-sdk-8.0`
- 2026-11-08 기준 테스트 93개 통과

## Unity에서 여는 방법 (기획자 PC)

1. Unity Hub에서 **Add → Add project from disk**를 누르고 레포의 `unity` 폴더를 고른다.
2. 에디터 버전을 고르라고 하면 **Unity 6.6 (6000.6.8f1)**을 고른다.
3. Unity가 `ProjectSettings`, `Packages` 같은 나머지 파일과 `.meta` 파일을 만든다. 이 파일들은 레포에 커밋한다.
4. 콘솔 창에 오류가 뜨면 오류 메시지를 그대로 알려준다.

## 데이터 (제안)

- 아이템, 지역, 이벤트, 직업, 특성은 JSON 데이터 파일로 분리
- Unity 쪽 JSON 처리는 Unity 공식 패키지 `com.unity.nuget.newtonsoft-json` 사용 검토

## 클라우드 환경 메모

- .NET 8 SDK: `apt-get install -y dotnet-sdk-8.0` 으로 설치, NUnit 테스트 실행 확인 (2026-11-07)
- Godot 실행 파일은 이 환경에서 내려받을 수 없었음 (다운로드 경로가 막혀 있음). 이것이 Unity로 정한 배경 중 하나

## 기존 웹 초안

기획이 정해지기 전에 만든 HTML5 웹게임 초안(고양이 무인도 생존)은 첫 버전 개발을 시작하면서 삭제했다 (2026-11-08). git 기록(커밋 15c766a)에 남아 있다.
