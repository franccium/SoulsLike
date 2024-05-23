using Godot;
using System;

public partial class Sword : Weapon
{
    protected AudioStream _blockSound;
    protected AudioStream _hitSound;

    private MeshInstance3D _sheathMesh;
    private MeshInstance3D _sheathMesh2;

	public override void _Ready()
	{
        base._Ready();

        _blockSound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/warfare_swords_x_2_hit_scrape_001.mp3");
        _hitSound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/sword_strike_metal_shield_armour_clang_001_95364.mp3");

        _sheathMesh = GetNode<MeshInstance3D>("Sheath1");
        _sheathMesh2 = GetNode<MeshInstance3D>("Sheath2");

        _weaponAttributesComponent = new WeaponAttributesComponent();
    }

	public override void _Process(double delta)
	{
	}

    public override void DrawWeapon()
    {
        _sheathMesh.Visible = false;
        _sheathMesh2.Visible = false;
    }

    public override void SheathWeapon()
    {
        _sheathMesh.Visible = true;
        _sheathMesh2.Visible = true;
    }

    public override void Attack()
    {
        _hitboxArea.Monitoring = true;
    }

    public override void FinishAttack()
    {
        _hitboxArea.Monitoring = false;
    }

    protected override void HitHuman(Human human)
    {
        human.TakeHealthDamage(_weaponAttributesComponent.Damage);
        human.TakeStaminaDamage(_weaponAttributesComponent.StaminaDamage);
    }

    public override void OnHitboxAreaBodyEntered(Node3D body)
    {
        if(_isOwnedByPlayer)
        {
            if(body is HumanEnemy enemy)
            {
                GD.Print("Sword hit enemy");
                HitHuman(enemy);
            }
        }
        else
        {
            if(body is Player player)
            {
                GD.Print("Sword hit player");
                HitHuman(player);
            }
        }
    }

    public override void OnHitboxAreaOtherAreaEntered(Area3D area)
    {
        if(area is WeaponHitboxArea weaponHitboxArea)
        {
            if (weaponHitboxArea.Weapon.WeaponOwner == this.WeaponOwner)
                return;

            if (weaponHitboxArea.Weapon is Sword sword)
            {
                GD.Print("Sword hit sword");
                _audioStreamPlayer.Stream = _blockSound;
                _audioStreamPlayer.Play();
                WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockedByOtherStaminaCost);
                sword.WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockingAttackStaminaCost);
            }
            else if(weaponHitboxArea.Weapon is Shield shield)
            {
                GD.Print("Sword hit shield");
                _audioStreamPlayer.Stream = _hitSound;
                _audioStreamPlayer.Play();
                WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockedByOtherStaminaCost);
                shield.WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockingAttackStaminaCost);
                shield.WeaponOwner.Stagger();
            }
        }
    }
}
