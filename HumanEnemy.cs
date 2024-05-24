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
        
        if(_playerSeen)
        {
            if(!_isTakingAction)
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
        if(_shouldMoveUpToPlayer)
            MoveInPlayersDirection();
        else 
            Patrol();

        base.UpdateMovementInDirection(ref velocity, (float)delta);

        Velocity = velocity;
        MoveAndSlide();
    }

    protected virtual void TakeAction()
    {
        GD.Print("anim state: " + CurrentAnimationState);
        if(CurrentAnimationState != AnimationStates.Idle)
            return;

        if(GetDistanceToPlayer() > _actionRange)
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

        UpdateBehaviourLevels();
        UpdateMovementWeigths();
        UpdateRecoveringWeights();

        foreach(var (weight, action) in _weightedActions)
        {
            if(weight() > 0f)
                GD.Print(weight() + " : " + action.Method.Name);
        }

        TakeRandomWeightedAction();
    }

    protected virtual float GetDistanceToPlayer()
    {
        return GlobalPosition.DistanceTo(_player.GlobalPosition);
    }

    #region COMBAT

    protected float _detectionRange = 150f;
    protected bool _playerSeen = false;
    protected bool _inCombat = false;

    protected float _attackRange = 5f;
    protected bool _inAttackRange = false;
    protected bool _queuedAttack = false;

    protected float _actionRange = 10f;
    protected bool _shouldMoveUpToPlayer = false;

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

    protected virtual void Patrol()
    {
        _movementDirection = Vector3.Zero;
    }

    protected virtual void MoveInPlayersDirection()
    {
        GD.Print("Moving to player");
        _movementDirection = GlobalPosition.DirectionTo(_player.GlobalPosition);
    }

    protected override void OnAnimationFinished(StringName animName)
    {
        GD.Print("animation finished: " + animName);

        switch (CurrentAnimationState)
        {
            case AnimationStates.Attacking:
                _isAttacking = false;
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

    protected virtual void InitialiseAttack()
    {
        GD.Print("Enemy trying to attack");
        _queuedAttack = false;

        _swordCombatComponent.OneHandAttackSword();
        _currentAIState = AIStates.Attack;
    }

    protected virtual void InitialiseBlock()
    {
        GD.Print("Enemy trying to block");

        if (CurrentAnimationState == AnimationStates.Idle)
        {
            BlockConst();
            _shieldCombatComponent.OneHandConstBlockShield();
            _currentAIState = AIStates.Block;
        }
    }

    protected virtual void InitialiseParry()
    {
        GD.Print("Enemy trying to parry");

        Parry();
        _shieldCombatComponent.OneHandParryShield();
        _currentAIState = AIStates.Parry;
    }

    protected virtual void InitialiseDodgeRoll()
    {
        GD.Print("Enemy trying to dodge roll");

        DodgeRoll();
        _currentAIState = AIStates.DodgeRoll;
    }

    protected virtual void InitialiseStrafe()
    {
        GD.Print("Enemy trying to strafe");
        _currentAIState = AIStates.Strafe;
    }

    protected virtual void InitialiseMoveUp()
    {
        GD.Print("Enemy trying to move");

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

        _swordCombatComponent.OneHandAttackSword();
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

    protected virtual void UpdateBehaviourLevels()
    {
        // when low on hp, defensive level increases
        // when low on stamina, aggresive level increases
        // or it should be inverted idk yet
    }

    protected virtual void UpdateMovementWeigths()
    {
        // when player tries to go away, move to him
        if(GetDistanceToPlayer() > _attackRange)
            _moveUpWeight = 2.5f;
    }

    protected virtual void TakeRandomWeightedAction()
    {
        float totalWeight = GetSumOfWeights();

        float randomWeight = (float)GD.RandRange(0, totalWeight) + 0.1f;

        foreach(var (weight, action) in _weightedActions)
        {
            randomWeight -= weight();
            if(randomWeight <= 0f)
            {
                action();
                break;
            }
        }

        _isTakingAction = true;
    }

    protected virtual void UpdateRecoveringWeights()
    {
        if(_attributeComponent.GetHealthPercentage() > 0.65f)
            _tryHealWeight = 0f;
        else
            _tryHealWeight = _defensiveLevel * 1 / _attributeComponent.GetHealthPercentage();
        
        if(_attributeComponent.GetStaminaPercentage() > 0.65f)
            _tryRecoverWeight = 0f;
        else
            _tryRecoverWeight = _aggresiveLevel * 1 / _attributeComponent.GetStaminaPercentage();
    }

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

        _counterAttackWeight = 0.8f + _aggresiveLevel;
        _attackWeight = 0.2f + _aggresiveLevel;

        GD.Print("Player staggered, weights adjusted");
        //? take action?
    }

    #endregion

    #endregion
}
