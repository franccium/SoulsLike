using Godot;
using System;
using System.Collections.Generic;

public partial class SwordCombatComponent : CombatComponent
{
    public Node3D RightHandContainer { get; set; }
    public Node3D LeftHandContainer { get; set; }
    public Node3D RightHipContainer { get; set; }
    public Node3D LeftHipContainer { get; set; }
    public Node3D RightHipItemContainer { get; set; }
    public Node3D LeftHipItemContainer { get; set; }

    public static StringName DrawSwordRightHandName { get; set; } = "draw_sword_right_hand";
    public static StringName DrawSwordLeftHandName { get; set; } = "draw_sword_left_hand";
    public static StringName SheathWeaponStateName { get; set; } = "sword_sheath_2";

    public static StringName OneHandAttackName { get; set; } = "sword_and_shield_attack_2";
    public static StringName StrongOneHandAttackName { get; set; } = "sword_and_shield_slash_2";

    public static StringName AltOneHandAttackName { get; set; } = "sword_and_shield_attack_4";
    public static StringName StrongAltOneHandAttackName { get; set; } = "sword_and_shield_slash_3";

    public enum SwordAttacks
    {
        None,
        OneHandAttack,
        AltOneHandAttack,
        StrongOneHandAttack,
        StrongAltOneHandAttack
    }
    public SwordAttacks CurrentSwordAttack { get; set; }

    public override void _Ready()
    {
        CombatState = CombatStates.SwordSheathed;
    }

    public override void _Process(double delta)
    {
    }

    public void SwordAttack(StringName attackName)
    {
        GD.Print(Owner.GetType().Name + " used Sword Attack: " + attackName);
        UpperBodyStateMachinePlayback.Travel(attackName);
        LocomotionStateMachinePlayback.Travel(attackName);

        //? lowerBodyPlayback.Travel(_oneHandAttackName);

        EquippedWeapon.Attack();
    }

    private static readonly Dictionary<SwordAttacks, StringName> _attackDictionary = new Dictionary<SwordAttacks, StringName>
    {
        { SwordAttacks.OneHandAttack, OneHandAttackName },
        { SwordAttacks.AltOneHandAttack, AltOneHandAttackName },
        { SwordAttacks.StrongOneHandAttack, StrongOneHandAttackName },
        { SwordAttacks.StrongAltOneHandAttack, StrongAltOneHandAttackName }
    };

    public void SwordAttack(SwordAttacks attack)
    {
        if(attack == SwordAttacks.None)
            return;

        if(_attackDictionary.TryGetValue(attack, out StringName attackName))
        {
            SwordAttack(attackName);
        }
        else
        {
            GD.Print("Attack not found");
        }
    }

    public void FinishAttack()
    {
        EquippedWeapon.FinishAttack();
    }

    /// <summary>
    /// changes ownership of the rightHipItemContainer to the leftHandContainer
    /// </summary>
    public void EquipWeaponRightHand()
    {
        GD.Print("Equip Weapon Right Hand");
        EquippedWeapon.EquipWeapon();

        CombatState = CombatStates.SwordDrawnOneHanded;
        UpperBodyStateMachinePlayback.Travel(DrawSwordRightHandName);

        LocomotionStateMachinePlayback.Travel(CombatWalkStateName);
    }

    public void UnequipWeaponRightHand()
    {
        GD.Print("Unequip Weapon Right Hand");
        EquippedWeapon.UnequipWeapon();

        CombatState = CombatStates.SwordSheathed;
        UpperBodyStateMachinePlayback.Travel(SheathWeaponStateName);

        LocomotionStateMachinePlayback.Travel(SheathWeaponStateName);
    }
}
