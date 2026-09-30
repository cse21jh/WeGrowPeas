using System.Collections.Generic;

public static class MessengerSaveSystem
{
    private static HashSet<string> _readKeys = new HashSet<string>();

    // 기기 설정(프로필과 무관)
    public static bool PlayAlarmForSeenMessages
    {
        get => DeviceSettings.Data.playAlarmForSeenMessages;
        set => DeviceSettings.Data.playAlarmForSeenMessages = value;
    }

    public static void MarkAsRead(string partnerName, int index)
    {
        if (string.IsNullOrEmpty(partnerName)) return;
        bool changed = false;
        for (int i = 0; i <= index; i++)
        {
            string key = $"{partnerName}_{i}";
            changed |= _readKeys.Add(key);
        }
        if (changed) SaveManager.RequestProfileSave();
    }

    public static void MarkMessageAsRead(string partnerName, int index)
    {
        if (string.IsNullOrEmpty(partnerName) || index < 0) return;
        if (_readKeys.Add($"{partnerName}_{index}")) SaveManager.RequestProfileSave();
    }

    public static bool IsRead(string partnerName, int index)
    {
        if (string.IsNullOrEmpty(partnerName)) return false;
        string key = $"{partnerName}_{index}";
        return _readKeys.Contains(key);
    }

    public static int GetLastSeenIndex(string partnerName, int maxCount)
    {
        if (string.IsNullOrEmpty(partnerName)) return -1;
        int lastSeen = -1;
        for (int i = 0; i < maxCount; i++)
        {
            if (IsRead(partnerName, i))
            {
                lastSeen = i;
            }
        }
        return lastSeen;
    }

    /// <summary>프로필에 담는다. <see cref="LoadFrom"/>과 짝.</summary>
    public static void SaveTo(MessengerProfile profile)
    {
        profile.readKeys = new List<string>(_readKeys);
    }

    public static void LoadFrom(MessengerProfile profile)
    {
        _readKeys = profile?.readKeys != null ? new HashSet<string>(profile.readKeys) : new HashSet<string>();
    }

    public static void ResetAll()
    {
        _readKeys.Clear();
    }
}
