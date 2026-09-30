using System;
using System.Collections.Generic;

/// <summary>
/// 계정(프로필) 단위로 유지되는 진행. 런이 끝나거나 게임오버가 나도 남는다.
/// <see cref="SaveData"/>(한 판)와 같은 방식으로 시스템별 하위 클래스로 나뉘고,
/// 각 하위 클래스는 그 시스템이 직접 채우고 읽는다 (예: <c>UnlockManager.SaveTo/LoadFrom</c>).
///
/// 새 항목을 추가할 때는
///   1) 해당 시스템의 하위 클래스에 필드를 넣고
///   2) 그 시스템의 SaveTo / LoadFrom 한 쌍만 고치면 된다.
///
/// 여기 없는 것: 기기 설정(볼륨 등)은 <see cref="DeviceSettings"/>, 도감은 codex.json,
/// 회상은 Recall 폴더에 따로 저장한다(도감·회상은 자주·크게 쓰여서 분리).
/// </summary>
[Serializable]
public class ProfileData
{
    /// <summary>저장 형식 버전. (<see cref="SaveManager.ProfileVersion"/>)</summary>
    public int version;

    public ProfileMeta meta = new();
    public AbilityProfile ability = new();
    public UnlockProfile unlock = new();
    public DawnProfile dawn = new();
    public MessengerProfile messenger = new();
}

/// <summary>프로필 선택 화면에 보여줄 정보.</summary>
[Serializable]
public class ProfileMeta
{
    /// <summary>마지막으로 저장한 시각(UTC, 유닉스 초).</summary>
    public long lastSavedUnix;
}

/// <summary>특성 화면의 해금·포인트와 유전자 (AbilityManager).</summary>
[Serializable]
public class AbilityProfile
{
    public int genetics;
    public int generalAbilityPoint;
    public List<PlantProgress> plants = new();
    public List<NamedFlag> generalAbilities = new();
}

[Serializable]
public class PlantProgress
{
    public PlayablePlantType type;
    public bool unlocked;
    public int abilityPoint;
}

[Serializable]
public class NamedFlag
{
    public string name;
    public bool value;
}

[Serializable]
public class NamedInt
{
    public string name;
    public int value;
}

/// <summary>상점/특수 아이템 해금 id (UnlockManager).</summary>
[Serializable]
public class UnlockProfile
{
    public List<string> ids = new();
}

/// <summary>식물별로 해금된 최대 새벽 단계 (DawnSystem).</summary>
[Serializable]
public class DawnProfile
{
    public List<NamedInt> maxUnlockedStage = new();
}

/// <summary>읽은 메신저 메시지 (MessengerSaveSystem).</summary>
[Serializable]
public class MessengerProfile
{
    public List<string> readKeys = new();
}
