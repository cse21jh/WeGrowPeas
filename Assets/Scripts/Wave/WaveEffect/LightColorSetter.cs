using UnityEngine;

public class LightColorSetter : MonoBehaviour, ColorSetterInterface
{
    [SerializeField] Gradient gradient = null;

    public LightColorType type = LightColorType.None;

    private UnityEngine.Rendering.Universal.Light2D[] lights;


    public void Refresh()
    {
        lights = GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>();
    }

    public void SetColor(float time)
    {
        foreach (var light in lights)
        {
            light.color = gradient.Evaluate(time);
        }
    }
}
