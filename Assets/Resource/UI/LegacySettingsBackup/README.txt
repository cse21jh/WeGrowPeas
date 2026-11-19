설정창 교체 전 백업 (2026-11-19)

각 프리팹은 해당 씬의 기존 SettingPanel 전체 계층을 보존합니다.
같은 이름의 씬은 파일명 뒤의 씬 GUID 앞 8자리로 구별합니다.
SharedCanvas.prefab은 공용 Canvas.prefab 안에 있던 설정창입니다.
씬마다 달랐던 버튼·문구·토글 및 레이아웃을 별도로 보존했습니다.

프리팹 에셋은 씬 밖의 오브젝트 참조를 직접 저장할 수 없으므로,
각 .bindings.json에 원래 씬 경로·부모 계층·외부 참조의 GlobalObjectId를 함께 보관합니다.
원래 씬에서 백업을 인스턴스화한 뒤 SettingsDialogMigration.RestoreExternalBindings에
인스턴스와 해당 JSON 경로를 넘기면 기존 컨트롤러/버튼 연결을 복구할 수 있습니다.
옛 창으로 완전히 되돌릴 경우 새 SettingsDialog를 제거하고 SettingUI의 SettingsDialog 참조를 비운 뒤,
SettingPanel·볼륨 슬라이더·토글 참조를 복원한 패널의 항목으로 다시 지정해야 합니다.
공용 Canvas 백업은 공용 Canvas 프리팹 편집 모드에서 수동으로 참조를 연결합니다.

이 폴더는 보관용입니다. 현재 씬이나 Resources에서 자동으로 로드하지 않습니다.
