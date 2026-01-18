using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestWave : Wave
{
    public override WaveType WaveType => WaveType.Pest;
    public override string WaveName => "해충";
    public override string WaveDescription => "불길한 날개짓 소리가 들립니다......";
    public override string WaveSoundString => "Pest";
    public const int UnlockStage = 6;
    
    // 벌레가 실제로 등장하기 시작하는 스테이지 (Grid.SpawnRandomBug의 stage >= 6 조건)
    // 웨이브 해금(stage 4)과 실제 벌레 스폰(stage 6)은 다름
    public const int BugAppearStage = 6; // stage 6부터 실제 벌레가 스폰됨 (6웨이브)
}
