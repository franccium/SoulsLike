using Godot;
using System;

public partial class Human : CharacterBody3D
{
    protected AnimationTree _animationTree;
    protected const float TransitionSpeed = 0.1f;
    protected const float RotationSpeed = 10;
    protected float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    protected static readonly StringName _locomotionStatePlaybackPath = "parameters/LocomotionStateMachine/playback";
    protected static readonly StringName _locomotionBlendPath = "parameters/LocomotionStateMachine/walk/blend_position";

    protected static readonly StringName _jumpStateName = "jump";
    protected static readonly StringName _fallingStateName = "fall";
    protected static readonly StringName _walkingStateName = "walk";

    protected static readonly StringName _upperBodyStatePlaybackPath = "parameters/UpperBodyStateMachine/playback";

    AnimationNodeStateMachinePlayback _upperBodyStateMachinePlayback;
    AnimationNodeStateMachinePlayback _locomotionStateMachinePlayback;

    protected Vector2 _current2DVelocity = Vector2.Zero;
    protected Vector2 _current2DDirection = Vector2.Zero;
    protected Vector3 _movementDirection = Vector3.Zero;

    protected bool jumpQueued = false;

    public enum AnimationStates
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling,
        Attacking,
        DodgeRoll,
        Stagger,
    }

    public AnimationStates CurrentAnimationState = AnimationStates.Idle;

    protected float _currentSpeed = 0f;

    public override void _Ready()
    {
        _animationTree = GetNode<AnimationTree>("AnimationTree");

        _upperBodyStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_upperBodyStatePlaybackPath);
        _locomotionStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_locomotionStatePlaybackPath);

        _animationTree.AnimationFinished += OnAnimationFinished;

        GatherCombatRequirements();
        GatherAttributeRequirements();
    }

    public override void _Process(double delta)
    {
        Vector2 newDelta = _current2DDirection - _current2DVelocity;
        if (newDelta.Length() > TransitionSpeed * (float)delta)
        {
            newDelta = newDelta.Normalized() * TransitionSpeed * (float)delta;
        }
        _current2DVelocity += newDelta;

        if (_combatState == CombatComponent.CombatStates.SwordSheathed)
        {
            _animationTree.Set(_locomotionBlendPath, _current2DVelocity);
        }
        else
        {
            _animationTree.Set(_swordCombatComponent.CombatLocomotionBlendPath, _current2DVelocity);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        UpdateGroundedStateMovement(ref velocity, (float)delta);

        //direction and 2D direction
        if(CurrentAnimationState != AnimationStates.Idle)
        {
            UpdateAnimationRootMotion(ref velocity, (float)delta);
        }

        Velocity = velocity;

        MoveAndSlide();
    }

    protected virtual void UpdateAnimationRootMotion(ref Vector3 velocity, float delta)
    {
        Vector3 rootMotionPosition = _animationTree.GetRootMotionPosition();

        if (_rollDirection != Vector3.Zero)
            Rotation = new Vector3(Rotation.X, Mathf.Atan2(-_rollDirection.X, -_rollDirection.Z), Rotation.Z);

        Quaternion currentRotation = Transform.Basis.GetRotationQuaternion();

        velocity = -(currentRotation.Normalized() * rootMotionPosition * _attributeComponent.RollSpeed) / delta;
    }

    protected virtual void OnAnimationFinished(StringName animName)
    {
        GD.Print("animation finished: " + animName);
        
        switch (CurrentAnimationState)
        {
            case AnimationStates.Attacking:
                _isAttacking = false;
                break;
            case AnimationStates.DodgeRoll:
                OnDodgeRollFinished();
                break; 
        }

        CurrentAnimationState = AnimationStates.Idle;
    }


    #region MOVEMENT

    public enum GroundedStates
    {
        Grounded,
        Jumping,
        Falling,
    }
    public GroundedStates GroundedState = GroundedStates.Grounded;

    protected virtual void UpdateGroundedStateMovement(ref Vector3 velocity, float delta)
    {
        if (!IsOnFloor())
        {
            jumpQueued = false;
            velocity.Y -= Gravity * (float)delta;
            if (GroundedState != GroundedStates.Falling)
            {
                GroundedState = GroundedStates.Falling;
                _locomotionStateMachinePlayback.Travel(_fallingStateName);
            }
        }
        else
        {
            if (GroundedState != GroundedStates.Grounded)
            {
                GroundedState = GroundedStates.Grounded;
                GD.Print("falling stopped");

                if (_combatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
                {
                    _locomotionStateMachinePlayback.Travel(_swordCombatComponent.CombatWalkStateName);
                    GD.Print("combat walk");
                }
                else
                {
                    _locomotionStateMachinePlayback.Travel(_walkingStateName);
                    GD.Print("walk");
                }
            }
        }

        if (jumpQueued)
        {
            velocity.Y = _attributeComponent.JumpVelocity;
            jumpQueued = false;
            GroundedState = GroundedStates.Falling;
        }
    }

    protected virtual void BeginJump()
    {
        if (_combatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
        {
            _locomotionStateMachinePlayback.Travel(_swordCombatComponent.CombatJumpStateName);
        }
        else
        {
            _locomotionStateMachinePlayback.Travel(_jumpStateName);
        }

        GroundedState = GroundedStates.Jumping;
    }

    public void ExecuteJumpVelocity()
    {
        jumpQueued = true;
    }

    #endregion

    #region COMBAT

    protected Node3D _rightHandContainer;
    protected Node3D _leftHandContainer;
    protected Node3D _rightHipContainer;
    protected Node3D _leftHipContainer;

    protected Node3D _rightHipItemContainer;
    protected Node3D _leftHipItemContainer;

    protected CombatComponent _combatComponent;
    protected SwordCombatComponent _swordCombatComponent;

    protected CombatComponent.CombatStates _combatState
    {
        get => _combatComponent.CombatState;
        set => _combatComponent.CombatState = value;
    }

    protected bool _isAttacking = false;

    protected static readonly StringName _staggerStateName = "stagger";
    protected static readonly StringName _staggerOneShotRequestPath = "parameters/stagger_oneshot/request";
    protected static readonly StringName _staggerOneShotIsActivePath = "parameters/stagger_oneshot/active";

    protected virtual void GatherCombatRequirements()
    {
        _rightHandContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/RightHandAttachment/RightHandContainer");
        _leftHandContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/LeftHandAttachment/LeftHandContainer");
        _rightHipContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/RightHipAttachment/RightHipContainer");
        _leftHipContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/LeftHipAttachment/LeftHipContainer");

        _rightHipItemContainer = _rightHipContainer.GetNode<Node3D>("ItemContainer");
        _leftHipItemContainer = _leftHipContainer.GetNode<Node3D>("ItemContainer");


        _combatComponent = GetNode<CombatComponent>("CombatComponent");
        _swordCombatComponent = _combatComponent as SwordCombatComponent;

        _swordCombatComponent.RightHandContainer = _rightHandContainer;
        _swordCombatComponent.LeftHandContainer = _leftHandContainer;
        _swordCombatComponent.RightHipContainer = _rightHipContainer;
        _swordCombatComponent.LeftHipContainer = _leftHipContainer;
        _swordCombatComponent.RightHipItemContainer = _rightHipItemContainer;
        _swordCombatComponent.LeftHipItemContainer = _leftHipItemContainer;

        _swordCombatComponent.AnimationTree = _animationTree;
        _swordCombatComponent.UpperBodyStateMachinePlayback = _upperBodyStateMachinePlayback;
        _swordCombatComponent.LocomotionStateMachinePlayback = _locomotionStateMachinePlayback;

        _swordCombatComponent.EquippedSword = _leftHipItemContainer.GetNode<Sword>("bastard_sword");

        //InitialiseHitbox();
    }

    #region STAGGER

    public void Stagger()
    {
        //_upperBodyStateMachinePlayback.Travel("stagger");
        //_locomotionStateMachinePlayback.Travel("stagger");
        //_animationTree.
        _animationTree.Set(_staggerOneShotRequestPath, (int)AnimationNodeOneShot.OneShotRequest.Fire);

        //todo disable actions
    }

    public bool isStaggered() => _animationTree.Get(_staggerOneShotIsActivePath).AsBool();

    #endregion

    #region DODGE ROLL

    protected static readonly StringName _dodgeRollOneShotRequestPath = "parameters/dodge_roll_oneshot/request";
    protected static readonly StringName _dodgeRollOneShotIsActivePath = "parameters/dodge_roll_oneshot/active";

    protected static readonly StringName _sideDodgeRollOneShotRequestPath = "parameters/side_dodge_roll_oneshot/request";
    protected static readonly StringName _sideDodgeRollOneShotIsActivePath = "parameters/side_dodge_roll_oneshot/active";

    public bool IsInvincible = false;

    protected Vector3 _rollDirection = Vector3.Zero;

    protected virtual void GatherDodgerollRequirements()
    {
        _animationTree.Set("parameters/dodge_roll_time_scale/scale", _attributeComponent.RollTimeScale);
        _animationTree.Set("parameters/side_dodge_roll_time_scale/scale", _attributeComponent.SideRollTimeScale);
    }

    protected virtual void DodgeRoll()
    {
        _rollDirection = _movementDirection;
        _animationTree.Set(_dodgeRollOneShotRequestPath, (int)AnimationNodeOneShot.OneShotRequest.Fire);
        GD.Print("dodge roll");
        //_hitboxArea.Monitoring = false; // wont work cause its the world collision shape that is being checked by swords area
        IsInvincible = true;
        //Velocity = _movementDirection * _attributeComponent.RollSpeed;
    }

    protected virtual void SideDodgeRoll()
    {
        _rollDirection = _movementDirection;
        _animationTree.Set(_sideDodgeRollOneShotRequestPath, (int)AnimationNodeOneShot.OneShotRequest.Fire);
        GD.Print("side dodge roll");
        IsInvincible = true;
        //Velocity = _movementDirection * _attributeComponent.RollSpeed;
    }

    // called by the dodge roll animation for now
    protected virtual void OnDodgeRollFinished()
    {
        GD.Print("dodge roll finished");
        //_hitboxArea.Monitoring = true;
        IsInvincible = false;
        _rollDirection = Vector3.Zero;
    }

    public bool isRolling() => _animationTree.Get(_dodgeRollOneShotIsActivePath).AsBool() || _animationTree.Get(_sideDodgeRollOneShotIsActivePath).AsBool();

    #endregion

    public bool isInAction() => isStaggered() || isRolling();

    #region HITBOX

    /*
    private Area3D _hitboxArea;

    
    protected virtual void InitialiseHitbox()
    {
        _hitboxArea = GetNode<Area3D>("HitboxArea");
        _hitboxArea.BodyEntered += OnHitboxAreaBodyEntered;
    }
    
    public void OnHitboxAreaBodyEntered(Node3D body)
    {
    }
    */

    #endregion

    #endregion

    #region ATTRIBUTES

    protected AttributeComponent _attributeComponent;

    protected float MovementSpeed => _attributeComponent.MovementSpeed;
    protected float MaxHealth => _attributeComponent.MaxHealth;
    protected float Health => _attributeComponent.Health;
    protected float MaxStamina => _attributeComponent.MaxStamina;
    protected float Stamina => _attributeComponent.Stamina;

    protected virtual void GatherAttributeRequirements()
    {
        _attributeComponent = GetNode<AttributeComponent>("AttributeComponent");

        GatherDodgerollRequirements();
    }

    /*
    protected HealthBar _healthBar;

    protected virtual void InitialiseHealthBar()
    {
        _healthBar = GetNode<SubViewport>("HealthBar").GetNode<HealthBar>("HealthBarProgressBar");
        _healthBar.SetMaxHealth(_attributeComponent.MaxHealth);
    }

    protected StaminaBar _staminaBar;
    
    */

    public virtual void TakeHealthDamage(int damage)
    {
        if (IsInvincible)
            return;

        _attributeComponent.Health -= damage;
        //_healthBar.SetHealth(_attributeComponent.Health);

        if (Health <= 0)
        {
            //todo die
        }
    }

    public virtual void TakeStaminaDamage(int damage)
    {
        if(IsInvincible)
            return;

        _attributeComponent.Stamina -= damage;

        GD.Print(_attributeComponent.Stamina);
        //_staminaBar

        if(Stamina <= 0)
        {
            Stagger();
            _attributeComponent.Stamina = _attributeComponent.MaxStamina;
        }
    }

    #endregion
}
