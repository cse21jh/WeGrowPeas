using UnityEngine;

public class MushroomCurse : CurseInstance
{
    public MushroomCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("버섯 발생 실행");
        //How-To
        //Activate 함수를 구현할 때, 본 스크립트 내에 구현 로직을 모두 작성하지 말고
        //메인 타깃이 되는 Manager나 Grid 내에 구현 후 본 스크립트에서는 호출만 해 주세요
        //ex: Grid.ActivateMushroom
    }

    public override void Deactivate()
    {
        Debug.Log("버섯 발생 끝");
        //How-To
        //턴 종료 후 특별히 해제되어야 하는 저주일 경우 Activate와 같은 방식으로 호출
        //ex: Grid.ClearMushroom
        //Deactivate가 필요하지 않는 저주도 있습니다. (ex: 도둑이야! 등)
    }
}
