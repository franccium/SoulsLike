using Godot;
using System;

public partial class Weapon : Node3D
{
    public Human WeaponOwner { get; set; }
    protected WeaponAttributesComponent _weaponAttributesComponent;

    protected WeaponHitboxArea _hitboxArea;

    protected bool _isOwnedByPlayer = false;
    protected bool _isAttacking = false;

    protected AudioStreamPlayer _audioStreamPlayer;

    public override void _Ready()
    {
        _hitboxArea = (WeaponHitboxArea)GetNode<Area3D>("WeaponHitboxArea");
        _hitboxArea.Weapon = this;

        _hitboxArea.BodyEntered += OnHitboxAreaBodyEntered;
        _hitboxArea.AreaEntered += OnHitboxAreaOtherAreaEntered;
        _hitboxArea.Monitoring = false;

        _audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");

        _weaponAttributesComponent = new WeaponAttributesComponent();
    }

    public override void _Process(double delta)
    {
    }

    public virtual void SetOwner(Human owner)
    {
        WeaponOwner = owner;
        if (owner is Player)
            _isOwnedByPlayer = true;
    }

    public virtual WeaponAttributesComponent GetStats()
    {
        return _weaponAttributesComponent;
    }

    public virtual void DrawWeapon()
    {
    }

    public virtual void SheathWeapon()
    {
    }

    public virtual void Attack()
    {
        //_hitboxArea.Monitoring = true;
        //_isAttacking = true;
    }

    public virtual void FinishAttack()
    {
        //_hitboxArea.Monitoring = false;
        //_isAttacking = false;
    }

    public virtual void Block()
    {
        //_hitboxArea.Monitoring = true;
    }

    public virtual void FinishBlock()
    {
        //_hitboxArea.Monitoring = false;
    }

    protected virtual void HitHuman(Human human)
    {
        human.TakeHealthDamage(_weaponAttributesComponent.Damage);
        human.TakeStaminaDamage(_weaponAttributesComponent.StaminaDamage);
    }

    public virtual void OnHitboxAreaBodyEntered(Node3D body)
    {
        if(body == WeaponOwner)
            return;

        GD.Print("Weapon hit " + body.Name);
    }

    public virtual void OnHitboxAreaOtherAreaEntered(Area3D area)
    {
        if(area is WeaponHitboxArea weaponHitboxArea)
        {
            if (weaponHitboxArea.Weapon.WeaponOwner == this.WeaponOwner)
                return;

            GD.Print("Weapon hit weapon " + weaponHitboxArea.Owner.Name);
        }
    }
}
