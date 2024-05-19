using Godot;
using System;

public partial class SwordCombatComponent : CombatComponent
{
    public AnimationTree AnimationTree { get; set; }
    public AnimationNodeStateMachinePlayback UpperBodyStateMachinePlayback { get; set; }
    public AnimationNodeStateMachinePlayback LocomotionStateMachinePlayback { get; set; }

    public Sword EquippedSword { get; set; }

    public Node3D RightHandContainer { get; set; }
    public Node3D LeftHandContainer { get; set; }
    public Node3D RightHipContainer { get; set; }
    public Node3D LeftHipContainer { get; set; }
    public Node3D RightHipItemContainer { get; set; }
    public Node3D LeftHipItemContainer { get; set; }


    private StringName _combatLocomotionBlendPath = "parameters/LocomotionStateMachine/sword_and_shield_walk_and_strafe/blend_position";

    public StringName SwordAndShieldIdleName { get; set; } = "sword_and_shield_idle";

    public StringName DrawSwordRightHandName { get; set; } = "draw_sword_right_hand";
    public StringName DrawSwordLeftHandName { get; set; } = "draw_sword_left_hand";
    public StringName SheathWeaponStateName { get; set; } = "sword_sheath_2";

    public StringName OneHandAttackName { get; set; } = "sword_and_shield_attack_2";
    public StringName StrongOneHandAttackName { get; set; } = "sword_and_shield_slash_2";

    public StringName AltOneHandAttackName { get; set; } = "sword_and_shield_attack_4";
    public StringName StrongAltOneHandAttackName { get; set; } = "sword_and_shield_attack_5";

    public StringName CombatWalkStateName { get; set; } = "sword_and_shield_walk_and_strafe";
    public StringName CombatJumpStateName { get; set; } = "sword_and_shield_jump_1";

    public StringName CombatLocomotionBlendPath { get; set; } = "parameters/LocomotionStateMachine/sword_and_shield_walk_and_strafe/blend_position";


    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
    }

    public void OneHandAttackSword()
    {
        GD.Print("One Hand Attack");
        UpperBodyStateMachinePlayback.Travel(OneHandAttackName);

        //? lowerBodyPlayback.Travel(_oneHandAttackName);
    }

    public void OneHandAltAttackSword()
    {
        GD.Print("One Hand Alt Attack");
        UpperBodyStateMachinePlayback.Travel(AltOneHandAttackName);

        //? lowerBodyPlayback.Travel(_altOneHandAttackName);
    }

    public void StrongOneHandAttackSword()
    {
        GD.Print("Strong One Hand Attack");
        UpperBodyStateMachinePlayback.Travel(StrongOneHandAttackName);

        //? lowerBodyPlayback.Travel(_strongOneHandAttackName);
    }

    public void StrongAltOneHandAttackSword()
    {
        GD.Print("Strong Alt One Hand Attack");
        UpperBodyStateMachinePlayback.Travel(StrongAltOneHandAttackName);

        //? lowerBodyPlayback.Travel(_strongOneHandAttackName);
    }

    /// <summary>
    /// changes ownership of the rightHipItemContainer to the leftHandContainer
    /// </summary>
    public void EquipWeaponRightHand()
    {
        LeftHipItemContainer.SwitchNodeOwnership(RightHandContainer);

        CombatState = CombatStates.SwordDrawnOneHanded;
        UpperBodyStateMachinePlayback.Travel(DrawSwordRightHandName);

        LocomotionStateMachinePlayback.Travel(CombatWalkStateName);

        DrawWeapon();
    }

    public void UnequipWeaponRightHand()
    {
        LeftHipItemContainer.SwitchNodeOwnership(LeftHipContainer);

        CombatState = CombatStates.SwordSheathed;
        UpperBodyStateMachinePlayback.Travel(SheathWeaponStateName);

        LocomotionStateMachinePlayback.Travel(SheathWeaponStateName);

        SheathWeapon();
    }

    public void DrawWeapon()
    {
        EquippedSword.DrawWeapon();
    }

    public void SheathWeapon()
    {
        EquippedSword.SheathWeapon();
    }
}
