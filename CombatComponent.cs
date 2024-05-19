using Godot;

public partial class CombatComponent : Node
{
    public enum CombatStates
    {
        SwordSheathed,
        SwordDrawnOneHanded,
        SwordDrawnTwoHanded,
    }

    public CombatStates CombatState { get; set; } = CombatStates.SwordSheathed;
}