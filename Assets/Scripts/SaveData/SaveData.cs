using System;
using System.Collections.Generic;

/// <summary>
/// 한 판(런)의 저장 데이터. 시스템별 하위 클래스로 나뉘어 있고,
/// 각 하위 클래스는 그 시스템이 직접 채우고 읽는다 (예: <c>Grid.SaveGrid/LoadGrid</c>).
///
/// 새 저장 항목을 추가할 때는
///   1) 해당 시스템의 하위 클래스에 필드를 넣고
///   2) 그 시스템의 SaveXxx / LoadXxx 한 쌍만 고치면 된다.
/// GameManager는 어떤 필드가 있는지 알 필요가 없다.
///
/// JsonUtility로 직렬화되므로 모든 하위 클래스는 [Serializable] + 기본 생성자가 있어야 한다.
///
/// 여기 담는 것은 "한 판" 단위 데이터뿐이다. 계정 단위로 유지되는 값(유전자, 해금,
/// 볼륨, 튜토리얼 시청 여부 등)은 <see cref="ProfileData"/>(SaveManager)가 따로 관리하고,
/// 도감/해금/새벽 진행은 각자 별도 파일(codex.json, unlocks.json)에 저장한다.
/// </summary>
[Serializable]
public class SaveData
{
    public ProgressSave progress = new();
    public GridSave grid = new();
    public WaveSave wave = new();
    public EconomySave economy = new();
    public ShopSave shop = new();
    public ModSave mod = new();
    public RequestSave request = new();
    public GraphSave graph = new();
    public PhoneSave phone = new();
    public AbilitySave ability = new();
    public CurseSave curse = new();
    public SpecialItemSave specialItem = new();
}

/// <summary>진행 상황 전반 (GameManager / TaxManager / DawnSystem / 시작 방식).</summary>
[Serializable]
public class ProgressSave
{
    public int stage;
    public bool seenFirstGold;
    public string currentPlant;

    /// <summary>마지막으로 세금을 납부한 스테이지.</summary>
    public int lastPaidTaxStage;

    /// <summary>이 런에서 선택한 새벽(승천) 단계. 이어하기에도 제약이 유지되도록 저장.</summary>
    public int selectedDawnStage;

    /// <summary>불러왔을 때의 시작 방식. 저장 시 이어하기 상태로 정규화된다.</summary>
    public GameStartType gst = GameStartType.None;
}

/// <summary>밭 상태 + 아이템/특성으로 누적된 각종 보정치 (Grid).</summary>
[Serializable]
public class GridSave
{
    // ── 밭 / 교배 ──────────────────────────────────────────────────────────
    public List<PlantData> plantList = new();
    public int maxCol;
    public float maxBreedTimer;
    public int maxBreedCount;
    public int additionalInheritance;

    public int totalBreedCount;
    public int totalPeaBreedcount;
    public int totalPeanutBreedCount;
    public int mostExpensivePlant;

    // ── 벌레 / 무당벌레 / 네펜데스 ─────────────────────────────────────────
    public int killBugCount;
    public float bugSpawnTimeInterval;
    public float lastBugSpawnTimeInterval;
    public float bugSpeedDecreasement;
    public float bugSpawnIntervalIncreasement;
    public int additionalBugGold;

    public float ladybugSpawnProbability;
    public int maxLadybugCount;
    public int additionalLadybugGoldPerUnit;
    public float additionalLadybugResistancePerUnit;

    public float nepenthesSpawnProbability;
    public int additionalNepenthesGold;
    public bool hasNepenthesPheromone;
    public float additionalNepenthesPheromoneSizeMultiplier;

    // ── 유전자 보너스 ──────────────────────────────────────────────────────
    public float weakGeneticsResistanceBonus;
    public float strongGeneticsResistanceBonus;
    public float goldenGeneticsProbabilityBonus;

    // ── 식물 일반 특성 ─────────────────────────────────────────────────────
    public float resistanceBonus;
    public int additionalPlantGold;
    public float additionalPlantGoldMultiplier;
    public float additionalPestResistance;

    // ── 완두콩 특성 ────────────────────────────────────────────────────────
    public float resistanceDecayReduction;
    public float resistanceAdaptation;

    // ── 땅콩 특성 ──────────────────────────────────────────────────────────
    public float additionalPeanutCopyProbability;
    public float bonusRatioWhenDie;

    // ── 일반 특성 (정보 표시) ──────────────────────────────────────────────
    public bool hasResistanceScouter;
    public bool hasGoldScouter;
    public bool hasWeatherForecast;

    // ── 페트병 ─────────────────────────────────────────────────────────────
    public List<int> perBottleTiles = new();
    public int petBottleInitialStockBonus;
    public int petBottlePriceReduction;
    public float petBottleSpawnProbability;
    /// <summary>페트병 보호 횟수 보너스 (전체).</summary>
    public int petBottleBlockCountBonus;

    // ── 고추 ───────────────────────────────────────────────────────────────
    public int chiliPepperRangeLevel;
    public float chiliPepperSpawnProbability;
    public float chiliPepperHealPercent;

    // ── 타일 / 비료 ────────────────────────────────────────────────────────
    public List<int> goldSoilTiles = new();
    public List<int> fertilizerColumns = new();
    public List<WaveType> fertilizerType = new();
    /// <summary>저항력 흡수 비료가 깔린 타일 인덱스.</summary>
    public List<int> absorbFertilizerTiles = new();

