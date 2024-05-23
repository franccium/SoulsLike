using Godot;
using System;

public partial class Shield : Weapon
{
    protected AudioStream _blockSound;
    protected AudioStream _parrySound;
    protected AudioStream _hitSound;

    public override void _Ready()
    {
        base._Ready();
        _hitboxArea.Monitoring = true;
        //? make this hitbox area always monitor to make gampelay more "fun"

        _blockSound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/sword_strike_metal_shield_armour_clang_001_95364.mp3");
        _parrySound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/sword_strike_metal_shield_armour_clang_001_95364.mp3");
        _hitSound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/sword_strike_metal_shield_armour_clang_001_95364.mp3");

        _weaponAttributesComponent.BlockedByOtherStaminaCost = 10;
        _weaponAttributesComponent.BlockingAttackStaminaCost = 5;
    }

    public override void _Process(double delta)
    {
    }

    public override void DrawWeapon()
    {
    }

    public override void SheathWeapon()
    {
    }

    public override void Attack()
    {
        //_hitboxArea.Monitoring = true;
        //_isAttacking = true;
    }

    public override void FinishAttack()
    {
        //_hitboxArea.Monitoring = false;
        //_isAttacking = false;
    }

    public override void Block()
    {
        //_hitboxArea.Monitoring = true;
    }

    public override void FinishBlock()
    {
        //_hitboxArea.Monitoring = false;
    }

    protected override void HitHuman(Human human)
    {
        human.TakeHealthDamage(_weaponAttributesComponent.Damage);
        human.TakeStaminaDamage(_weaponAttributesComponent.StaminaDamage);
    }

    public override void OnHitboxAreaBodyEntered(Node3D body)
    {
        if(!_isAttacking) 
            return;

        if (_isOwnedByPlayer)
        {
            if (body is HumanEnemy enemy)
            {
                GD.Print("Shield hit enemy");
                HitHuman(enemy);
                _audioStreamPlayer.Stream = _hitSound;
                _audioStreamPlayer.Play();
            }
        }
        else
        {
            if (body is Player player)
            {
                GD.Print("Shield hit player");
                HitHuman(player);
                _audioStreamPlayer.Stream = _hitSound;
                _audioStreamPlayer.Play();
            }
        }
    }

    public override void OnHitboxAreaOtherAreaEntered(Area3D area)
    {
        if (area is WeaponHitboxArea weaponHitboxArea)
        {
            if(weaponHitboxArea.Weapon.WeaponOwner == this.WeaponOwner)
                return;

            if (weaponHitboxArea.Weapon is Sword sword)
            {
                GD.Print("Shield hit sword");
                _audioStreamPlayer.Stream = _blockSound;
                _audioStreamPlayer.Play();
                WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockedByOtherStaminaCost);
                sword.WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockingAttackStaminaCost);
            }
            else if (_isAttacking && weaponHitboxArea.Weapon is Shield shield)
            {
                GD.Print("Shield hit shield");
                _audioStreamPlayer.Stream = _hitSound;
                _audioStreamPlayer.Play();
                WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockedByOtherStaminaCost);
                shield.WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockingAttackStaminaCost);
                shield.WeaponOwner.Stagger();
            }
        }
    }
}
