/// <summary>
/// 무한 모드(테스트 기능). 설정에서 켜면 엔딩(40일) 뒤에도 계속할 수 있다.
/// 엔딩 뒤에는 세금이 빠르게 오르고(<see cref="TaxSchedule"/>), 5일을 더 버틸 때마다 새벽 단계를 하나 더 건너뛰어 연다.
/// </summary>
public static class EndlessMode
{
    /// <summary>엔딩 일차. 이 날 뒤부터가 무한 모드다.</summary>
    public const int StartStage = 40;
    /// <summary>새벽 단계를 하나 더 여는 간격(일).</summary>
    public const int UnlockInterval = 5;

    public static bool Enabled
    {
        get => DeviceSettings.Data.endlessModeEnabled;
        set
        {
            if (DeviceSettings.Data.endlessModeEnabled == value) return;
            DeviceSettings.Data.endlessModeEnabled = value;
            DeviceSettings.Save();
        }
    }

    /// <summary>엔딩 뒤 버틴 일수에 따라 추가로 건너뛰는 새벽 단계 수. 45일 → 1, 50일 → 2 …</summary>
    public static int ExtraUnlocks(int survivedStage)
        => survivedStage > StartStage ? (survivedStage - StartStage) / UnlockInterval : 0;

    /// <summary>엔딩 뒤 하루를 끝까지 버텼을 때. 5일째마다 새벽 단계를 건너뛰어 연다.</summary>
    public static void OnDaySurvived(int stage)
    {
        if (stage <= StartStage || (stage - StartStage) % UnlockInterval != 0) return;
        DawnSystem.RecordEndlessMilestone(ExtraUnlocks(stage));
    }
}
