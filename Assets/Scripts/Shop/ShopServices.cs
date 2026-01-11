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

    void Awake()
    {
        //if (player == null)
        //    player = FindAnyObjectByType<PlayerManager>();
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
        //if (wave == null)
        //    wave = FindAnyObjectByType<WaveManager>();
        //if (bugs == null)
        //    bugs = FindAnyObjectByType<BugManager>();
        if (economy == null)
            economy = FindAnyObjectByType<EconomyManager>();
        if (placement == null)
            placement = FindAnyObjectByType<PlacementController>();
    }

    //public PlayerManager Player => player;
    public Grid Grid => grid;
    //public WaveManager Wave => wave;
    //public BugManager Bugs => bugs;
    public EconomyManager Economy => economy;
    public PlacementController Placement => placement;
}
