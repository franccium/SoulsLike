using Godot;

public partial class CombatComponent : Node
{
    public enum CombatStates
    {
        SwordSheathed,
        SwordDrawnOneHanded,
        SwordDrawnTwoHanded,
        ShieldDrawnOneHanded,
        ShieldDrawnTwoHanded,
        ShieldSheathed,
    }

    public CombatStates CombatState { get; set; } = CombatStates.SwordSheathed;
}