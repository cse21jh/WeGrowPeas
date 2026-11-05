/// <summary>
/// 식물 종 식별자. Plant.speciesname, GameManager.currentPlant, 세이브, 회상 기록에 그대로 저장된다.
/// 값을 바꾸면 기존 세이브가 깨지므로 표시 이름을 바꿀 때는 번역 표(plant.csv)를 고친다.
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
