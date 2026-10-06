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

## 기존 회상 목록과 연결

가장 쉬운 방법은 프리팹의 `RecallBookExample > Use Saved Records`를 켜는 것이다.
현재 프로필의 RecallStore 기록을 최신순으로, 한 펼침면당 8개씩 불러온다.
`Detail UI`에 기존 `RecallDetailUI`를 연결하면 기록 클릭으로 기존 상세 화면을 연다.
또는 `Entry Selected(string)` 이벤트에 별도 화면 전환 함수를 연결할 수 있다.
다른 프로필로 바꾼 뒤에는 책을 닫았다 열어 목록을 갱신한다.

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

기존 `RecallUIController.BuildList()`는 모든 기록을 한 Grid에 놓는 구조다.
새 디자인으로 전환할 때는 위 프리팹의 목록 표시 부분으로 대체하거나, 한 펼침면의 8개만
양쪽 컨테이너에 배치하도록 목록 그리기 함수를 분리한다. 기존 사진 로딩과 상세 UI는 재사용한다.

## 상세·타임라인 화면

세 번째 줄 시안처럼 왼쪽에는 밭, 오른쪽에는 수치·아이콘을 넣고 같은 `Pages` 구조를 사용한다.
타임라인 슬라이더는 캡처 대상 밖에 둔다. 연속 드래그 중에는 기존 방식대로 즉시 갱신하고,
목록↔상세 전환이나 이전/다음 페이지 버튼에만 넘김 효과를 적용하는 편이 조작하기 쉽다.
자주 바뀌는 그래프나 애니메이션도 넘기는 0.65초 동안에는 캡처 당시 모습으로 고정된다.
최종 상세·타임라인 레이아웃 이식은 포함하지 않았다.

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
