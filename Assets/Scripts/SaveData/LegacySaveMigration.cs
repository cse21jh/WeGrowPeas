using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 프로필 도입 전 세이브를 첫 번째 프로필(Profile_0)과 기기 설정으로 한 번 옮긴다.
///
/// 옛 위치:
///   Assets(=dataPath)/ProfileData.json   계정 진행 + 볼륨 등 설정 (평평한 구조)
///   PlayerPrefs "Dawn_MaxUnlockedStage_*"  새벽 진행
///   persistentDataPath/codex.json          도감
///   dataPath/../Recall/                    회상
///   dataPath/UserData_0~2.json             런 세이브 슬롯
///
/// 원본은 지우지 않고 복사만 한다(문제가 생기면 되돌릴 수 있도록).
/// 다 옮기면 profiles.json에 표시해 다시 실행하지 않는다.
/// </summary>
public static class LegacySaveMigration
{
    private const int TargetProfile = 0;

    private static string LegacyProfilePath => Path.Combine(Application.dataPath, "ProfileData.json");
    private static string LegacyCodexPath => Path.Combine(Application.persistentDataPath, "codex.json");

    public static void RunIfNeeded()
    {
        if (ProfileStore.Global.legacyMigrated) return;

        try
        {
            string dir = ProfileStore.DirOf(TargetProfile);
            Directory.CreateDirectory(dir);

            MigrateProfileAndSettings();
            CopyFileIfMissing(LegacyCodexPath, Path.Combine(dir, "codex.json"));
            CopyDirectoryIfMissing(RecallStore.LegacyRootPath, Path.Combine(dir, "Recall"));
            for (int i = 0; i < SaveContext.SlotCount; i++)
            {
                string name = SaveContext.SlotFileName(i);
                CopyFileIfMissing(Path.Combine(Application.dataPath, name), Path.Combine(dir, name));
            }
        }
        catch (Exception e)
        {
            // 표시를 남기지 않으면 다음 실행에서 다시 시도한다. 이미 옮긴 파일은 건너뛴다.
            Debug.LogError($"[Migration] 옛 세이브 이전 실패. 다음 실행에서 다시 시도합니다.\n{e}");
            return;
        }

        ProfileStore.Global.legacyMigrated = true;
        ProfileStore.Global.lastProfileIndex = TargetProfile;
        ProfileStore.SaveGlobal();
    }

    private static void MigrateProfileAndSettings()
    {
        string target = ProfileStore.ProfileFileOf(TargetProfile);
        if (SaveIO.Exists(target)) return;

        var profile = new ProfileData { version = SaveManager.ProfileVersion };
        bool hasData = false;

        if (SaveIO.TryRead(LegacyProfilePath, out LegacyProfileData old))
        {
            ConvertProfile(old, profile);
            profile.meta.lastSavedUnix = new DateTimeOffset(File.GetLastWriteTimeUtc(LegacyProfilePath)).ToUnixTimeSeconds();
            hasData = true;

            DeviceSettings.SeedIfMissing(new SettingsData
            {
                bgmVolume = old.BGMVolume,
                effectVolume = old.EffectVolume,
                showBreedPopup = old.showBreedPopupSetting,
                playAlarmForSeenMessages = old.playAlarmForSeenMessages,
            });
        }
        else if (SaveIO.Exists(LegacyProfilePath))
        {
            Debug.LogError($"[Migration] 옛 프로필을 읽지 못해 옮기지 못했습니다. 원본은 그대로 남아 있습니다: {LegacyProfilePath}");
        }

        profile.dawn = DawnSystem.ReadLegacyPlayerPrefs();
        if (profile.dawn.maxUnlockedStage.Count > 0) hasData = true;

        if (!hasData) return; // 옮길 게 없으면 빈 프로필 파일을 만들지 않는다(선택 화면에서 "비어있음")

        if (!SaveIO.Write(target, profile))
            throw new IOException($"프로필 쓰기 실패: {target}");

        Debug.Log($"[Migration] 옛 프로필을 프로필 {TargetProfile + 1}로 옮겼습니다.");
    }

    private static void ConvertProfile(LegacyProfileData old, ProfileData profile)
    {
        var a = profile.ability;
        a.genetics = old.genetics;
        a.generalAbilityPoint = old.generalAbilityPoint;

        var points = new Dictionary<PlayablePlantType, int>();
        for (int i = 0; i < old.plantTypeOfAbilityPoint.Count && i < old.plantAbilityPoint.Count; i++)
            points[old.plantTypeOfAbilityPoint[i]] = old.plantAbilityPoint[i];

        for (int i = 0; i < old.unlockPlantType.Count && i < old.isPlantUnlocked.Count; i++)
        {
            var type = old.unlockPlantType[i];
            points.TryGetValue(type, out int point);
            a.plants.Add(new PlantProgress { type = type, unlocked = old.isPlantUnlocked[i], abilityPoint = point });
        }

        for (int i = 0; i < old.generalAbilityDataName.Count && i < old.isGeneralAbilityDataUnlocked.Count; i++)
            a.generalAbilities.Add(new NamedFlag { name = old.generalAbilityDataName[i], value = old.isGeneralAbilityDataUnlocked[i] });

        if (old.unlockedItems != null) profile.unlock.ids = new List<string>(old.unlockedItems);
        if (old.readMessengerKeys != null) profile.messenger.readKeys = new List<string>(old.readMessengerKeys);
    }

    private static void CopyFileIfMissing(string src, string dst)
    {
        if (!File.Exists(src) || File.Exists(dst)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(dst));
        File.Copy(src, dst);
    }

    private static void CopyDirectoryIfMissing(string src, string dst)
    {
        if (!Directory.Exists(src)) return;
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src))
            CopyFileIfMissing(file, Path.Combine(dst, Path.GetFileName(file)));
    }

    /// <summary>프로필 도입 전 ProfileData.json 형식. 읽기 전용.</summary>
    [Serializable]
    private class LegacyProfileData
    {
        public int genetics;

        public List<PlayablePlantType> unlockPlantType = new();
        public List<bool> isPlantUnlocked = new();

        public List<PlayablePlantType> plantTypeOfAbilityPoint = new();
        public List<int> plantAbilityPoint = new();

        public List<string> generalAbilityDataName = new();
        public List<bool> isGeneralAbilityDataUnlocked = new();

        public int generalAbilityPoint;

        public float BGMVolume = 0.05f;
        public float EffectVolume = 0.3f;
        public bool showBreedPopupSetting;

        public List<string> readMessengerKeys = new();
        public List<string> unlockedItems = new();
        public bool playAlarmForSeenMessages = true;
    }
}
