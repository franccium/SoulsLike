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
            InitialiseAttack();
            return;
        }

        ScanForAttackInRange();

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        UpdateGroundedStateMovement(ref velocity, (float)delta);

        // follow player
        _movementDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);
        if (_movementDirection != Vector3.Zero)
        {
            velocity.X = _movementDirection.X * MovementSpeed;
            velocity.Z = _movementDirection.Z * MovementSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, MovementSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MovementSpeed);
        }

        if (Velocity.Length() > 0.1f)
        {
            RotationDegrees = new Vector3(
                RotationDegrees.X,
                Mathf.Floor(Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(RotationDegrees.Y), Mathf.Atan2(-Velocity.X, -Velocity.Z), (float)delta * RotationSpeed))),
                RotationDegrees.Z
            );
        }

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

        _attackRange = _swordCombatComponent.EquippedSword.GetStats().AttackRange;
    }

    protected virtual void ScanForPlayerSeen()
    {
        if (GlobalPosition.DistanceTo(_player.GlobalPosition) < _detectionRange)
        {
            _playerSeen = true;
            _inCombat = true;

            _swordCombatComponent.EquipWeaponRightHand();
        }
        else
        {
            _playerSeen = false;
            _inCombat = false;

            _swordCombatComponent.UnequipWeaponRightHand();
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

    #endregion
}
