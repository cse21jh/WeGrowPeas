using System.Collections.Generic;

public static class MessengerSaveSystem
{
    private static HashSet<string> _readKeys = new HashSet<string>();

    public static bool PlayAlarmForSeenMessages { get; set; } = true;

    public static void MarkAsRead(string partnerName, int index)
    {
        if (string.IsNullOrEmpty(partnerName)) return;
        for (int i = 0; i <= index; i++)
        {
            string key = $"{partnerName}_{i}";
            _readKeys.Add(key);
        }
    }

    public static void MarkMessageAsRead(string partnerName, int index)
    {
        if (string.IsNullOrEmpty(partnerName) || index < 0) return;
        _readKeys.Add($"{partnerName}_{index}");
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

    public static List<string> GetReadKeys()
    {
        return new List<string>(_readKeys);
    }

    public static void SetReadKeys(List<string> keys)
    {
        if (keys == null)
        {
            _readKeys = new HashSet<string>();
            return;
        }
        _readKeys = new HashSet<string>(keys);
    }

    public static void ResetAll()
    {
        _readKeys.Clear();
    }
}
