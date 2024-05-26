using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class HumanEnemy : Human
{
    private Player _player;

    public override void _Ready()
    {
        base._Ready();

        _player = GameController.Instance.GetPlayer();

        InitialiseAI();
    }

    public override void _Process(double delta)
    {
        _current2DDirection = new Vector2(_movementDirection.X, _movementDirection.Z);

        if (_playerSeen)
        {
            TakeAction();
        }
        else
            ScanForPlayerSeen();

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        UpdateGroundedStateMovement(ref velocity, (float)delta);

        // follow player
        if (_shouldMoveUpToPlayer)
            MoveInPlayersDirection();
        else if (_currentAIState == AIStates.Run)
            Run();
        else if (_currentAIState == AIStates.MoveAround)
            MoveAround();
        else if (_currentAIState == AIStates.Strafe)
            Strafe();
        else
            Patrol();

        base.UpdateMovementInDirection(ref velocity, (float)delta);

        Velocity = velocity;
        MoveAndSlide();
    }

    protected virtual float GetDistanceToPlayer()
    {
        return GlobalPosition.DistanceTo(_player.GlobalPosition);
    }

    #region COMBAT

    protected float _detectionRange = 150f;
    protected bool _playerSeen = false;
    protected bool _inCombat = false;

    protected float _attackRange = 3f;
    protected bool _inAttackRange = false;

    protected float _actionRange = 10f;
    protected bool _shouldMoveUpToPlayer = false;

    protected override void GatherCombatRequirements()
    {
        base.GatherCombatRequirements();

        _attackRange = _swordCombatComponent.EquippedWeapon.GetStats().AttackRange;
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
        // not used now
        if (GetPlayerInAttackRange())
        {
            _inAttackRange = true;
        }
        else
        {
            _inAttackRange = false;
        }
    }

    protected bool GetPlayerInAttackRange()
    {
        return GlobalPosition.DistanceTo(_player.GlobalPosition) < _attackRange;
    }

    #region MOVEMENT ACTIONS

    /// <summary>
    /// wait until seeing the player
    /// </summary>
    protected virtual void Patrol()
    {
        _movementDirection = Vector3.Zero;
    }

    /// <summary>
    /// move straight to the player
    /// </summary>
    protected virtual void MoveInPlayersDirection()
    {
        //GD.Print("Moving to player");
        _movementDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);
    }

    /// <summary>
    /// run from the player
    /// </summary>
    protected virtual void Run()
    {
        // run
        //*GD.Print("Running");
        _movementDirection = -GlobalPosition.DirectionTo(_player.GlobalPosition);

        if (GetDistanceToPlayer() > _detectionRange + 5f)
            _currentAIState = AIStates.Patrol;
    }

    /// <summary>
    /// move around the player, try to get at his back
    /// </summary>
    protected virtual void MoveAround()
    {
        //*GD.Print("Moving around player");

        Vector3 directionToPlayer = GlobalPosition.DirectionTo(_player.GlobalPosition);
        _movementDirection = directionToPlayer.Rotated(Vector3.Up, Mathf.Pi / 2);

        float angleToPlayer = directionToPlayer.AngleTo(_movementDirection);
        if (Mathf.Abs(angleToPlayer - Mathf.Pi) < 0.1f)
        {
            _movementDirection = Vector3.Zero;
            // somehow reset to get out of this state if its necessary, but the calculation of exact 180 degrees is not realistic ever
        }
    }

    protected float _strafeDir = 1;
    /// <summary>
    /// move in an arc in front of the player
    /// </summary> <summary>
    protected virtual void Strafe()
    {
        //*GD.Print("Strafing");
        _movementDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);

        Vector3 directionToPlayer = GlobalPosition.DirectionTo(_player.GlobalPosition);
        float dirRotation = Mathf.Pi / 2 * _strafeDir;
        _movementDirection = directionToPlayer.Rotated(Vector3.Up, dirRotation);

        float angleToPlayer = directionToPlayer.AngleTo(_movementDirection);
        if (Mathf.Abs(angleToPlayer - Mathf.Pi / 4) < 10f)
        {
            _movementDirection = Vector3.Zero;
            _strafeDir = -_strafeDir;
            // somehow reset to get out of this state if its necessary
        }
    }

    #endregion

    #region ON ANIMATION FINISHED

    protected override void OnAnimationFinished(StringName animName)
    {
        GD.Print("animation finished: " + animName);

        switch (CurrentAnimationState)
        {
            case AnimationStates.Attacking:
                _swordCombatComponent.FinishAttack();
                break;
            case AnimationStates.DodgeRoll:
                OnDodgeRollFinished();
                break;
            case AnimationStates.Parrying:
                _shieldCombatComponent.FinishShieldAction();
                break;
        }

        _isTakingAction = false;

        CurrentAnimationState = AnimationStates.Idle;
    }

    #endregion

    #region INITIALISING ACTIONS

    protected virtual Vector3 GetDirectionToPlayer()
    {
        return GlobalPosition.DirectionTo(_player.GlobalPosition);
    }

    protected virtual Vector3 GetDirectionPerpendicularToPlayer(float sideDirection)
    {
        Vector3 directionToPlayer = GlobalPosition.DirectionTo(_player.GlobalPosition);
        return directionToPlayer.Rotated(Vector3.Up, Mathf.Pi / 2 * sideDirection);
    }

    protected virtual void QueueRandomAttack()
    {
        _queuedAttack = (SwordCombatComponent.SwordAttacks)new Random().Next(4) + 1;
    }

    protected virtual void InitialiseAttack()
    {
        GD.Print("Enemy trying to attack");

        QueueRandomAttack();
        Attack(GetDirectionToPlayer());
        _currentAIState = AIStates.Attack;
    }

    protected virtual void InitialiseBlock()
    {
        GD.Print("Enemy trying to block");

        if (CurrentAnimationState == AnimationStates.Idle)
        {
            BlockConst();
            _currentAIState = AIStates.Block;
        }
    }

    protected virtual void InitialiseParry()
    {
        GD.Print("Enemy trying to parry");

        Parry(GetDirectionToPlayer());
        _currentAIState = AIStates.Parry;
    }

    protected virtual void InitialiseDodgeRoll()
    {
        GD.Print("Enemy trying to dodge roll");

        Vector3 rollDirection;
        if (_defensiveLevel > _aggresiveLevel)
        {
            // roll away from the player
            rollDirection = -GetDirectionToPlayer();
        }
        else
        {
            // roll to the side of the player
            float sideDirection = new Random().Next(2) * 2 - 1;
            rollDirection = GetDirectionPerpendicularToPlayer(sideDirection);
        }

        DodgeRoll(rollDirection);
        _currentAIState = AIStates.DodgeRoll;
    }

    protected virtual void InitialiseStrafe()
    {
        GD.Print("Enemy trying to strafe");
        _currentAIState = AIStates.Strafe;
    }

    protected virtual void InitialiseMoveUp()
    {
        GD.Print("Enemy trying to move up");

        _shouldMoveUpToPlayer = true;
        _currentAIState = AIStates.MoveUp;
    }

    protected virtual void InitialiseRun()
    {
        GD.Print("Enemy trying to run");

        _currentAIState = AIStates.Run;
    }

    protected virtual void InitialiseMoveAround()
    {
        GD.Print("Enemy trying to move around the player");

        _currentAIState = AIStates.MoveAround;
    }

    protected virtual void InitialiseCounterAttack()
    {
        GD.Print("Enemy trying to counter attack");

        _swordCombatComponent.SwordAttack(SwordCombatComponent.SwordAttacks.StrongOneHandAttack);
        _currentAIState = AIStates.CounterAttack;
    }

    protected virtual void InitialiseTryCritical()
    {
        GD.Print("Enemy trying to critical");

        _currentAIState = AIStates.TryCritical;
    }

    protected virtual void InitialiseTryHeal()
    {
        GD.Print("Enemy trying to heal");

        _currentAIState = AIStates.TryHeal;
    }

    protected virtual void InitialiseTryRecover()
    {
        GD.Print("Enemy trying to recover");

        _currentAIState = AIStates.TryRecover;
    }

    #endregion

    #endregion

    #region AI

    protected enum AIStates
    {
        Patrol,
        Attack,
        Block,
        Parry,
        DodgeRoll,
        MoveUp,
        Strafe,
        Run,
        MoveAround,
        CounterAttack,
        TryCritical,
        TryHeal,
        TryRecover
    }

    protected AIStates _currentAIState = AIStates.Patrol;

    protected bool _isTakingAction = false;

    protected float _passiveLevel = 0.5f;
    protected float _aggresiveLevel = 0.5f;
    protected float _defensiveLevel = 0.5f;

    protected float _attackWeight = 0.5f;
    protected float _blockWeight = 0.5f;
    protected float _parryWeight = 0.5f;
    protected float _dodgeRollWeight = 0.5f;
    protected float _moveUpWeight = 0.5f;
    protected float _strafeWeight = 0.5f;
    protected float _runWeight = 0.5f;
    protected float _moveAroundWeight = 0.5f;
    protected float _counterAttackWeight = 0.5f;

    protected float _tryCriticalWeight = 0.5f;

    protected float _tryHealWeight = 0.5f;
    protected float _tryRecoverWeight = 0.5f;

    #region AI WEIGHTS INITIALISATION

    protected virtual void InitialiseAI()
    {
        ZeroOutWeights();
        InitialiseWeightedActions();
        InitialisePlayerSignals();
    }

    protected void ZeroOutWeights()
    {
        _attackWeight = 0f;
        _blockWeight = 0f;
        _parryWeight = 0f;
        _dodgeRollWeight = 0f;
        _moveUpWeight = 0f;
        _strafeWeight = 0f;
        _runWeight = 0f;
        _moveAroundWeight = 0f;
        _counterAttackWeight = 0f;

        _tryCriticalWeight = 0f;

        _tryHealWeight = 0f;
        _tryRecoverWeight = 0f;
    }

    protected float GetSumOfWeights()
    {
        return _weightedActions.Sum(x => x.weight());
    }

    private List<(Func<float> weight, Action action)> _weightedActions;

    protected virtual void InitialiseWeightedActions()
    {
        _weightedActions = new List<(Func<float> weight, Action action)>
        {
            (() => _attackWeight, () => InitialiseAttack()),
            (() => _blockWeight, () => InitialiseBlock()),
            (() => _parryWeight, () => InitialiseParry()),
            (() => _dodgeRollWeight, () => InitialiseDodgeRoll()),
            (() => _moveUpWeight, () => InitialiseMoveUp()),
            (() => _strafeWeight, () => InitialiseStrafe()),
            (() => _runWeight, () => InitialiseRun()),
            (() => _moveAroundWeight, () => InitialiseMoveAround()),
            (() => _counterAttackWeight, () => InitialiseCounterAttack()),
            (() => _tryCriticalWeight, () => InitialiseTryCritical()),
            (() => _tryHealWeight, () => InitialiseTryHeal()),
            (() => _tryRecoverWeight, () => InitialiseTryRecover())
        };
    }

    #endregion

    #region BEHAVIOUR LEVELS

    protected virtual void UpdateBehaviourLevels()
    {
        // when low on hp, defensive level increases
        // when low on stamina, aggresive level increases
        // or it should be inverted idk yet
    }

    #endregion

    #region MOVEMENT WEIGHTS

    protected virtual void UpdateMovementWeigths()
    {
        // when player tries to go away, move to him
        if (GetDistanceToPlayer() > _attackRange)
        {
            _moveUpWeight = 2.5f;
            //_dodgeRollWeight = 0.3f; // make sure its a roll in direction of the player
        }
    }

    #endregion

    #region RECOVERING WEIGHTS

    protected const float RECOVERING_MIN_PERCENTAGE = 0.65f;

    protected virtual void UpdateRecoveringWeights()
    {
        //todo not gonna use it yet
        /*
        if (_attributeComponent.GetHealthPercentage() > RECOVERING_MIN_PERCENTAGE)
            _tryHealWeight = 0f;
        else
            _tryHealWeight = _defensiveLevel * 1 / _attributeComponent.GetHealthPercentage();

        if (_attributeComponent.GetStaminaPercentage() > RECOVERING_MIN_PERCENTAGE)
            _tryRecoverWeight = 0f;
        else
            _tryRecoverWeight = _aggresiveLevel * 1 / _attributeComponent.GetStaminaPercentage();
        */
    }

    #endregion

    #region TAKE ACTION

    protected virtual void TakeAction()
    {
        //GD.Print("anim state: " + CurrentAnimationState);
        if (CurrentAnimationState != AnimationStates.Idle && CurrentAnimationState != AnimationStates.BlockingConst)
            return;

        //*GD.Print("distance to player: " + GetDistanceToPlayer() + " action range: " + _actionRange, " attack range: " + _attackRange);

        if (GetDistanceToPlayer() > _actionRange) //todo make him acually move up to his attack range with actions, kinda weird now
        {
            _shouldMoveUpToPlayer = true;
            return;
        }
        else
        {
            _shouldMoveUpToPlayer = false;
            if (_currentAIState == AIStates.MoveUp) // in order to not get stuck with taking action of moving, as its not an animation
                _isTakingAction = false;
        }

        //?if(_isTakingAction) not sure yet, maybe, but after reseting takingaction for shouldmoveuptoplayer like it is now, and in case of things like strafe and stuff id need a way to reset without relying on animation finished
        //?return;
        //!!!! StopBlockConst();

        UpdateBehaviourLevels();
        UpdateMovementWeigths();
        UpdateRecoveringWeights();

        foreach (var (weight, action) in _weightedActions)
        {
            //*if (weight() > 0f)
            //*GD.Print(weight() + " : " + action.Method.Name);
        }

        TakeRandomWeightedAction();

        ZeroOutWeights();
    }

    #endregion

    #region RANDOM ACTION

    protected virtual void TakeRandomWeightedAction()
    {
        float totalWeight = GetSumOfWeights();

        float randomWeight = (float)GD.RandRange(0, totalWeight) + 0.1f;

        foreach (var (weight, action) in _weightedActions)
        {
            randomWeight -= weight();
            if (randomWeight <= 0f)
            {
                action();
                break;
            }
        }

        _isTakingAction = true;
    }

    #endregion

    #region PLAYER SIGNALS

    public virtual void InitialisePlayerSignals()
    {
        _player.PlayerAttack += () => PlayerAttackWeightsAdjustment();
        _player.PlayerBlock += () => PlayerBlockWeightsAdjustment();
        _player.PlayerDodgeRoll += () => PlayerDodgeRollWeightsAdjustment();
        _player.PlayerStaggered += () => PlayerStaggeredWeightsAdjustment();
    }

    public virtual void PlayerAttackWeightsAdjustment()
    {
        ZeroOutWeights();

        if (!GetPlayerInAttackRange())
        {
            UpdateMovementWeigths();
            return;
        }

        _blockWeight = 0.6f + _defensiveLevel;
        _dodgeRollWeight = 0.4f + _defensiveLevel;
        _attackWeight = 0.3f + _aggresiveLevel;
        _parryWeight = 0.2f + _aggresiveLevel;

        GD.Print("Player attacked, weights adjusted");

        //? take action?
    }

    public virtual void PlayerBlockWeightsAdjustment()
    {
        ZeroOutWeights();

        if (!GetPlayerInAttackRange())
        {
            UpdateMovementWeigths();
            return;
        }

        _moveAroundWeight = 0.5f + _aggresiveLevel;
        _attackWeight = 0.3f + _aggresiveLevel;
        _strafeWeight = 0.2f + _defensiveLevel;

        GD.Print("Player blocked, weights adjusted");
        //? take action?

        //todo after just attacking and the player blocks, dodge roll away from the player
    }

    public virtual void PlayerDodgeRollWeightsAdjustment()
    {
        ZeroOutWeights();

        _moveUpWeight = 0.7f + _passiveLevel;
        _dodgeRollWeight = 0.3f + _aggresiveLevel;

        GD.Print("Player dodge rolled, weights adjusted");
        //? take action?

        //todo try a ranged attack?
    }

    public virtual void PlayerStaggeredWeightsAdjustment()
    {
        ZeroOutWeights();

        if (!GetPlayerInAttackRange())
        {
            UpdateMovementWeigths();
            return;
        }

        _counterAttackWeight = 0.8f + _aggresiveLevel;
        _attackWeight = 0.2f + _aggresiveLevel;

        GD.Print("Player staggered, weights adjusted");
        //? take action?
    }

    #endregion

    #endregion
}
