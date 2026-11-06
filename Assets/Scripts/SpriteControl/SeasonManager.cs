using UnityEngine;
using DG.Tweening;


public class SeasonManager : MonoBehaviour
{
    [SerializeField] private Season previousSeason = Season.Spring;
    [SerializeField] private Season currentSeason = Season.Spring;

    [SerializeField] private float changeDuration = 2f;
    [SerializeField] private Ease changeEase = Ease.InOutSine;

    [SerializeField] private Material grassMat;
    [SerializeField] private Material flowerMat;
    [SerializeField] private Material mapleMat;
    [SerializeField] private Material[] snowMats;

    [SerializeField] private GameObject snowEffect;


    private void Start()
    {
        InitSettings();
        ChangeToSeason(currentSeason);
    }


    private void InitSettings()
    {
        currentSeason = previousSeason;
        grassMat.SetFloat(ShaderProps.Dryness, 0f);
        flowerMat.SetFloat(ShaderProps.DissolveAmount, 1f);
        mapleMat.SetFloat(ShaderProps.DissolveAmount, 1f);
        foreach (Material snowMat in snowMats)
            snowMat.SetFloat(ShaderProps.MeltStrength, 1.2f);
        snowEffect.SetActive(false);
    }


    private void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.V))
        {
            currentSeason = (Season)(((int)currentSeason + 1) % 4);
            ChangeToSeason(currentSeason);
        }
        */

        //Debug.Log(grassMat.GetFloat(ShaderProps.Dryness));
    }


    public void ChangeToSeason(Season season)
    {
        if(season == currentSeason)
            return;

        switch (season)
        {
            case Season.Spring:
                snowEffect.SetActive(false);
                foreach (Material snowMat in snowMats)
                    ChangeMaterialValue(snowMat, ShaderProps.MeltStrength, 0.6f, 1.2f);       // 눈 쌓이기
                ChangeMaterialValue(grassMat, ShaderProps.Dryness, 1f, 0f);
                grassMat.SetFloat(ShaderProps.Dryness, 0f);
                ChangeMaterialValue(flowerMat, ShaderProps.DissolveAmount, 1f, 0f);   // 꽃 나타나기
                break;
            case Season.Summer:
                grassMat.SetFloat(ShaderProps.Dryness, 0f);
                ChangeMaterialValue(flowerMat, ShaderProps.DissolveAmount, 0f, 1f);   // 꽃 없애기
                break;
            case Season.Fall:
                ChangeMaterialValue(grassMat, ShaderProps.Dryness, 0f, 1f);
                ChangeMaterialValue(mapleMat, ShaderProps.DissolveAmount, 1f, 0f);   // 단풍잎 나타나기
                grassMat.SetFloat(ShaderProps.Dryness, 1f);
                break;
            case Season.Winter:
                grassMat.SetFloat(ShaderProps.Dryness, 1f);
                ChangeMaterialValue(mapleMat, ShaderProps.DissolveAmount, 0f, 1f);   // 단풍잎 없애기
                foreach (Material snowMat in snowMats)
                    ChangeMaterialValue(snowMat, ShaderProps.MeltStrength, 1.2f, 0.6f);       // 눈 쌓이기
                snowEffect.SetActive(true);
                break;
        }

        previousSeason = currentSeason;
        currentSeason = season;
    }

    private void ChangeMaterialValue(Material mat, int property, float fromValue, float toValue)
    {
        DOTween.To(() => fromValue, x => mat.SetFloat(property, x), toValue, changeDuration).SetEase(changeEase);
    }
}
