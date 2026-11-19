환경설정창

프리팹: Assets/Resource/UI/SettingsDialog/SettingsDialog.prefab
기존 설정창이 있던 스타트 4종, Garden_GrassUpdate, Tutorial 및 TutorialChapter1~3,
기존 Garden 테스트 씬 2종과 공용 Canvas.prefab에 연결되어 있습니다.
설정창 전용 테스트 씬은 제거했습니다. 시작할 때는 닫혀 있으며 기존 설정 버튼/ESC로 엽니다.
X 또는 Escape로 닫습니다. 일반/조작의 긴 목록은 스크롤됩니다.

일반: 언어, 교배 결과 팝업, 읽은 메시지 알림, 무한 모드(테스트), 도감, 메인 화면 이동, 종료, 1000골드 치트
그래픽: 해상도, 화면 모드, 품질, 수직 동기화, 프레임 제한
사운드: 배경음·효과음 볼륨
조작: 기존 GameInput의 6개 단축키와 초기화. 중복 키는 서로 교환하고 ESC로 지정을 취소합니다.

기존 DeviceSettings/settings.json, Loc, GameInput, SoundManager를 사용합니다.
볼륨은 즉시 반영하고 저장을 짧게 지연하여 슬라이더 드래그 중 쓰기를 줄입니다.
창을 닫을 때 남은 변경을 저장합니다. 기존 저장 파일은 그래픽 기본값을 유지합니다.
해상도/화면 모드는 데스크톱 Player에서 적용되며 Editor Game View는 바꾸지 않습니다.
브라우저에서 지원하지 않는 창 설정과 종료 버튼은 비활성화됩니다.
게임 매니저가 없는 씬에서는 도감/메인 화면/골드 버튼이 비활성화됩니다.
품질은 기존 GraphicsQuality의 낮음/보통/높음과 graphicsQuality 저장값을 사용합니다.
Unity QualitySettings의 품질 레벨은 변경하지 않아 렌더 파이프라인·VSync 기본값을 보존합니다.
무한 모드는 기존 EndlessMode.Enabled와 endlessModeEnabled 저장값을 사용합니다.

설명 편집:
1. 프로젝트 창에서 SettingsDialog.prefab을 선택하거나 씬의 SettingsDialog 루트를 선택합니다.
2. SettingsDialogView 인스펙터의 '편집할 설정'에서 항목을 고릅니다.
3. 각 언어의 이름/설명을 수정하고 '번역표에 저장 · 적용'을 누릅니다.
4. 플레이 중이면 언어 변경 이벤트를 통해 화면에 즉시 반영됩니다.
'CSV 파일 선택'으로 원본을 찾을 수 있고 '다시 불러오기'는 미저장 편집을 취소합니다.
항목을 바꾸기 전에 저장하세요. 문구 저장은 프리팹 인스턴스별이 아니라 공통 번역표에 적용됩니다.

