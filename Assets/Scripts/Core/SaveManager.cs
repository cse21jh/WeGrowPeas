using UnityEngine;

/// <summary>
/// 계정(프로필) 진행을 파일(<see cref="ProfileData"/>)과 주고받는다.
/// 무엇을 저장하는지는 각 시스템의 SaveTo/LoadFrom이 알고, 여기서는 순서와 파일 입출력만 맡는다.
///
/// 저장 시점: 각 시스템이 값을 바꾸면 <see cref="RequestProfileSave"/>를 부르고,
/// 그 프레임 끝(LateUpdate)에 한 번 모아서 쓴다. 종료 시 저장은 보조일 뿐이다.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    /// <summary>
    /// 프로필 저장 형식 버전. 구조가 바뀌면 올리고 <see cref="MigrateProfile"/>에 보정을 추가한다.
    /// 필드가 늘어나는 정도는 JsonUtility가 기본값으로 채우므로 올릴 필요 없다.
    /// </summary>
    public const int ProfileVersion = 1;

    // 로드가 끝나기 전에 저장하면 기본값으로 기존 프로필을 덮어쓴다. 그걸 막는 표시.
    private bool isProfileLoaded;

    // 저장 요청 표시. 매니저가 아직 없을 때 요청돼도 로드 후 첫 LateUpdate에 반영된다.
    private static bool saveRequested;

    /// <summary>프로필 값이 바뀌었음을 알린다. 이번 프레임 끝에 한 번 저장된다.</summary>
    public static void RequestProfileSave() => saveRequested = true;

    // 현재 프로필의 누적 플레이 시간(초). 매 프레임 바뀌므로 저장 요청 없이
    // 다른 저장(하루 종료·런 종료·게임 종료)에 실려 기록된다.
    private double playTimeSeconds;

    /// <summary>현재 프로필의 누적 플레이 시간(초). 아직 파일에 쓰지 않은 시간도 포함.</summary>
    public double PlayTimeSeconds => playTimeSeconds;

    /// <summary>플레이 시간을 더한다. 런 중에 GameManager가 매 프레임 부른다.</summary>
    public void AddPlayTime(float seconds)
    {
        if (!isProfileLoaded || seconds <= 0f) return;
        if (!Application.isFocused) return; // 창을 내려둔 시간은 세지 않는다
        playTimeSeconds += seconds;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LegacySaveMigration.RunIfNeeded();
        ProfileStore.SelectLastProfile();
        CodexProgress.Invalidate(); // 프로필이 정해지기 전에 누가 읽었다면 Profile_0 것일 수 있다
        LoadProfileData();
        if (isProfileLoaded) AchievementSystem.OnProfileLoaded();
    }

    private void LateUpdate()
    {
        if (saveRequested && isProfileLoaded) SaveProfileData();
    }

    void OnApplicationQuit()
    {
        SaveProfileData();
        DeviceSettings.Save();
    }

    private void LoadProfileData()
    {
        string path = ProfileStore.CurrentProfileFile;
        if (!SaveIO.Exists(path))
        {
            isProfileLoaded = true; // 새 프로필: 지금 상태(기본값)가 곧 프로필
            return;
        }

        if (!SaveIO.TryRead(path, out ProfileData profileData))
        {
            // 파일은 있는데 못 읽었다. 여기서 저장을 허용하면 기존 진행이 기본값으로 덮인다.
            Debug.LogError($"[SaveManager] 프로필을 읽지 못해 이번 실행에서는 프로필을 저장하지 않습니다: {path}");
            return;
        }

        if (profileData.version > ProfileVersion)
        {
            Debug.LogError($"[SaveManager] 더 새로운 빌드의 프로필입니다 (v{profileData.version} > v{ProfileVersion}). 저장하지 않습니다: {path}");
            return;
        }
        MigrateProfile(profileData);

        playTimeSeconds = profileData.meta.playTimeSeconds;

        // 시스템별 복원. 각 필드가 무엇인지는 해당 시스템의 LoadFrom이 안다.
        AbilityManager.Instance.LoadProfile(profileData.ability);
        UnlockManager.LoadFrom(profileData.unlock);
        DawnSystem.LoadFrom(profileData.dawn);
        MessengerSaveSystem.LoadFrom(profileData.messenger);
        AchievementSystem.LoadFrom(profileData.achievement);

        isProfileLoaded = true;
    }

    public void SaveProfileData()
    {
        saveRequested = false;
        if (!isProfileLoaded) return;
        if (AbilityManager.Instance == null)
        {
            // 일부만 채운 프로필로 덮어쓰지 않는다.
            Debug.LogWarning("[SaveManager] 매니저가 준비되지 않아 프로필 저장을 건너뜁니다.");
            return;
        }

        var profileData = new ProfileData { version = ProfileVersion };
        profileData.meta.lastSavedUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        profileData.meta.playTimeSeconds = playTimeSeconds;

        // 시스템별 저장. LoadProfileData와 같은 순서로 두어 짝을 눈으로 확인할 수 있게 한다.
        AbilityManager.Instance.SaveProfile(profileData.ability);
        UnlockManager.SaveTo(profileData.unlock);
        DawnSystem.SaveTo(profileData.dawn);
        MessengerSaveSystem.SaveTo(profileData.messenger);
        AchievementSystem.SaveTo(profileData.achievement);

        SaveIO.Write(ProfileStore.CurrentProfileFile, profileData);
    }

    // ── 프로필 전환 ───────────────────────────────────────────────────────────

    /// <summary>현재 프로필의 내용이 바뀌었다(다른 프로필로 전환 / 현재 프로필 삭제). UI 갱신용.</summary>
    public static event System.Action OnProfileChanged;

    /// <summary>프로필은 시작 화면에서만 바꿀 수 있다. 런 도중엔 세이브 경로가 바뀌면 안 된다.</summary>
    public static bool CanSwitchProfile => GameManager.Instance == null;

    /// <summary>
    /// 다른 프로필로 전환한다. 현재 프로필을 저장한 뒤 메모리 상태를 비우고 새 프로필을 읽는다.
    /// 비어 있는 프로필이면 새 프로필(기본값)로 시작한다.
    /// </summary>
    public bool SwitchProfile(int index)
    {
        if (!ProfileStore.IsValidIndex(index)) return false;
        if (!CanSwitchProfile)
        {
            Debug.LogWarning("[SaveManager] 게임 진행 중에는 프로필을 바꿀 수 없습니다.");
            return false;
        }
        if (index == ProfileStore.CurrentIndex) return true;

        SaveProfileData(); // 떠나는 프로필 마무리
        ProfileStore.SetCurrent(index);
        ReloadCurrentProfile();

        Debug.Log($"[SaveManager] 프로필 {index + 1}(으)로 전환했습니다.");
        return true;
    }

    /// <summary>
    /// 프로필을 지운다(진행·도감·회상·런 세이브 전부). 현재 프로필이면 빈 상태로 다시 시작한다.
    /// </summary>
    public bool DeleteProfile(int index)
    {
        if (!ProfileStore.IsValidIndex(index)) return false;
        if (!CanSwitchProfile)
        {
            Debug.LogWarning("[SaveManager] 게임 진행 중에는 프로필을 지울 수 없습니다.");
            return false;
        }

        bool isCurrent = index == ProfileStore.CurrentIndex;
        if (isCurrent) isProfileLoaded = false; // 지우는 사이 대기 중인 저장이 파일을 되살리지 않도록

        bool ok = ProfileStore.DeleteFiles(index);
        if (isCurrent) ReloadCurrentProfile();

        Debug.Log($"[SaveManager] 프로필 {index + 1} 삭제 {(ok ? "완료" : "실패")}");
        return ok;
    }

    private void ReloadCurrentProfile()
    {
        isProfileLoaded = false;
        saveRequested = false;
        playTimeSeconds = 0;

        ResetProfileState();
        LoadProfileData();
        if (isProfileLoaded) AchievementSystem.OnProfileLoaded();

        OnProfileChanged?.Invoke();
    }

    /// <summary>
    /// 프로필에 속한 메모리 상태를 새 프로필 상태로 되돌린다.
    /// 새 프로필 단위 상태를 추가하면 여기에도 초기화를 넣는다.
    /// </summary>
    private static void ResetProfileState()
    {
        if (AbilityManager.Instance != null) AbilityManager.Instance.ResetProfileToDefaults();
        UnlockManager.LoadFrom(null);
        DawnSystem.LoadFrom(null);
        DawnSystem.SetSelectedStage(0);
        MessengerSaveSystem.LoadFrom(null);
        AchievementSystem.LoadFrom(null);
        CodexProgress.Invalidate();                                   // 다음 접근 때 새 프로필의 codex.json을 읽는다
        if (SaveContext.Instance != null) SaveContext.Instance.ClearSlot(); // 이전 프로필 슬롯을 가리키지 않도록
    }

    /// <summary>옛 버전 프로필을 현재 형식으로 맞춘다. 버전을 올릴 때마다 단계를 추가한다.</summary>
    private static void MigrateProfile(ProfileData data)
    {
        // v0 → v1: 없음(v1이 첫 구조화 버전. 그 이전 형식은 LegacySaveMigration이 변환한다).
        if (data.version < 1) data.version = 1;
    }

    [ContextMenu("Debug: Unlock All Elements")]
    public void DebugUnlockAllElements()
    {
        // 0. 모든 식물 타입 및 일반 특성 강제 해금 (AbilityManager)
        if (AbilityManager.Instance != null)
        {
            foreach (System.Enum type in System.Enum.GetValues(typeof(PlayablePlantType)))
            {
                var plantType = (PlayablePlantType)type;
                if (!AbilityManager.Instance.IsPlantUnlocked.ContainsKey(plantType))
                    AbilityManager.Instance.IsPlantUnlocked.Add(plantType, true);
                else
                    AbilityManager.Instance.IsPlantUnlocked[plantType] = true;
            }

            foreach (var ability in AbilityManager.Instance.GetAllGeneralAbility())
            {
                if (ability != null && !string.IsNullOrEmpty(ability.abilityName))
                {
                    if (!AbilityManager.Instance.IsGeneralAbilityDataUnlocked.ContainsKey(ability.abilityName))
                        AbilityManager.Instance.IsGeneralAbilityDataUnlocked.Add(ability.abilityName, true);
                    else
                        AbilityManager.Instance.IsGeneralAbilityDataUnlocked[ability.abilityName] = true;
                }
            }
        }

        // 1. 모든 새벽 단계 해금 (모든 식물)
        // 간혹 로드 타이밍 문제로 StageCount가 0이 되는 것을 방지하여 최소 20단계까지 강제 보장
        int maxStage = Mathf.Max(20, DawnSystem.StageCount);
        foreach (var plant in DawnSystem.Plants)
        {
            DawnSystem.SetMaxUnlockedStage(plant, maxStage);
        }

        // 2. 상점 아이템 모두 해금
        var allItems = Resources.LoadAll<ItemData>("");
        foreach (var item in allItems)
        {
            if (!string.IsNullOrEmpty(item.UnlockId))
            {
                UnlockManager.Unlock(item.UnlockId);
            }
        }

        // 3. 특수 아이템 모두 해금
        var allSpecialItems = Resources.LoadAll<SpecialItemData>("");
        foreach (var spec in allSpecialItems)
        {
            if (!string.IsNullOrEmpty(spec.UnlockId))
            {
                UnlockManager.Unlock(spec.UnlockId);
            }
        }

        // 4. 인게임 사건 해금
        UnlockManager.Unlock(UnlockManager.Ids.GoldenPlantCreated);
        UnlockManager.Unlock(UnlockManager.Ids.WinterReached);
        UnlockManager.Unlock(UnlockManager.Ids.FertilizerFourColumns);

        // 변경 사항 저장
        SaveProfileData();

        Debug.Log("[SaveManager] 디버그: 모든 새벽 단계 및 아이템을 성공적으로 해금했습니다.");
    }

    [ContextMenu("Debug: Reset All Data")]
    public void DebugResetAllData()
    {
        // 아래 초기화가 저장 요청을 보내도 다시 쓰지 않도록 먼저 막는다.
        isProfileLoaded = false;

        // 1. 현재 프로필 파일(아이템 해금, 특성, 유전자, 새벽 진행 등) 삭제
        string path = ProfileStore.CurrentProfileFile;
        if (SaveIO.Exists(path))
        {
            SaveIO.Delete(path);
            Debug.Log($"[SaveManager] 프로필 데이터를 삭제했습니다: {path}");
        }

        // 2. 메모리 상의 해금 목록·새벽 진행 초기화
        UnlockManager.ResetAll();
        DawnSystem.ResetAllPlantProgress();

        // 3. PlayerPrefs(디버그 패널 등) 초기화
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[SaveManager] 디버그: 모든 게임 진행 데이터가 초기화되었습니다. 초기 화면으로 돌아갑니다.");

        // 4. 싱글톤 매니저들을 파괴하고 첫 씬(타이틀)으로 리로드하여 완벽하게 초기 상태로 재구축
        if (AbilityManager.Instance != null) Destroy(AbilityManager.Instance.gameObject);
        if (SoundManager.Instance != null) Destroy(SoundManager.Instance.gameObject);
        if (UIManager.Instance != null) Destroy(UIManager.Instance.gameObject);
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);

        // 첫 번째 씬(타이틀 화면)으로 리로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);

        // SaveManager 자신도 파괴 (씬 리로드 시 새로 생성되도록)
        Destroy(gameObject);
    }
}
