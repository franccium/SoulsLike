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

    public Weapon EquippedWeapon { get; set; }

    /// <summary>
    /// Set the weapon for the combat component along with the weapon's properties
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="weapon"></param>
    /// <param name="itemContainer">constant parent container for the weapon</param>
    /// <param name="sheathedContainer"></param>
    /// <param name="equippedContainer"></param> <summary>
    public void SetWeapon(Human owner, Weapon weapon, Node3D itemContainer, Node3D sheathedContainer, Node3D equippedContainer)
    {
        EquippedWeapon = weapon;
        weapon.SetProperties(owner, itemContainer, sheathedContainer, equippedContainer);
    }

    public AnimationTree AnimationTree { get; set; }
    public AnimationNodeStateMachinePlayback UpperBodyStateMachinePlayback { get; set; }
    public AnimationNodeStateMachinePlayback LocomotionStateMachinePlayback { get; set; }
    public static StringName CombatUpperBodyIdleStateName { get; set; } = "sword_and_shield_idle";
    public static StringName CombatWalkStateName { get; set; } = "sword_and_shield_walk_and_strafe";
    public static StringName CombatJumpStateName { get; set; } = "sword_and_shield_jump_1";
    public static StringName CombatLocomotionBlendPath { get; set; } = "parameters/LocomotionStateMachine/sword_and_shield_walk_and_strafe/blend_position";
}