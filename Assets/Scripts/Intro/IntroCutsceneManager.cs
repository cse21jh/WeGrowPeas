using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class IntroCutsceneManager : MonoBehaviour
{
    [SerializeField] private IntroCutsceneData data;
    [SerializeField] private Image topImage;
    [SerializeField] private TextMeshProUGUI bottomText;
    [SerializeField] private GameObject advanceIcon;

    [Header("Typing")]
    [SerializeField] private float charDelay = 0.06f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Empty text")]
    [SerializeField] private float emptyTextAutoAdvanceDelay = 1f;

    [Header("Completion")]
    [SerializeField] private string nextSceneName = "StartScene";

    private int _index;
    private bool _isTyping;
    private bool _waitingAdvance;
    private bool _isAutoAdvanceWaiting;
    private bool _isAdvancing;
    private Coroutine _typingCoroutine;
    private Coroutine _autoAdvanceCoroutine;
    private Coroutine _fadeOutAdvanceCoroutine;

    private void Start()
    {
        if (advanceIcon != null)
            advanceIcon.SetActive(false);

        if (data == null || data.Count == 0)
        {
            LoadNextScene();
            return;
        }
        _index = 0;
        ShowEntry(_index);
    }

    private void Update()
    {
        if (_isAutoAdvanceWaiting || _isAdvancing) return;
        if (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space)) return;

        if (_isTyping)
        {
            SkipTyping();
            return;
        }

        if (_waitingAdvance)
        {
            AdvanceToNext();
        }
    }

    private void ShowEntry(int index)
    {
        var entry = data.Get(index);
        if (entry == null)
        {
            LoadNextScene();
            return;
        }

        if (advanceIcon != null)
            advanceIcon.SetActive(false);

        bool hasImage = entry.image != null;
        bool hasText = !string.IsNullOrEmpty(entry.text);

        if (topImage != null)
        {
            if (hasImage)
            {
                topImage.sprite = entry.image;
                topImage.gameObject.SetActive(true);
                SetImageAlpha(topImage, 0f);
                topImage.DOKill();
                topImage.DOFade(1f, fadeInDuration).SetUpdate(true);
            }
            else
            {
                topImage.DOKill();
                topImage.gameObject.SetActive(false);
            }
        }

        if (bottomText != null)
            bottomText.text = "";

        _waitingAdvance = false;
        _isTyping = false;
        _isAutoAdvanceWaiting = false;

        if (hasText)
        {
            _isTyping = true;
            _typingCoroutine = StartCoroutine(TypeText(ToDisplayText(entry.text)));
        }
        else
        {
            _isAutoAdvanceWaiting = true;
            if (_autoAdvanceCoroutine != null)
                StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay(emptyTextAutoAdvanceDelay));
        }
    }

    private static void SetImageAlpha(Image img, float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    /// <summary> SO에 저장된 literal \n을 실제 줄바꿈으로 변환 </summary>
    private static string ToDisplayText(string raw)
    {
        return string.IsNullOrEmpty(raw) ? "" : raw.Replace("\\n", "\n");
    }

    private IEnumerator TypeText(string full)
    {
        for (int i = 0; i < full.Length && _isTyping; i++)
        {
            if (bottomText != null)
                bottomText.text = full.Substring(0, i + 1);
            yield return new WaitForSecondsRealtime(charDelay);
        }

        if (_isTyping)
        {
            if (bottomText != null)
                bottomText.text = full;
            _isTyping = false;
            _waitingAdvance = true;
            if (advanceIcon != null)
                advanceIcon.SetActive(true);
        }
        _typingCoroutine = null;
    }

    private IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _autoAdvanceCoroutine = null;
        _isAutoAdvanceWaiting = false;
        AdvanceToNext();
    }

    private void SkipTyping()
    {
        if (_typingCoroutine == null) return;

        StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
        _isTyping = false;

        var entry = data.Get(_index);
        if (entry != null && bottomText != null)
            bottomText.text = ToDisplayText(entry.text);

        _waitingAdvance = true;
        if (advanceIcon != null)
            advanceIcon.SetActive(true);
    }

    private void AdvanceToNext()
    {
        _waitingAdvance = false;
        if (advanceIcon != null)
            advanceIcon.SetActive(false);

        if (topImage != null && topImage.gameObject.activeSelf)
        {
            _isAdvancing = true;
            if (_fadeOutAdvanceCoroutine != null)
                StopCoroutine(_fadeOutAdvanceCoroutine);
            _fadeOutAdvanceCoroutine = StartCoroutine(FadeOutThenAdvance());
        }
        else
        {
            AdvanceImmediate();
        }
    }

    private IEnumerator FadeOutThenAdvance()
    {
        if (topImage != null)
        {
            topImage.DOKill();
            var tween = topImage.DOFade(0f, fadeOutDuration).SetUpdate(true);
            yield return tween.WaitForCompletion();
        }

        _fadeOutAdvanceCoroutine = null;
        _isAdvancing = false;
        AdvanceImmediate();
    }

    private void AdvanceImmediate()
    {
        _index++;

        if (_index >= data.Count)
        {
            LoadNextScene();
            return;
        }

        ShowEntry(_index);
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName)) return;
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDisable()
    {
        if (topImage != null)
            topImage.DOKill();
    }
}
