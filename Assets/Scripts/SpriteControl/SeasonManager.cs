using UnityEngine;
using DG.Tweening;


public class SeasonManager : MonoBehaviour
{
    [SerializeField] private Season currentSeason = Season.Spring;

    [SerializeField] private float changeDuration = 2f;
    [SerializeField] private Ease changeEase = Ease.InOutSine;

    [SerializeField] private Material grassMat;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            currentSeason = (Season)(((int)currentSeason + 1) % 4);
            ChangeToSeason(currentSeason);
        }

        Debug.Log(grassMat.GetFloat("_Dryness"));
    }


    private void ChangeToSeason(Season season)
    {
        switch(season)
        {
            case Season.Spring:
                DOTween.To(() => 1f, x => grassMat.SetFloat("_Dryness", x), 0f, changeDuration).SetEase(changeEase);
                grassMat.SetFloat("_Dryness", 0f);
                break;
            case Season.Summer:
                grassMat.SetFloat("_Dryness", 0f);
                break;
            case Season.Fall:
                DOTween.To(() => 0f, x => grassMat.SetFloat("_Dryness", x), 1f, changeDuration).SetEase(changeEase);
                grassMat.SetFloat("_Dryness", 1f);
                break;
            case Season.Winter:
                grassMat.SetFloat("_Dryness", 1f);
                break;
        }
    }
}