문구의 유일한 원본:
Assets/Resources/Localization/settings-dialog.csv
키 예: settings-dialog.Resolution.name / settings-dialog.Resolution.description
Descriptions/*.asset은 설정 항목과 번역 키를 연결하는 개별 ScriptableObject입니다.
기존 LocGroup/ILocKeyed/LocKey 규칙을 사용하며 문구를 SO에 중복 저장하지 않습니다.
기존 Tools > Localization > Localization Window의 검사·내보내기·가져오기도 그대로 사용합니다.
CSV에 언어 열을 추가하면 인스펙터도 그 열을 표시합니다.
설정 제목·탭·공통 안내 문구 역시 같은 CSV의 settings.dialog.* 키로 관리합니다.
SO 파일을 직접 선택해도 동일한 문구 편집기가 표시됩니다.

재사용:
프리팹을 씬에 추가하고 EventSystem이 있는지 확인합니다.
자체 Canvas/CanvasScaler를 포함하며 Expand로 원본 비율을 유지합니다.
다른 Canvas 프리팹 안에 포함되어 있으면 Awake에서 씬 루트로 옮겨 자체 스케일러를 사용합니다.
'시작할 때 열기'는 기본적으로 꺼져 있습니다. SettingUI.settingsDialog에 연결합니다.
기존 SettingUI의 Show/Hide/Toggle과 버튼·ESC 진입점을 유지하며 이전 패널은 씬에서 제거했습니다.
직접 연결할 때는 SettingsDialogView.Open()/Close(), OnTabChanged, OnClosed를 사용합니다.
설정창이 열리면 일시정지와 클릭 차단을 적용하고 닫을 때 직전 상태로 되돌립니다.

PNG 출처·배율은 FigmaSource.json에 기록되어 있습니다. 원본 알파를 보존하며
벡터 장식은 4배, 내용 배경은 2배로 내보냈습니다. 나무 패널은 원본 래스터입니다.
반투명 UI의 색을 Figma와 맞추는 전용 머티리얼을 사용합니다.
나무/내용 배경색을 바꾸면 OverWood/OverContent 머티리얼의 배경색도 갱신해야 합니다.

폰트:
Tenada는 기존 Assets/TextMesh Pro/Fonts/Tenada/Tenada SDF.asset을 공유합니다.
설정창 폴더에 있던 중복 Tenada SDF는 제거했습니다.
기존에 없던 Cafe24Ssurround 원본과 SDF는 Assets/TextMesh Pro/Fonts/Cafe24Ssurround에 모았습니다.
모든 설정창 텍스트의 폰트는 TMP 폴더에서 관리하며 재생성 도구도 이를 재사용합니다.

기존 설정창 백업:
Assets/Resource/UI/LegacySettingsBackup에 씬별 원형 프리팹과 공용 Canvas의 설정창을 보관합니다.
각 .bindings.json에는 원래 씬·계층 위치와 외부 컨트롤러 참조를 GlobalObjectId로 기록했습니다.
백업은 런타임에 포함하거나 자동으로 생성하지 않습니다.

재생성: Tools > UI > Settings Dialog > Create Prefab and Test Scene
이 명령은 독립 프리팹과 테스트 씬을 다시 만듭니다. 수동 레이아웃 수정은 먼저 보존하세요.
기존 설명 연결 SO와 번역 CSV 내용은 덮어쓰지 않습니다.
게임 씬이나 빌드 씬 목록은 변경하지 않습니다.

효과 조절:
SettingsDialog 루트의 SettingsDialogMotion에서 열기/닫기 시간, 이동 거리,
이동 커브와 알파 커브를 조절합니다. 기본값은 0.28초, 아래쪽 90 UI 단위입니다.
열기는 알파 0→1과 위로 이동, 닫기는 같은 경로를 거꾸로 재생합니다.
닫는 도중 다시 열어도 현재 진행도에서 이어지며 닫기 완료 후 일시정지를 해제합니다.

Modal/Window/Close의 SettingsCloseButtonHover에서 확대 배율(기본 1.08),
확대 시간(0.12초), 한 바퀴 회전 시간(0.32초), 회전 속도 커브를 조절합니다.
진입 시 Figma의 빨간 PNG, 이탈 시 원래 흰 PNG를 표시합니다.
회전 속도 커브는 기본 0→1→0이며 커브 면적을 정규화하여 매번 360도 회전합니다.
연속 진입/이탈은 회전을 이어서 재생합니다. 클릭 영역은 변하지 않습니다.
닫기 클릭 또는 상호작용 비활성화 시 회전은 즉시 취소하고 원래 각도로 되돌립니다.
각 탭 Background와 Close의 SettingsButtonFeedback에서 호버와 클릭 펀치 커브를 편집합니다.
닫기 버튼의 펀치는 별도 ClickFeedback 레이어를 사용해 기존 호버 확대와 충돌하지 않습니다.
모든 효과는 unscaled time을 사용해 게임 일시정지 중에도 동작합니다.

드롭다운은 Figma의 테두리/배경/구분선/체크 PNG를 사용합니다.
선택된 항목 오른쪽에 체크를 표시하고 행 배경을 강조합니다.
안쪽 반지름에 맞춘 마스크가 첫/마지막 행의 하이라이트도 둥근 테두리 안으로 자릅니다.
최대 7행을 표시하며 더 긴 목록은 스크롤됩니다. 펼칠 때 선택된 항목이 보이게 이동합니다.
선택 행/호버 색상은 Dropdown/Template/Viewport/Content/Item의
SettingsDropdownOption에서 조절할 수 있습니다.

효과만 갱신: Tools > UI > Settings Dialog > Update Effects and Dropdowns
이 명령은 기존 레이아웃과 번역을 유지하며 효과 컴포넌트 및 드롭다운 템플릿만 갱신합니다.
