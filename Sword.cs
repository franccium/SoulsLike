using Godot;
using System;

public partial class Sword : Weapon
{
    protected AudioStream _hitOtherSwordSound;
    protected AudioStream _hitShieldArmorSound;
    protected AudioStream _hitBodySound;

    private MeshInstance3D _sheathMesh;
    private MeshInstance3D _sheathMesh2;

	public override void _Ready()
	{
        base._Ready();

        _hitOtherSwordSound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/warfare_swords_x_2_hit_scrape_001.mp3");
        _hitShieldArmorSound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/sword_strike_metal_shield_armour_clang_001_95364.mp3");
        _hitBodySound = ResourceLoader.Load<AudioStream>("res://Weapons/SoundEffects/warfare_sword_stab_into_body_flesh_light_squelch_93748.mp3");

        _sheathMesh = GetNode<MeshInstance3D>("Sheath1");
        _sheathMesh2 = GetNode<MeshInstance3D>("Sheath2");

        _weaponAttributesComponent = new WeaponAttributesComponent();
    }

	public override void _Process(double delta)
	{
	}

    public override void EquipWeapon()
    {
        base.EquipWeapon();
        _sheathMesh.Visible = false;
        _sheathMesh2.Visible = false;
    }

    public override void UnequipWeapon()
    {
        base.UnequipWeapon();
        _sheathMesh.Visible = true;
        _sheathMesh2.Visible = true;
    }

    public override void Attack()
    {
        HitboxArea.Monitoring = true;
    }

    public override void FinishAttack()
    {
        HitboxArea.Monitoring = false;
    }

    protected override void HitHuman(Human human)
    {
        human.TakeHealthDamage(_weaponAttributesComponent.Damage);
        human.TakeStaminaDamage(_weaponAttributesComponent.StaminaDamage);
        _audioStreamPlayer.Stream = _hitBodySound;
        _audioStreamPlayer.Play();
    }

    
    public override void OnHitboxAreaBodyEntered(Node3D body)
    {
        /*
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
        */
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
                _audioStreamPlayer.Stream = _hitOtherSwordSound;
                _audioStreamPlayer.Play();
                WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockedByOtherStaminaCost);
                sword.WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockingAttackStaminaCost);
            }
            else if(weaponHitboxArea.Weapon is Shield shield)
            {
                GD.Print("Sword hit shield");
                //_audioStreamPlayer.Stream = _hitSound;
                //_audioStreamPlayer.Play();
                WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockedByOtherStaminaCost);
                shield.WeaponOwner.TakeStaminaDamage(_weaponAttributesComponent.BlockingAttackStaminaCost);
                //shield.WeaponOwner.Stagger();
            }
        }
    }
}
