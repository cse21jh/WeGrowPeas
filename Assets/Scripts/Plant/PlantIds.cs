/// <summary>
/// 식물 종 식별자. Plant.speciesname, GameManager.currentPlant, 세이브, 회상 기록에 그대로 저장된다.
/// 값이 한글이지만 표시에는 쓰지 않는다. 화면 이름은 번역 표(plant.csv)가 정하므로 표시 이름을 바꿔도 이 값은 그대로다.
/// 영문 ID로 옮기지 않은 이유: 런 세이브, 새벽 단계, 도감, 회상 기록의 값과 에셋의 plantName, plantId,
/// metaRequiredDawnPlant를 모두 함께 바꿔야 하는데 얻는 것은 값이 영문이 된다는 점뿐이다.
/// </summary>
public static class PlantIds
{
    public const string Pea = "완두콩";
    public const string Peanut = "땅콩";
    public const string ChiliPepper = "고추";
    public const string Nepenthes = "네펜데스";
    public const string Sprinkler = "스프링클러";
    public const string MoneyTree = "돈나무";
}
