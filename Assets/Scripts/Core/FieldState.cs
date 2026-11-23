/// <summary>
/// 밭 규칙이 참조하는 상태 묶음. 칸과 식물, 보정치, 땅 효과.
/// 게임에서는 <see cref="Grid"/>가 자기 것을 묶어 내주고, 시뮬레이터는 씬 없이 직접 만든다.
/// </summary>
public sealed class FieldState
{
    public FieldState(PlantBoard board, GridModifiers modifiers, SoilState soil)
    {
        Board = board;
        Modifiers = modifiers;
        Soil = soil;
    }

    /// <summary>씬 없이 빈 밭을 만든다.</summary>
    public static FieldState CreateEmpty()
    {
        var board = new PlantBoard();
        var modifiers = new GridModifiers();
        return new FieldState(board, modifiers, new SoilState(board, modifiers));
    }

    public PlantBoard Board { get; }
    public GridModifiers Modifiers { get; }
    public SoilState Soil { get; }
}
