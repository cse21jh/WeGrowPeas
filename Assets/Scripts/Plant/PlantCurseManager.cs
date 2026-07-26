using DG.Tweening;
using UnityEngine;

public class PlantCurseManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem mutant_plus;
    [SerializeField] private ParticleSystem mutant_minus;

    [SerializeField] private GameObject polenRoot;
    [SerializeField] private Color polenColor;
    [SerializeField] private Color normalColor;

    // 식물이 생성될 때는 변종 이펙트가 보이지 않아야 한다.
    // 파티클 오브젝트가 프리팹에서 활성 상태라 그냥 두면 방출되므로, 시작 시 끄고 재생할 때만 켠다.
    private void Awake()
    {
        if (mutant_plus != null) mutant_plus.gameObject.SetActive(false);
        if (mutant_minus != null) mutant_minus.gameObject.SetActive(false);
    }

    public void SetMutantPlusEffect(bool isActive)
    {
        SetEffect(mutant_plus, isActive);
    }

    public void SetMutantMinusEffect(bool isActive)
    {
        SetEffect(mutant_minus, isActive);
    }

    private static void SetEffect(ParticleSystem ps, bool isActive)
    {
        if (ps == null) return; // 아직 이펙트가 연결되지 않은 식물(예: 땅콩)

        if (isActive)
        {
            ps.gameObject.SetActive(true);
            ps.Play(true);
        }
        else
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
        }
    }

    public void SetPolenSpritesColor(bool isActive)
    {
        if (polenRoot == null) return; // 아직 이펙트가 연결되지 않은 식물(예: 땅콩)

        Color targetColor = isActive ? polenColor : normalColor;

        foreach(SpriteRenderer spriteRenderer in polenRoot.GetComponentsInChildren<SpriteRenderer>())
        {
            spriteRenderer.DOColor(targetColor, 0.5f);
        }
    }



}
