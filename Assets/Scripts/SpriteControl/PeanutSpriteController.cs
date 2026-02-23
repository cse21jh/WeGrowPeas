using System.Collections;
using UnityEngine;

public class PeanutSpriteController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Animator faceAnim;
    public Vector2 pairData_TraitFace = new Vector2(8, -1);
    [SerializeField] private int randFaceIndex = -1;
    [SerializeField] private float faceStartMaxDelay = 0.1f; // 얼굴 애니메이션 시작 지연 시간

    [SerializeField] private Sprite[] peanutSprites;
    [SerializeField] private SpriteRenderer accessoryRenderer;
    [SerializeField] private GameObject accessoryRenderer_SUB;

    [SerializeField] private GameObject WindEffect;
    [SerializeField] private GameObject SweatEffect;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Animator[] anims = GetComponentsInChildren<Animator>();
        for (int i = 0; i < anims.Length; i++)
        {
            if (anims[i].gameObject != this.gameObject)
            {
                faceAnim = anims[i];
                break;
            }
        }

        randFaceIndex = Random.Range(0, 9);
        pairData_TraitFace.y = randFaceIndex;

        if (faceAnim != null)
        {
            StartCoroutine(FaceStart());
        }
    }

    private IEnumerator FaceStart()
    {
        float rand = Random.Range(0f, faceStartMaxDelay);
        yield return new WaitForSeconds(rand);
        faceAnim.SetInteger("faceIndex", randFaceIndex);
        faceAnim.SetTrigger("Start");
    }


    public void SetPeanutSprite(int index)
    {
        pairData_TraitFace.x = index;

        if (spriteRenderer == null)
        {
            spriteRenderer = this.GetComponent<SpriteRenderer>();
        }



        switch (index)
        {
            case (int)TraitType.NaturalDeath:
                spriteRenderer.sprite = peanutSprites[1]; // 자연사 저항
                accessoryRenderer.sprite = null;
                break;
            case (int)TraitType.Wind:
                spriteRenderer.sprite = peanutSprites[0]; // 바람 저항
                accessoryRenderer.sprite = peanutSprites[2];
                break;
            case (int)TraitType.Flood:
                spriteRenderer.sprite = peanutSprites[0]; // 홍수 저항
                accessoryRenderer.sprite = peanutSprites[3];
                break;
            case (int)TraitType.Pest:
                spriteRenderer.sprite = peanutSprites[0]; // 해충 저항
                accessoryRenderer.sprite = peanutSprites[4];
                break;
            case (int)TraitType.Cold:
                spriteRenderer.sprite = peanutSprites[0]; // 추위 저항
                accessoryRenderer.sprite = peanutSprites[5];
                break;
            case (int)TraitType.HeavyRain:
                spriteRenderer.sprite = peanutSprites[0]; // 폭우 저항
                accessoryRenderer.sprite = peanutSprites[6];
                break;
            case (int)TraitType.Drought:
                spriteRenderer.sprite = peanutSprites[8]; // 가뭄 저항 유전자 2개 (폭우 0개) 
                accessoryRenderer.sprite = null;
                break;
            case (int)TraitType.Heat:
                spriteRenderer.sprite = peanutSprites[0]; // 더위 저항 유전자 2개 (추위 0개)
                accessoryRenderer.sprite = peanutSprites[7];
                //땀 이펙트
                SweatEffect.SetActive(true);
                break;
            case (int)TraitType.None + 1:
                spriteRenderer.sprite = peanutSprites[8]; // 폭우 가뭄 반반
                accessoryRenderer.sprite = peanutSprites[6];
                break;
            case (int)TraitType.None + 2:
                spriteRenderer.sprite = peanutSprites[0]; // 추위 더위 반반
                accessoryRenderer.sprite = peanutSprites[5];
                accessoryRenderer_SUB.SetActive(true);
                //땀 이펙트
                SweatEffect.SetActive(true);
                break;
            case (int)TraitType.None:
                spriteRenderer.sprite = peanutSprites[0]; // 기본
                accessoryRenderer.sprite = null;
                break;
            default:
                Debug.LogWarning("Invalid index for PeaSpriteController: " + index);
                break;
        }

        if (index == (int)TraitType.Wind)
        {
            WindEffect.SetActive(true);
        }
        else
        {
            WindEffect.SetActive(false);
        }


        if (index == (int)TraitType.Heat || index == (int)TraitType.CH)
        {
            SweatEffect.SetActive(true);
            Debug.Log("Sweat Effect Activated");
        }
        else
        {
            SweatEffect.SetActive(false);
        }
    }

}
