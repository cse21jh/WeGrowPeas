using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.Rendering;

public class ShopServices : MonoBehaviour
{
    //[SerializeField] private PlayerManager player;
    [SerializeField] private Grid grid;
    //[SerializeField] private WaveManager wave;
    //[SerializeField] private BugManager bugs;
    [SerializeField] private EconomyManager economy;
    [SerializeField] private PlacementController placement;

    //public PlayerManager Player => player;
    public Grid Grid => grid;
    //public WaveManager Wave => wave;
    //public BugManager Bugs => bugs;
    public EconomyManager Economy => economy;
    public PlacementController Placement => placement;
}
