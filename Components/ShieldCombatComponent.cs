using Godot;
using System;

public partial class ShieldCombatComponent : CombatComponent
{
    public AnimationTree AnimationTree { get; set; }
    public AnimationNodeStateMachinePlayback UpperBodyStateMachinePlayback { get; set; }
    public AnimationNodeStateMachinePlayback LocomotionStateMachinePlayback { get; set; }

    public Shield EquippedShield { get; set; }

    public Node3D RightHandContainer { get; set; }
    public Node3D LeftHandContainer { get; set; }
    public Node3D BackItemContainer { get; set; }

    public StringName DrawShieldStateName = "shield_draw";
    public StringName SheathShieldStateName = "shield_sheath";

    public StringName OneHandParryName { get; set; } = "shield_block_1";
    public StringName OneHandConstantBlockName { get; set; } = "shield_block_1_const";

    

    public override void _Ready()
    {
        CombatState = CombatStates.ShieldSheathed;
    }

    public override void _Process(double delta)
    {
    }

    public void ShieldAction(StringName actionName)
    {
        GD.Print("Shield Action");
        UpperBodyStateMachinePlayback.Travel(actionName);
        LocomotionStateMachinePlayback.Travel(actionName);

        //? lowerBodyPlayback.Travel(_oneHandAttackName);

        EquippedShield.Block();
    }

    public void FinishShieldAction()
    {
        GD.Print("Finish Shield Action");
        EquippedShield.FinishBlock();
    }

    public void OneHandParryShield()
    {
        GD.Print("One Hand Parry");
        ShieldAction(OneHandParryName);
    }

    public void OneHandConstBlockShield()
    {
        GD.Print("One Hand Const Block");
        ShieldAction(OneHandConstantBlockName);
    }

    /// <summary>
    /// changes ownership of the rightHipItemContainer to the leftHandContainer
    /// </summary>
    public void EquipShieldLeftHand()
    {
        GD.Print("Equip Shield Right Hand");
        BackItemContainer.SwitchNodeOwnership(LeftHandContainer);

        CombatState = CombatStates.ShieldDrawnOneHanded;
        UpperBodyStateMachinePlayback.Travel(DrawShieldStateName);

        //LocomotionStateMachinePlayback.Travel(CombatWalkStateName);

        DrawShield();
    }

    public void UnequipShieldLeftHand()
    {
        GD.Print("Unequip Shield Right Hand");
        LeftHandContainer.SwitchNodeOwnership(BackItemContainer);

        CombatState = CombatStates.ShieldSheathed;
        UpperBodyStateMachinePlayback.Travel(SheathShieldStateName);

        //LocomotionStateMachinePlayback.Travel(SheatShieldStateName);

        SheathShield();
    }

    public void DrawShield()
    {
        EquippedShield.DrawWeapon();
    }

    public void SheathShield()
    {
        EquippedShield.SheathWeapon();
    }
}
