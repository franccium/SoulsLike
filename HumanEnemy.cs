using Godot;
using System;

public partial class HumanEnemy : Human
{
    private Player _player;

    public override void _Ready()
    {
        base._Ready();

        _player = GameController.Instance.GetPlayer();
    }

    public override void _Process(double delta)
    {
        _current2DDirection = new Vector2(_movementDirection.X, _movementDirection.Z);
        
        if(_queuedAttack)
        {
            InitialiseBlock();
            //InitialiseAttack();
            return;
        }
        if(_playerSeen)
            ScanForAttackInRange();
        else 
            ScanForPlayerSeen();

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        UpdateGroundedStateMovement(ref velocity, (float)delta);

        // follow player
        if(_playerSeen)
            _movementDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);
        else 
            _movementDirection = Vector3.Zero;
        base.UpdateMovementInDirection(ref velocity, (float)delta);

        Velocity = velocity;
        MoveAndSlide();
    }

    #region COMBAT

    protected float _detectionRange = 150f;
    protected bool _playerSeen = false;
    protected bool _inCombat = false;

    protected float _attackRange = 5f;
    protected bool _inAttackRange = false;
    protected bool _queuedAttack = false;

    protected override void GatherCombatRequirements()
    {
        base.GatherCombatRequirements();

        _swordCombatComponent.EquippedSword.SetOwner(this);
        _shieldCombatComponent.EquippedShield.SetOwner(this);

        _attackRange = _swordCombatComponent.EquippedSword.GetStats().AttackRange;
    }

    protected virtual void ScanForPlayerSeen()
    {
        if (GlobalPosition.DistanceTo(_player.GlobalPosition) < _detectionRange)
        {
            _swordCombatComponent.EquipWeaponRightHand();
            _shieldCombatComponent.EquipShieldLeftHand();

            _playerSeen = true;
            _inCombat = true;
        }
        else
        {
            _swordCombatComponent.UnequipWeaponRightHand();
            _shieldCombatComponent.UnequipShieldLeftHand();

            _playerSeen = false;
            _inCombat = false;
        }
    }

    protected virtual void ScanForAttackInRange()
    {
        if (GlobalPosition.DistanceTo(_player.GlobalPosition) < _attackRange)
        {
            _inAttackRange = true;
            _queuedAttack = true;
        }
        else 
        {
            _inAttackRange = false;
        }
    }

    protected virtual void InitialiseAttack()
    {
        _queuedAttack = false;

        _swordCombatComponent.OneHandAttackSword();
    }

    protected virtual void InitialiseBlock()
    {
        _queuedAttack = false;

        if (CurrentAnimationState == AnimationStates.Idle)
        {
            BlockConst();
            _shieldCombatComponent.OneHandConstBlockShield();
        }
    }

    #endregion
}
