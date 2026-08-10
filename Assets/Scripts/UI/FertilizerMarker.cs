using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FertilizerMarker : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fertilizerMarker;
    [SerializeField] private List<FertilizerIcon> fertilizerIcons;

    private Dictionary<WaveType, Sprite> iconMap;

    private bool isOn = false;

    [System.Serializable]
    public struct FertilizerIcon
    {
        public WaveType waveType;
        public Sprite icon;
    }
    private void Awake()
    {
        iconMap = new Dictionary<WaveType, Sprite>();
        foreach (var fi in fertilizerIcons)
        {
            if (!iconMap.ContainsKey(fi.waveType))
                iconMap.Add(fi.waveType, fi.icon);
        }

        fertilizerMarker.enabled = false; // 기본은 숨김
    }

    public void SetFertilizer(WaveType wavetype)
    {
        fertilizerMarker.sprite = iconMap[wavetype];
        fertilizerMarker.enabled = true;
        isOn = true;
    }

    public void RemoveFertilizer()
    {
        fertilizerMarker.sprite = null;
        fertilizerMarker.enabled = false;
        isOn = false;
    }

    public bool IsOn => isOn;
}
