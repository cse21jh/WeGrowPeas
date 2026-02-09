using UnityEngine;

[CreateAssetMenu(fileName = "WeatherForecastAbility)", menuName = "Abilities/General/WeatherForecastAbility")]
public class WeatherForecastAbility : GeneralAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.SetWeatherForecast(true);
    }
}
