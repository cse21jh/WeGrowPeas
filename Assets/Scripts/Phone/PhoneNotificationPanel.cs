using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PhoneNotificationPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("Dismiss")]
    [SerializeField] private bool dismissOnClick = true;

    private readonly Queue<PhoneNotificationData> _queue = new();
    private Coroutine _runner;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnEnable()
    {
        PhoneNotificationBus.OnShow += Enqueue;
        PhoneNotificationBus.OnHide += ClearAndHide;
    }

    private void OnDisable()
    {
        PhoneNotificationBus.OnShow -= Enqueue;
        PhoneNotificationBus.OnHide -= ClearAndHide;
    }

    // UI Button OnClick에 연결할 함수
    public void OnClickDismiss()
    {
        if (!dismissOnClick) return;
        HideImmediate();
    }

    private void Enqueue(PhoneNotificationData data)
    {
        if (data == null) return;

        _queue.Enqueue(data);

        if (_runner == null)
            _runner = StartCoroutine(RunQueue());
    }

    private IEnumerator RunQueue()
    {
        while (_queue.Count > 0)
        {
            var data = _queue.Dequeue();
            ShowImmediate(data);

            float duration = Mathf.Max(0f, data.duration);

            if (duration > 0f)
            {
                // 자동 닫힘 + 클릭 닫힘 둘 다 허용
                float t = 0f;
                while (t < duration && root != null && root.activeSelf)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // duration=0이면 클릭(또는 다른 코드)로만 닫힘
                while (root != null && root.activeSelf)
                    yield return null;
            }

            HideImmediate();
            yield return new WaitForSeconds(0.1f);
        }

        _runner = null;
    }

    private void ShowImmediate(PhoneNotificationData data)
    {
        if (titleText != null) titleText.text = data.title;
        if (messageText != null) messageText.text = data.message;
        if (root != null) root.SetActive(true);
    }

    private void HideImmediate()
    {
        if (root != null) root.SetActive(false);
    }

    private void ClearAndHide()
    {
        _queue.Clear();
        HideImmediate();

        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }
}
