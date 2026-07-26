using DG.Tweening;
using UnityEngine;

public class PlantCurseManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem mutant_plus;
    [SerializeField] private ParticleSystem mutant_minus;

    [SerializeField] private GameObject polenRoot;
    [SerializeField] private Color polenColor;
    [SerializeField] private Color normalColor;

    public void SetMutantPlusEffect(bool isActive)
    {
        if (isActive)
        {
            mutant_plus.Play();
        }
        else
        {
            mutant_plus.Stop();
        }
    }

    public void SetMutantMinusEffect(bool isActive)
    {
        if (isActive)
        {
            mutant_minus.Play();
        }
        else
        {
            mutant_minus.Stop();
        }
    }

    public void SetPolenSpritesColor(bool isActive)
    {
        Color targetColor = isActive ? polenColor : normalColor;

        foreach(SpriteRenderer spriteRenderer in polenRoot.GetComponentsInChildren<SpriteRenderer>())
        {
            spriteRenderer.DOColor(targetColor, 0.5f);
        }
    }



}
