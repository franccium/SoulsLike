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

    protected Node3D _weaponContainer;
    protected Node3D _sheathedContainer;
    protected Node3D _equippedContainer;

    public override void _Ready()
    {
        _hitboxArea = (WeaponHitboxArea)GetNode<Area3D>("WeaponHitboxArea");
        _hitboxArea.Weapon = this;

        _hitboxArea.BodyEntered += OnHitboxAreaBodyEntered;
        _hitboxArea.AreaEntered += OnHitboxAreaOtherAreaEntered;
        _hitboxArea.Monitoring = false;

        _audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        _audioStreamPlayer.VolumeDb = -10;

        _weaponAttributesComponent = new WeaponAttributesComponent();
    }

    public override void _Process(double delta)
    {
    }

    public virtual void SetProperties(Human owner, Node3D weaponContainer, Node3D sheathedContainer, Node3D equippedContainer)
    {
        SetOwner(owner);
        _weaponContainer = weaponContainer;
        _sheathedContainer = sheathedContainer;
        _equippedContainer = equippedContainer;
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

    /// <summary>
    /// changes ownership of the rightHipItemContainer to the leftHandContainer
    /// </summary>
    public virtual void EquipWeapon()
    {
        GD.Print("Equip Weapon");
        _weaponContainer.SwitchNodeOwnership(_equippedContainer);
    }

    public virtual void UnequipWeapon()
    {
        GD.Print("Unequip Weapon");
        _weaponContainer.SwitchNodeOwnership(_sheathedContainer);
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