    // ── 신규 아이템 스탯 ───────────────────────────────────────────────────
    public int timeIsGoldLevel;
    public int badGuyMoreRiceLevel;
    public int sprinklerRangeBonus;
    public float sprinklerFertilizerSynergyBonus;

    // 신용카드·쌍둥이·완두커피·슈퍼 변종·활성형 껍질·왕위 계승·땅과 콩
    public float creditCardRefundPercent;
    public float twinBreedProbability;
    public float peaCoffeeMultiplier;
    public float superMutationChanceBonus;
    public bool hasSuperMutation;
    public float activeShellProbability;
    public float successionInheritRatio;
    public int landAndBeanLevel;

    /// <summary>특수 아이템(땅부자)의 세로줄별 골드 배수 (index = col).</summary>
    public List<float> columnGoldMulBonusList = new();
}

/// <summary>웨이브 / 계절 (EnemyController).</summary>
[Serializable]
public class WaveSave
{
    public Season currentSeason;
    public WaveType lastWaveType;
    public WaveType curWaveType;
    public WaveType nextWaveType;
    public int remainWaveSkipCount;
    public int[] waveKillCount = new int[8];

    public List<WaveType> stageWaveRecord = new();
    public List<int> stageKillRecord = new();
    public List<int> stageNoTraitRecord = new();
}

/// <summary>골드 / 판매 집계 (EconomyManager).</summary>
[Serializable]
public class EconomySave
{
    public int gold;
    /// <summary>[0] = 완두콩, [1] = 땅콩 판매 수.</summary>
    public int[] sellCount = new int[2];
    public int totalGold;
    public int consumeGold;
}

/// <summary>상점 구매 이력 / 시드 (ShopManager).</summary>
[Serializable]
public class ShopSave
{
    // itemName[i]의 구매 횟수가 itemPurchaseCount[i] (인덱스 매칭)
    public List<string> itemName = new();
    public List<int> itemPurchaseCount = new();

    // shopSeedDays[i]일의 시드가 shopSeeds[i] (인덱스 매칭)
    public List<int> shopSeedDays = new();
    public List<int> shopSeeds = new();

    /// <summary>게임별 고유 시드. 저장/불러오기 사이에 유지된다.</summary>
    public int gameUniqueShopSeed = -1;
}

/// <summary>보유 모드 (ModManager).</summary>
[Serializable]
public class ModSave
{
    public List<Mod> mods = new();
}

/// <summary>의뢰 진행 (RequestManager).</summary>
[Serializable]
public class RequestSave
{
    public int cycleEndRound;
    public int dayPassed;
    public List<RequestInstanceSaveData> activeRequests = new();
    public int completeRequestCount;
}

/// <summary>결과 그래프용 일자별 기록 (PlayerRecordForGraph).</summary>
[Serializable]
public class GraphSave
{
    public List<int> survivedPlants = new();
    public List<int> earnedGolds = new();
    public List<int> waveEachDay = new();
}

/// <summary>메신저 진행 (PhoneManager).</summary>
[Serializable]
public class PhoneSave
{
    // chatPartners[i]가 읽은 마지막 인덱스가 conversationSeenIndices[i] (인덱스 매칭)
    public List<string> chatPartners = new();
    public List<int> conversationSeenIndices = new();

    public List<string> activatedTriggers = new();

    // dayChatPartners[i]의 날짜 구분선이 dayByChatPartners[i] (인덱스 매칭)
    public List<string> dayChatPartners = new();
    public List<ChatDayData> dayByChatPartners = new();
}

/// <summary>보유 능력 / 유전자 저장고 (AbilityManager).</summary>
[Serializable]
public class AbilitySave
{
    public List<PlantAbilityData> currentPlantAbility = new();
    public List<GeneralAbilityData> currentGeneralAbility = new();
    public int geneStorage;
}

/// <summary>진행 중인 저주 (CurseManager).</summary>
[Serializable]
public class CurseSave
{
    /// <summary>[0] = 일시 저주 id, [1] = 계절 저주 id.</summary>
    public string[] curseId = new string[2];
    public int remainSeasonCurseDay;
    public int remainTempCurseDay;
}

/// <summary>특수 아이템 보유/선물 (SpecialItemSystem).</summary>
[Serializable]
public class SpecialItemSave
{
    public List<string> ownedSpecialItems = new();
    /// <summary>아직 수령하지 않은 선물 수. 수령 전까지 계속 유지된다.</summary>
    public int pendingSpecialGifts;
    /// <summary>선택지 칸별 남은 리롤 횟수.</summary>
    public List<int> specialItemRerolls = new();
}

/// <summary>밭에 심긴 식물 1개의 저장 형태.</summary>
[Serializable]
public class PlantData
{
    public string speciesname;
    public List<GeneticTrait> traits = new List<GeneticTrait>();
    public int gridIndex;
    public int taste;
    public int resistWaveCount;
    public int survivedTurns; // MoneyTree 생존 턴 수
    public float travelSellBonus; // 특수(세계여행) 누적 배수
    public int freeTimePassedCount; // 완두커피: 자유시간 경과 횟수
    public bool hasTriedBreed; // 활성형 껍질: 교배 시도 여부
    public bool isRooted; // 뿌리내림(이동 불가). 새벽 뿌리 효과 + 땅과 콩 가격 보너스 판정에 사용
}

/// <summary>메신저 대화 1개의 날짜 구분선 (index[i]번째 메시지가 day[i]일차).</summary>
[Serializable]
public class ChatDayData
{
    public List<int> index;
    public List<int> day;
}
