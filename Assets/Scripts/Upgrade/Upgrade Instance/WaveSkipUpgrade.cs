using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSkipUpgrade : Upgrade
{
    public override string Name => "¿þÀÌºê ½ºÅµ È½¼ö Áõ°¡";
    public override string Explanation => "´ÙÀ½ ¿þÀÌºê ½ºÅµ °¡´É È½¼ö +1";
    public override int MaxAmount => -1;
    public override void OnSelectAction()
    {
        GameManager.Instance.enemyController.SetNextWave();
        Debug.Log(Explanation);
    }
}