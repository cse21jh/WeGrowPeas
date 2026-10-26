# 회상 노트: 2D 에셋으로 책장 넘기기

첨부 시안처럼 정면에서 보는 노트에는 **2D 그림 + 넘기는 종이만 변형하는 UI 메시**가 적합하다.
Blender 모델, 뼈대 애니메이션, 3D 조명이 필요 없다. 페이지에 놓인 사진·TMP 글자·아이콘을
넘기기 직전에 RenderTexture로 담고, 그 이미지를 붙인 얇은 종이를 Canvas 안에서 휜다.
평소에는 실제 uGUI를 보여주므로 버튼과 타임라인 슬라이더를 그대로 사용할 수 있다.

참고 영상: [EndlessBook Demo](https://www.youtube.com/watch?v=C3YmOCgoAs0).
이번 구현은 정면 UI의 양방향 페이지 넘김에 집중한 독립 구현이다. 영상의 전체 3D 책 회전,
표지 열기·닫기, 마우스로 모서리를 붙잡고 끄는 기능은 포함하지 않는다.

## 바로 확인

1. `Assets/Scenes/RecallBookDemo.unity`를 열고 Play한다.
2. 아래 `<`, `>` 버튼으로 세 개 펼침면(총 여섯 페이지)을 넘긴다.
3. 다른 시작 씬이 강제 실행되면 `Tools > Always Start From StartScene`의 체크를 해제한다.

데모는 임시 그림과 24개 예시 기록을 사용한다. 회상 저장 파일을 생성하거나 수정하지 않는다.
현재 시작 씬의 기존 회상 UI를 교체하지 않았으므로, 최종 아트가 정해지면 아래 방법으로 연결한다.

프리팹: `Assets/Resources/Prefabs/Recall/Book/RecallBookExample.prefab`

- Canvas 아래에 놓는다. 부모에 GraphicRaycaster, 씬에 EventSystem이 필요하다.
- 기준 크기는 1180×800. 데모 Canvas는 1440×1000 기준이며 Expand로 작은 화면에도 맞춘다.
- `Tools > Recall Book > Create Example Prefab` 및 `Create Demo Scene`은 에셋이 있으면 선택만 한다.
  기존 에셋이나 사용자 씬을 덮어쓰지 않는다.

## 그림을 준비하는 방법

권장 레이어는 표지/종이 더미, 왼쪽 종이, 오른쪽 종이, 중앙 링, 책갈피·닫기 버튼이다.
사진과 설명은 종이 위의 일반 UI로 만든다. 중앙 링, 표지, 화살표, 닫기 버튼은 종이 밖에 둔다.
기존 임시 종이 더미(PageStack/PaperEdge)와 철심(Ring/RingShadow)은 제거했다.
종이 두께는 하드커버 그림에 함께 그려 `Cover`의 Image > Source Image에 적용한다.
철심은 투명 PNG로 준비하고, 빈 `PageTurnOverlay/Binding` 아래에 UI Image를 추가해서 넣는다.
PNG의 Texture Type은 Sprite (2D and UI)로 설정하고, Cover의 Image Color는 흰색으로 바꾸면 원본 색이 유지된다.

```text
RecallBookExample
  Cover                    하드커버와 종이 두께를 합친 2D 그림
  Pages                    LeftPage / RightPage를 각각 캡처
    LeftPage               사진 4개 + 기록 설명
    RightPage              사진 4개 + 기록 설명
  PageTurnOverlay          Pages보다 나중에 그려지는 효과·철심 컨테이너
    StationaryPage         넘기지 않는 기존 반쪽
    Binding                사용자 철심 이미지를 넣을 빈 컨테이너; 이동 종이가 덮는 부분만 가려짐
    TurningShadow
    TurningSheet           BookPageCurlGraphic
  Previous / Next          캡처 대상 밖
```

종이 이미지는 `Pages/LeftPage`, `Pages/RightPage`의 Image > Source Image에 각각 넣는다.
이 두 오브젝트의 RectTransform으로 종이 크기와 위치를 조절한다.
`BookPageTurner`의 Left Page / Right Page에 이 두 RectTransform을 연결한다.
기존 프리팹 인스턴스에서 참조가 비어 있으면 Pages 아래의 같은 이름으로 자동 연결한다.
넘길 때마다 실제 종이의 크기·위치·스케일을 읽으므로 TurningSheet나 PageTurnOverlay의 크기는
별도로 맞출 필요가 없다. 효과가 맞춰지는 동안 표지와 Binding의 배치는 그대로 유지된다.
Canvas는 Hierarchy의 뒤쪽 형제를 나중에 그리므로, Binding을 StationaryPage 뒤,
TurningSheet 앞에 둔다. 철심 자체를 끄지 않아도 넘기는 종이가 철심 위를 덮는다.
Binding에 별도 Canvas의 Override Sorting을 지정하면 이 순서를 벗어날 수 있으므로 사용하지 않는다.
각 페이지는 Pages 영역 밖으로 조금 커지거나 중앙에서 겹쳐도 된다. 좌우 종이 크기가 달라도
출발 종이의 안쪽 끝에서 도착 종이의 안쪽 끝으로 넘기며 시작·종료 크기를 각각 맞춘다.
종이는 책과 같은 평면에서 회전 없이 배치한다.
종이 배경은 가급적 불투명하게 만든다. 종이가 화면 밖으로 살짝 부풀 공간도 남긴다.
넘기는 종이를 잘라버릴 수 있으므로 `PageTurnOverlay`에 페이지 크기의 RectMask2D를 씌우지 않는다.

## 회상 이외의 UI에 재사용

공용 프리팹: `Assets/Resource/Sprites/UI/Book/BookUIBase.prefab`.
현재 디자인의 종이·표지·철심·넘김 효과를 복사하고, 회상 예제 콘텐츠와 `RecallBookExample`을 제외했다.
원본 `Assets/Resource/Sprites/UI/Book/BookUI.prefab` 및 회상 프리팹은 변경하지 않는다.
`Tools > Book UI > Create Reusable Prefab`은 공용 프리팹이 없을 때만 만든다.

1. 공용 프리팹을 복제하거나 Prefab Variant로 만들어 Canvas 아래에 놓는다.
2. `BookContent/Pages/LeftPage/Layout0`, `BookContent/Pages/RightPage/Layout0` 안에 해당 화면의 UI를 배치한다.
   슬롯 수나 컴포넌트 이름은 자유다. TMP, Image, Button, Slider, ScrollRect 등을 쓸 수 있다.
   `Layout0`은 여백을 둔 컨테이너이므로 여백도 RectTransform에서 조절할 수 있다.
3. 책 한 개에 여러 화면을 넣을 때는 Layout1 안에 다른 UI를 만든다.
   최상위 `Book Layout Controller`의 `Layouts` 각 항목은 좌·우 콘텐츠 한 쌍이다.
   예: 0번은 인벤토리 격자 + 설명, 1번은 도감 그림 + 수치, 2번은 설정 옵션 + 슬라이더.
4. Inspector의 `Add Empty Layout`을 누르면 새 좌우 컨테이너와 배열 연결이 함께 생긴다.
   `Preview 0`, `Preview 1` 버튼으로 편집할 화면을 고른다. Preview는 데이터 이벤트를 실행하지 않는다.
   시작 화면은 별도의 `Initial Layout`(0부터 시작) 값으로 정한다.
5. 버튼 On Click에는 `BookLayoutController.Show(int)`와 화면 번호를 지정한다.
   기본 화살표는 Layouts 순서로 이동한다. 필요 없으면 Previous / Next 연결을 비우고 버튼을 숨긴다.
   한 화면만 쓸 때는 Layouts를 한 항목으로 줄이고 안 쓰는 Layout1 오브젝트도 삭제한다.

책 전체 크기는 **최상위 BookUIBase의 RectTransform > Width / Height**로 조절한다.
`Book Root Scaler`가 `BookContent` 아래의 표지·종이·철심·버튼·내용·넘김 효과를 함께 축소한다.
예: 기준 1180×800에서 590×400으로 변경하면 전체가 절반 크기가 된다. 편집·Prefab Mode에서도 바로 적용된다.
기본 `Preserve Aspect`가 켜져 있으면 주어진 영역 안에 비율을 유지해 맞추므로 한쪽에 여백이 남을 수 있다.
가로·세로를 각각 늘이려면 이 옵션을 끈다. Reference Size는 원본 디자인 크기 1180×800을 유지한다.
기존 최상위 Scale도 함께 적용된다. 전체와 같이 커지거나 작아질 새 요소는 `BookContent` 아래에 추가한다.
`BookContent` 자체의 크기·스케일은 자동 계산된다. 개별 종이와 콘텐츠는 그 아래에서 계속 편집한다.

공용 컨트롤러는 내용의 글자나 사진을 덮어쓰지 않는다. 고정 문구는 Inspector에서 그대로 편집한다.
게임 데이터는 각 UI 전용 스크립트가 채운다. 각 Layout의 `On Show`에 데이터 갱신 함수를 연결하면
활성화 직후, 넘김용 캡처 전에 실행된다. 초기 열기와 다시 열기에도 호출된다.
비동기 사진·데이터 로딩은 전환 전에 끝내고, On Show에서는 UI에 즉시 반영한다.
On Show 안에서 다른 페이지 이동이나 책 닫기를 호출하지 않는다.

```csharp
using WeGrowPeas.UI.Book;

// BookLayoutController bookLayouts;
bookLayouts.Show(1);               // 1번 레이아웃으로 넘김; 버튼에도 연결 가능
bookLayouts.ShowImmediate(0);      // 애니메이션 없이 변경
bool accepted = bookLayouts.TryShow(2); // 범위 밖 / 넘김 중 / 같은 화면이면 false
```

전환은 콘텐츠 루트만 켜고 끈다. 기존 컨트롤의 값은 각 컨트롤러가 초기화하지 않는 한 유지되며,
책을 닫았다 열어도 마지막 레이아웃을 유지한다. 넘기는 동안 클릭은 잠기고 연속 요청은 무시한다.
레이아웃 루트는 해당 종이의 자식이어야 하며, 종이 자체를 지정하거나 다른 레이아웃 루트와
공유·중첩하면 안 된다. 왼쪽 또는 오른쪽 참조를 비우면 그쪽은 빈 종이를 표시한다.
레이아웃 배열에서 항목을 제거할 때는 해당 콘텐츠 오브젝트도 삭제하거나 비활성화한다.

인벤토리의 다음 아이템 페이지처럼 **레이아웃은 같고 데이터만 바뀌는 경우**에는 해당 화면의
스크립트에서 `BookPageTurner.TryTurn(forward, () => RefreshItems())`를 호출한다.
`BookLayoutController`는 화면 종류 전환만 담당한다. 이 경우 기본 화살표의 Previous / Next
연결을 비우고, 전용 데이터 페이징 함수에 버튼을 연결한다.
실제 `RecallRoot`의 목록은 `RecallUIController`가 담당한다. 데모 전용인 `RecallBookExample`을
같은 책에 함께 붙이지 않는다.

넘김 효과의 기존 `WeGrowPeas.RecallBook` 네임스페이스는 직렬화 호환성을 위해 유지하지만,
`BookPageTurner`, `BookPageSnapshot`, `BookPageCurlGraphic`은 회상 데이터에 의존하지 않는다.
일반 uGUI 콘텐츠를 지원하며 페이지 밖의 별도 Canvas나 3D 오브젝트는 캡처 대상에 포함되지 않는다.

## 모서리 닫기 버튼

공용 책의 `BookContent/CloseButton`은 `BookCloseButton`을 사용한다. 일반 상태는
`xBtn0`·`rightPage`·`bookBase`, 마우스 하이라이트·누름·키보드 선택 상태는
`xBtn1`·`rightPage1`·`bookBase1`을 함께 표시한다. 비활성화하거나 책을 닫으면 일반 상태로 복구한다.
`Book Base Image`는 `BookContent/Cover`에 연결한다. 배경까지 함께 바꿔야 접힌 모서리 뒤로
기존 종이 면이 드러나지 않는다.

버튼의 두 그림은 각자 RectTransform을 사용한다. `CloseButton`의 Image가 작은 그림이고,
자식 `xBtn1`의 Image가 큰 그림이다. 큰 그림은 오른쪽 위를 고정하고 왼쪽·아래로 펼쳐진다.
Sprite Swap은 사용하지 않으므로 큰 그림이 작은 버튼 사각형 안에 압축되지 않는다.
두 그림의 크기와 위치는 원본 PNG의 슬라이스 좌표를 종이 크기에 맞춰 배치했다.
책 전체 크기는 기존처럼 `BookRootScaler`로 조절한다.

`RecallRoot` 인스턴스의 On Click에는 `RecallUIController.CloseRecall()`이 연결되어 있다.
다른 화면에 공용 책을 사용할 때는 `BookCloseButton`의 On Click에 그 화면의 닫기 함수를 연결한다.
페이지 콘텐츠나 목록 페이징과는 별도로 동작한다.

## 책 열기·닫기와 페이드

`RecallRoot > RecallPanel > BookUIBase`의 `Book Close Motion`에서 열기·닫기 동작을 조절한다.
기존 X 버튼의 CloseRecall 연결을 유지하며, 위로 올라갔다가 화면 아래로 빠져나간 뒤 패널을 닫는다.
회상 버튼의 OpenRecall은 같은 경로를 역재생해 화면 아래에서 올라온 뒤 살짝 내려앉는다.

- Rise Height: 위로 튀는 높이. 기본 50 UI 단위.
- Rise Duration / Rise Curve: 닫을 때 상승 시간(기본 0.14초)과 이동 곡선. 열 때 마지막 안착 구간에 역으로 적용된다.
- Drop Duration / Drop Curve: 닫을 때 하강 시간(기본 0.34초)과 이동 곡선. 열 때 처음 올라오는 구간에 역으로 적용된다.
- Offscreen Padding: 책이 화면 아래로 완전히 내려간 뒤의 추가 여백. 기본 40.
- Tint Fade Curve: RecallPanel의 어두운 배경 이미지 페이드 곡선.
- Title Fade Curve: 왼쪽 Title의 글자와 기록 개수 페이드 곡선.

이동 커브의 X는 각 구간의 시간 0~1, Y는 이동 거리 0~1이다. 시작은 (0,0), 끝은 (1,1)로 둔다.
페이드 커브의 X는 전체 열기·닫기 시간 0~1, Y는 페이드 진행도 0~1이다.
배경과 Title은 Graphic.DOFade로 색상의 알파만 변경한다. 열 때는 0에서 각 요소의 원래 알파까지,
닫을 때는 원래 알파에서 0까지 재생하며 RGB와 원래의 반투명 디자인을 유지한다.

화면 크기에 맞춰 하강 거리를 자동 계산한다. 이동과 페이드는 하나의 DOTween Sequence에 묶이며,
SetUpdate(true)로 게임이 일시 정지되어도 재생한다. 재생 중 중복 입력은 무시한다.
완료·비활성화·트윈 취소 시 원래 위치, 색상과 입력 상태를 복구한다.
씬 전환처럼 즉시 정리해야 할 때는 RecallUIController.CloseRecallImmediately()를 사용할 수 있다.

## 기존 회상 목록과 연결

`Assets/Resource/Sprites/UI/Book/RecallRoot.prefab`은 `RecallUIController`에서
`BookUIBase`의 **레이아웃 1(회상_목록)**에 현재 프로필의 기록을 최신순으로 표시한다.
기존 `ListScroll`은 비활성화되어 있으며, 왼쪽 4개·오른쪽 4개의 `RecallButton`을 재사용한다.
화살표는 레이아웃 번호를 바꾸지 않고 다음/이전 기록 8개로 책장을 넘긴다.
양쪽 하단 숫자는 같은 펼침면 번호/전체 펼침면 수이며, 기록이 없으면 `0/0`이다.
책을 다시 열면 목록을 다시 읽고 첫 펼침면부터 표시한다.

`RecallUIController`의 `Book Layouts`, `Left/Right Page Slots`, `Previous/Next Page`,
양쪽 빈 목록 문구와 페이지 번호가 프리팹에 연결되어 있다.
이 `RecallRoot` 안의 `BookLayoutController`는 기본 Previous/Next 참조를 비워
목록 넘김과 레이아웃 넘김이 동시에 실행되지 않게 했다. 공용 `BookUIBase`의 기본 동작은 유지한다.
카드를 추가하거나 순서를 바꿀 때는 `Left/Right Page Slots` 배열도 화면 순서에 맞춘다.

카드의 사진은 런타임 PNG를 표시하는 `RawImage`로 연결했다. 사진이 없으면 `사진 없음`을 표시한다.
`RecallListSlot`이 기록 번호, 날짜·시간, 일수·식물·승천 단계를 채우며 카드의 모양과 배치는
`RecallButton.prefab`에서 편집한다. `RecallRoot` 밖의 기존 씬은 종전 목록 방식도 계속 지원한다.

카드를 누르면 저장된 기록을 읽어 **레이아웃 2**를 연다. `BookRecallDetail`의
`RecallTimelineUI`가 마지막 일자의 텃밭, 아이템, 저주, 웨이브, 골드와 변화량을 채운다.
레이아웃 1의 On Show는 `RestoreBookList`, 레이아웃 2의 On Show는 `RenderBookDetail`에
연결되어 있다. 화면을 채우는 시점은 원래 페이지 캡처 이후다.
`목록으로` 또는 ESC로 돌아오면 선택 전의 목록 펼침면을 유지한다.

별도 데모 `RecallBookExample`에도 `Use Saved Records` 옵션이 있지만 실제 `RecallRoot`에서는
사용하지 않는다.

직접 만든 목록 컨트롤러에서는, 페이지 내용 변경을 아래 콜백 안에서 한 번 실행한다.

```csharp
using WeGrowPeas.RecallBook;

// bookTurner: Inspector에 연결한 BookPageTurner
// RenderSpread: 양쪽 페이지의 카드와 페이지 번호를 즉시 갱신하는 함수
if (targetSpread >= 0 && targetSpread < spreadCount)
{
    bookTurner.TryTurn(targetSpread > currentSpread, () =>
    {
        currentSpread = targetSpread;
        RenderSpread(currentSpread);
    });
}
```

콜백은 동기적으로 끝나야 한다. 파일·서버에서 가져올 사진은 미리 준비한다.
넘김 중 새 요청은 false를 반환하며 콜백을 실행하지 않는다.
캡처를 지원하지 못하면 목적지 화면을 즉시 표시하는 방식으로 전환한다.
목적지 데이터는 넘김 시작 시 확정된다. 창을 도중에 닫으면 그 목적지 상태를 유지한다.
`Time.unscaledDeltaTime`을 사용하므로 게임의 timeScale이 0이어도 넘김은 끝난다.

`RecallUIController.BuildBookList()`와 `RenderSpread()`가 책 목록을 채운다.
`BuildList()`는 책 참조가 없는 이전 씬의 Grid 목록을 위한 호환 경로다.

## 상세·타임라인 화면

세 번째 줄 시안처럼 왼쪽에는 밭, 오른쪽에는 수치·아이콘을 넣고 같은 `Pages` 구조를 사용한다.
왼쪽 날짜 슬라이더는 즉시 날짜를 바꾸며, 책 하단 화살표는 이전/다음 일자로 넘긴다.
양쪽 페이지 번호는 일자 스냅샷의 순번/전체 개수다. 넘김 중에는 날짜 입력을 받지 않는다.

기존 GardenPlant 32개를 재사용한다. 실제 저장 순서대로 세로 4칸을 먼저 채우며,
빈 땅은 아이콘과 그림자를 숨기고 아직 확장하지 않은 땅은 DisabledBlocker를 표시한다.
사용자가 만든 가로 스크롤과 칸 크기를 유지하며 Content 폭을 8열에 맞춘다.
아이템과 저주는 기존 RecallIconSlot.prefab을 생성한다. 항목이 많으면 패널 안에 들어가도록
칸 크기를 조절한다. 수량, 새로 획득한 항목 강조, 마우스 설명을 유지한다.

`결과 보기`는 기존 결과 요약과 그래프를 열고, `책으로`로 상세 페이지에 복귀한다.
일자별 기록이 없는 파일도 안내를 표시하고 결과 화면을 열 수 있다.
기존 전체 화면 타임라인은 결과 화면의 타임라인 버튼에서 계속 사용할 수 있다.

## 캐주얼한 셀 느낌 조절

`ToonBookPage.mat`는 `WeGrowPeas/UI/Toon Book Page` 셰이더를 참조한다.
씬의 빛과 상관없이 원본 그림 색을 유지하고, 굽힘 정도에 따라 정해진 단계의 색조를 곱한다.

| 설정 | 기본값 | 효과 |
| --- | --- | --- |
| Page Turner / Duration | 0.65초 | 넘김 속도 |
| Turning Sheet / Curl | 0.75 | 종이 휘어짐 |
| Turning Sheet / Perspective | 0.16 | 화면 쪽으로 들리는 느낌; 0이면 평평한 투영 |
| Material / Cel Bands | 3 | 2이면 더 또렷한 셀 음영 |
| Material / Shadow Strength | 0.24 | 사진·글자에 덮이는 음영 강도 |
| Material / Warm Shadow Tint | 갈색 | 검정보다 따뜻한 종이 그림자 |
| Material / Paper Edge Width | 0.0025 | 종이 테두리 두께, 0이면 끔 |
| Snapshot / Texture Width | 1024 | 종이 한 장의 캡처 너비. 더 선명하게 하려면 2048, 성능 우선이면 512 |

`Reduce Motion`을 켜면 애니메이션 없이 바로 바뀐다.
프리팹의 카드 Button은 잠금 중 색이 어두워지지 않게 Disabled Color를 Normal Color와 같게 설정했다.
별도 디자인을 연결할 때도 같은 설정을 권장한다.

3D 책 모델을 나중에 쓰더라도 같은 원칙(무광 종이, 2~3단계 음영, 갈색 얇은 외곽선,
약한 원근)을 적용하면 된다. 이번 셰이더는 Canvas 전용이며 일반 MeshRenderer용 조명 셰이더는 아니다.

## 렌더링과 비용

- Unity 6000.3.6f1, URP 17.3의 프로젝트 2D Renderer에서 확인했다.
- 정지 중에는 별도 캡처 카메라가 실행되지 않는다. 넘길 때만 이동 종이 앞·뒷면과 반대편 기존 종이를 각각 캡처한다.
- 기본 이동 종이는 48×8 스트립 셀, 삼각형 768개다.
- 현재 종이 비율에서 장당 약 1024×1285의 RGBA8와 깊이 버퍼가 필요하다. 전환용 세 장을 재사용하고 닫을 때 해제한다.
- ReadPixels는 검증용 PNG 저장에서만 사용한다. 실제 넘김에는 GPU→CPU 이미지 읽기가 없다.
- 캡처는 UI를 먼 위치의 임시 Canvas로 동기적으로 옮겼다가 같은 호출 안에서 부모·레이어·Transform을 복구한다.
  루트 Canvas도 캡처 레이어에 두어야 2D Renderer에서 출력된다.
- 캡처 대상은 일반 uGUI 페이지 콘텐츠로 한정한다. 대상 안의 별도 Canvas, CanvasScaler,
  부모 변경에 반응하는 커스텀 스크립트, 외부 카메라 종속 효과는 추가 검증이 필요하다.
- 캡처용 위치 `(0, -30000, 0)`은 게임 오브젝트가 없는 공간이어야 한다.
- 기본 UI Mask 스텐실과 RectMask2D 사각 클리핑 경로를 넣었다. 커스텀 마스크·소프트 마스크 플러그인은 검증하지 않았다.

URP 렌더 요청은 [Unity 공식 문서](https://docs.unity3d.com/kr/current/Manual/urp/User-Render-Requests.html)의
`UniversalRenderPipeline.SingleCameraRequest` 방식을 사용한다.

## 검증

공용 레이아웃 전환은 Play Mode에서 `Tools > Book UI > Validate In Play Mode`로 검증한다.
격자·설명 화면과 그림·슬라이더 화면을 임시 인스턴스에 만들고, 전환·입력 잠금·값 유지·재열기,
최상위 Width / Height / Scale 변경에 따른 전체 크기와 축소 상태의 넘김 정렬을 검사한다.
검증용 UI는 공용 프리팹에 저장하지 않는다. 확인용 이미지는 `Temp/BookLayoutValidation`에 생성한다.

Play Mode에서 `Tools > Recall Book > Validate In Play Mode`를 실행한다.
테스트가 임시 Canvas를 만들었다가 제거하며, timeScale을 원래 값으로 복구한다.
기존 씬을 열거나 저장하지 않는다.

검사 범위: 앞/뒤 넘김, 첫/마지막 페이지, 빠른 연속 요청, timeScale=0, Reduce Motion,
도중에 닫기·다시 열기, UI 부모와 위치 복구, 불투명 캡처와 실제 이동 메시 생성,
양방향 넘김의 시작·종료 크기와 위치, 좌우 크기·스케일·피벗 변경 후 정렬, 철심 배치 유지.
`Temp/RecallBookValidation/01-open.png`부터 `05-reverse-back.png`까지는 눈으로 확인할 이미지다.

추가 코드와 셰이더 컴파일 오류 없음. 실제 렌더에서 사진·글자의 앞면과 뒷면 방향을 확인했다.
기존 시작 씬에서는 별도로 `RecallTimelineUI.SetDayIndex()`의 빈 일자 목록 접근 오류가
재생 시작 때 관찰됐다. 이 파일은 이번 효과 작업에서 변경하지 않았다.
