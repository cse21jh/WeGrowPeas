using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
        SoundManager.Instance.PlayBgm("StartScene");
        Time.timeScale = 1f;
    }

    public void LoadIntroScene()
    {
        SceneManager.LoadScene("IntroScene");
        SoundManager.Instance.PlayBgm("IntroScene");
        Time.timeScale = 1f;
    }

    public void LoadTutorialScene()
    {
        SceneManager.LoadScene("Tutorial");
    }

    public void LoadGardenScene()
    {
        SceneManager.LoadScene("Garden_GrassUpdate");
    }

    public void LoadGameOverScene()
    {
        SceneManager.LoadScene("GameOverScene");
    }


    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"{scene.name} 씬 로드됨 (모드: {mode})");
        StartCoroutine(Transition(1.0f));
    }

    private IEnumerator Transition(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        FindAnyObjectByType<TransitionController>().Transition_In();
    }
}
