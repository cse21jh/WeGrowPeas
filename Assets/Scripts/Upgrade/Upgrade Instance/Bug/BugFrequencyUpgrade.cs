using UnityEngine;

public class BugFrequencyUpgrade : Upgrade
{
    public override string Name => "벌레 등장 간격 증가";
    public override string Explanation => "벌레의 등장 간격이 10% 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_24");
    public override int MaxAmount => 3;
    public override int UnlockStage => 26;
    public override int UpgradeId => 21;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddBugSpawnIntervalIncreasement(0.1f);
    }
}
