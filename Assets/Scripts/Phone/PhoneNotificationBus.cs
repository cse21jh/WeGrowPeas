using System;

public static class PhoneNotificationBus
{
    // 알림 열기
    public static Action<PhoneNotificationData> OnShow;

    // 알림 강제 닫기 (선택)
    public static Action OnHide;
}

[Serializable]
public class PhoneNotificationData
{
    public string title;
    public string message;
    public float duration; // 0이면 수동 닫기
}
