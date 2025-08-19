using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FenceElementController : MonoBehaviour
{
    [Header("완두콩 모습 관련")]
    [SerializeField] private Animator faceAnim;
    [SerializeField] private float faceMaxDelay = 0.5f;
    [SerializeField] private GameObject prop;
    Image faceImage;
    [SerializeField] private Sprite defaultPeaBase;
    [SerializeField] private Sprite normalPeaBase;

    [Space(10)]
    [Header("UI 요소들")]
    [SerializeField] private TextMeshProUGUI elementName;
    [SerializeField] private string defaultName;
    [SerializeField] private TextMeshProUGUI surviveProbability;
    [SerializeField] private Image[] dnaImages;
    [SerializeField] private Sprite[] dnaSprites;
    [SerializeField] private Image star;

    private void Start()
    {
        faceImage = GetComponent<Image>();
        if (faceAnim != null)
        {
            StartCoroutine(FaceStart());
        }
    }

    public void SetElement(bool isActive, string name = "", float surviveProb = 0f, int dna = 0, bool isStarActive = false)
    {
        if (!isActive)      // If the element is not active, hide all UI components
        {
            faceImage.sprite = normalPeaBase; // Reset face image to normal pea base
            if (prop != null)
                prop.SetActive(false);
            surviveProbability.gameObject.SetActive(false);
            elementName.text = name;
            dnaImages[0].gameObject.SetActive(false);
            dnaImages[1].gameObject.SetActive(false);
            star.gameObject.SetActive(false);
        }
        else                // If the element is active, set the UI components accordingly
        {
            faceImage.sprite = defaultPeaBase; // Reset face image to default
            if (prop != null)
                prop.SetActive(true);

            surviveProbability.gameObject.SetActive(true);
            surviveProbability.text = (surviveProb * 100f).ToString("F0") + "%";

            elementName.text = defaultName;

            dnaImages[0].gameObject.SetActive(true);
            dnaImages[1].gameObject.SetActive(true);
            switch (dna)
            {
                case 0:
                    dnaImages[0].sprite = dnaSprites[0];
                    dnaImages[1].sprite = dnaSprites[0];
                    break;
                case 1:
                    dnaImages[0].sprite = dnaSprites[1];
                    dnaImages[1].sprite = dnaSprites[0];
                    break;
                case 2:
                    dnaImages[0].sprite = dnaSprites[1];
                    dnaImages[1].sprite = dnaSprites[1];
                    break;
            }

            star.gameObject.SetActive(isStarActive);
        }
    }

    private IEnumerator FaceStart()
    {
        float delay = Random.Range(0f, faceMaxDelay);
        yield return new WaitForSeconds(delay);
        faceAnim.SetTrigger("Start");
    }
}
