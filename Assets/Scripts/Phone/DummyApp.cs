using UnityEngine;
using TMPro;

public class DummyApp : BasePhoneApp
{
    [SerializeField] private string appTitle = "더미 앱";
    [SerializeField] private TMP_Text label;

    public override string Title => appTitle;

    public override void OnCreate(PhoneManager phone)
    {
        base.OnCreate(phone);

        if (label != null)
            label.text = $"{appTitle}\n(생성됨)";
    }

    public override void OnShow()
    {
        if (label != null)
            label.text = $"{appTitle}\n(OnShow 호출)";
    }

    public override void OnHide()
    {
        // 굳이 안 써도 되지만, 확인용으로 남겨둠
        Debug.Log($"[DummyApp] {appTitle} OnHide");
    }
}