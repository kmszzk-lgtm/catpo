# 기술 구조

## 엔진 (확정)

- **Unity** (2026-11-07 결정). 버전은 **미정**. 최신 Unity 6 LTS를 추천.
- 설치할 때 Android Build Support 모듈도 함께 설치.

## 작업 방식 (제안)

클라우드 작업 환경(Claude Code)에서는 Unity 에디터를 실행할 수 없다. 그래서 코드를 두 층으로 나눈다.

| 층 | 내용 | 어디서 확인 |
|---|---|---|
| 규칙 코드 (Core) | 자원 계산, 수색 결과, 귀환·감염 판정, 이벤트, 낮밤 흐름, 저장 데이터. `UnityEngine`을 참조하지 않는 순수 C# | 클라우드에서 .NET + NUnit으로 자동 테스트 |
| Unity 코드 | 장면, UI, 입력, 그래픽, 사운드, 네트워크. Core를 호출해서 화면에 보여줌 | 기획자 PC의 Unity 에디터, 휴대폰 빌드 |

이렇게 나누면 저장과 멀티 동기화도 Core의 상태 데이터만 다루면 된다.

## 폴더 구조 (제안)

```
catpo/
├─ docs/                         기획 문서
├─ unity/                        Unity 프로젝트 (Assets, Packages, ProjectSettings)
│  └─ Assets/Scripts/
│     ├─ Core/                   규칙 코드. Catpo.Core.asmdef (엔진 참조 없음)
│     └─ Game/                   Unity 연결 코드. Catpo.Game.asmdef
└─ tests/
   └─ Catpo.Core.Tests/          .NET 테스트 프로젝트. unity/Assets/Scripts/Core/ 소스를 링크해서 컴파일
```

- 규칙 코드 원본은 Unity 프로젝트 안에 한 벌만 둔다. 테스트 프로젝트는 그 파일을 링크해서 쓴다.
- Unity Test Framework도 NUnit 기반이라 같은 테스트를 Unity 안에서도 돌릴 수 있다.
- C# 문법은 Unity가 지원하는 범위(C# 9)에 맞춘다.

## 데이터 (제안)

- 아이템, 지역, 이벤트, 직업, 특성은 JSON 데이터 파일로 분리
- Unity 쪽 JSON 처리는 Unity 공식 패키지 `com.unity.nuget.newtonsoft-json` 사용 검토

## 클라우드 환경 메모

- .NET 8 SDK: `apt-get install -y dotnet-sdk-8.0` 으로 설치, NUnit 테스트 실행 확인 (2026-11-07)
- Godot 실행 파일은 이 환경에서 내려받을 수 없었음 (다운로드 경로가 막혀 있음). 이것이 Unity로 정한 배경 중 하나

## 기존 웹 초안

레포 루트의 `index.html`, `css/`, `js/`는 기획이 정해지기 전에 만든 HTML5 웹게임 초안(고양이 무인도 생존)이다.
현재 기획과 관련이 없으며, Unity 프로젝트를 만들 때 삭제할 예정이다.
