# catpo

**좀비냥**: 좀비 아포칼립스 생존 모바일 게임 프로젝트 (Unity).

고양이화 바이러스가 퍼진 가상 도시 목성시. 고양이 귀가 달린 생존자 4명이 옥탑방 주택에서 버팁니다.
낮에는 사거리에서 각자 다른 방향으로 수색하고, 밤에는 집에 모입니다. 무게, 귀환 시간, 오염도가 긴장감을 만듭니다.

지금은 첫 버전(MVP)을 개발하고 있습니다.

- 기획 문서: [docs/](docs/README.md)
- 첫 버전 범위: [docs/15-mvp.md](docs/15-mvp.md)
- 게임 규칙 코드: `unity/Assets/Scripts/Core/` (Unity 없이 테스트 가능한 순수 C#)
- 테스트: `dotnet test tests/Catpo.Core.Tests`
- Unity에서 여는 방법: [docs/06-tech.md](docs/06-tech.md#unity에서-여는-방법-기획자-pc)
