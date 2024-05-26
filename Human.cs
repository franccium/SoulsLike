using Godot;
using System;
using System.Collections.Generic;

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
        Parrying,
        BlockingConst,
        DodgeRoll,
        SideDodgeRoll,
        Stagger,
    }

    public AnimationStates CurrentAnimationState = AnimationStates.Idle;

    public override void _Ready()
    {
        _animationTree = GetNode<AnimationTree>("AnimationTree");

        _upperBodyStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_upperBodyStatePlaybackPath);
        _locomotionStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_locomotionStatePlaybackPath);

        _animationTree.AnimationFinished += OnAnimationFinished;

        GatherCombatRequirements();
        GatherAttributeRequirements();
        InitialiseGUIElements();
        InitialiseHurboxes();

        _currentSpeed = _attributeComponent.MovementSpeed;
    }

    public override void _Process(double delta)
    {
        UpdateLocomotionBlend((float)delta);
    }

    protected virtual void UpdateLocomotionBlend(float delta)
    {
        Vector2 newDelta = _current2DDirection - _current2DVelocity;
        if (newDelta.Length() > TransitionSpeed * delta)
        {
            newDelta = newDelta.Normalized() * TransitionSpeed * delta;
        }
        _current2DVelocity += newDelta;

        if (_swordCombatComponent.CombatState == CombatComponent.CombatStates.SwordSheathed)
        {
            _animationTree.Set(_locomotionBlendPath, _current2DVelocity);
        }
        else
        {
            _animationTree.Set(SwordCombatComponent.CombatLocomotionBlendPath, _current2DVelocity);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        UpdateGroundedStateMovement(ref velocity, (float)delta);

        //tododirection and 2D direction

        if (IsInAction() && !IsBlockingConst())
        {
            UpdateAnimationRootMotion(ref velocity, (float)delta);
        }
        else
        {
            UpdateMovementInDirection(ref velocity, (float)delta);
        }

        Velocity = velocity;

        MoveAndSlide();
    }

    protected virtual void UpdateAnimationRootMotion(ref Vector3 velocity, float delta)
    {
        Vector3 rootMotionPosition = _animationTree.GetRootMotionPosition();

        if (_animationDirection != Vector3.Zero)
            Rotation = new Vector3(Rotation.X, Mathf.Atan2(-_animationDirection.X, -_animationDirection.Z), Rotation.Z);

        Quaternion currentRotation = Transform.Basis.GetRotationQuaternion();

        velocity = -(currentRotation.Normalized() * rootMotionPosition * _attributeComponent.RollSpeed) / delta;
    }

    protected virtual void UpdateMovementInDirection(ref Vector3 velocity, float delta)
    {
        if (_movementDirection != Vector3.Zero)
        {
            velocity.X = _movementDirection.X * _currentSpeed;
            velocity.Z = _movementDirection.Z * _currentSpeed;
            //?Vector3 currentNormalizedVelocity = ToLocal(GlobalPosition + velocity);
            //?_currentInput = new Vector2(currentNormalizedVelocity.X, currentNormalizedVelocity.Z).LimitLength(1);
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, _currentSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, _currentSpeed);
            //?_currentInput = Vector2.Zero;
        }

        if (Velocity.Length() > 0.1f)
        {
            RotationDegrees = new Vector3(
                RotationDegrees.X,
                Mathf.Floor(Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(RotationDegrees.Y), Mathf.Atan2(-Velocity.X, -Velocity.Z), (float)delta * RotationSpeed))),
                RotationDegrees.Z
            );
        }
    }

    #region MOVEMENT

    public enum GroundedStates
    {
        Grounded,
        Jumping,
        Falling,
    }
    public GroundedStates GroundedState = GroundedStates.Grounded;

    protected float _currentSpeed = 0f;

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

                if (_swordCombatComponent.CombatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
                {
                    _locomotionStateMachinePlayback.Travel(SwordCombatComponent.CombatWalkStateName);
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
        if (_swordCombatComponent.CombatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
        {
            _locomotionStateMachinePlayback.Travel(SwordCombatComponent.CombatJumpStateName);
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

    protected void BeginSprint()
    {
        _currentSpeed = _attributeComponent.SprintSpeed;
    }

    protected void EndSprint()
    {
        _currentSpeed = MovementSpeed;
    }

    #endregion

    #region COMBAT

    protected Node3D _rightHandContainer;
    protected Node3D _leftHandContainer;
    protected Node3D _rightHipContainer;
    protected Node3D _leftHipContainer;
    protected Node3D _backContainer;

    protected Node3D _rightHipItemContainer;
    protected Node3D _leftHipItemContainer;

    protected Node3D _backItemContainer;

    protected SwordCombatComponent _swordCombatComponent;
    protected ShieldCombatComponent _shieldCombatComponent;

    protected static readonly StringName _staggerStateName = "stagger";
    protected static readonly StringName _staggerOneShotRequestPath = "parameters/stagger_oneshot/request";
    protected static readonly StringName _staggerOneShotIsActivePath = "parameters/stagger_oneshot/active";

    protected virtual void GatherCombatRequirements()
    {
        _rightHandContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/RightHandAttachment/RightHandContainer");
        _leftHandContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/LeftHandAttachment/LeftHandContainer");
        _rightHipContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/RightHipAttachment/RightHipContainer");
        _leftHipContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/LeftHipAttachment/LeftHipContainer");
        _backContainer = GetNode<Node3D>("CharacterRig/Armature_001/Skeleton3D/Spine2Attachment/BackContainer");

        _rightHipItemContainer = _rightHipContainer.GetNode<Node3D>("ItemContainer");
        _leftHipItemContainer = _leftHipContainer.GetNode<Node3D>("ItemContainer");
        _backItemContainer = _backContainer.GetNode<Node3D>("BackItemContainer");


        _swordCombatComponent = GetNode<SwordCombatComponent>("SwordCombatComponent");

        _swordCombatComponent.RightHandContainer = _rightHandContainer;
        _swordCombatComponent.LeftHandContainer = _leftHandContainer;
        _swordCombatComponent.RightHipContainer = _rightHipContainer;
        _swordCombatComponent.LeftHipContainer = _leftHipContainer;
        _swordCombatComponent.RightHipItemContainer = _rightHipItemContainer;
        _swordCombatComponent.LeftHipItemContainer = _leftHipItemContainer;

        _swordCombatComponent.AnimationTree = _animationTree;
        _swordCombatComponent.UpperBodyStateMachinePlayback = _upperBodyStateMachinePlayback;
        _swordCombatComponent.LocomotionStateMachinePlayback = _locomotionStateMachinePlayback;

        _swordCombatComponent.SetWeapon(this, _leftHipItemContainer.GetNode<Sword>("bastard_sword"), _leftHipItemContainer, _leftHipContainer, _rightHandContainer);


        _shieldCombatComponent = GetNode<ShieldCombatComponent>("ShieldCombatComponent");
        _shieldCombatComponent.RightHandContainer = _rightHandContainer;
        _shieldCombatComponent.LeftHandContainer = _leftHandContainer;
        _shieldCombatComponent.BackItemContainer = _backItemContainer;

        _shieldCombatComponent.AnimationTree = _animationTree;
        _shieldCombatComponent.UpperBodyStateMachinePlayback = _upperBodyStateMachinePlayback;
        _shieldCombatComponent.LocomotionStateMachinePlayback = _locomotionStateMachinePlayback;
        _shieldCombatComponent.SetWeapon(this, _backItemContainer.GetNode<Shield>("VikingShield"), _backItemContainer, _backContainer, _leftHandContainer);

        //InitialiseHitbox();
    }

    #region QUEUED ATTACK

    protected SwordCombatComponent.SwordAttacks _queuedAttack = SwordCombatComponent.SwordAttacks.None;

    protected virtual void QueueAttack(SwordCombatComponent.SwordAttacks attack)
    {
        _queuedAttack = attack;
    }

    #endregion

    #region ANIMATED ACTIONS

    protected virtual void TakeAnimatedAction(AnimationStates action, Vector3? direction = null)
    {
        if (IsInAction() && !IsBlockingConst())
            return;

        SetAnimationState(action, direction);
    }

    protected virtual void Attack(Vector3? direction = null)
    {
        TakeAnimatedAction(AnimationStates.Attacking, direction);
    }

    protected virtual void Parry(Vector3? direction = null)
    {
        TakeAnimatedAction(AnimationStates.Parrying, direction);
    }

    protected virtual void BlockConst()
    {
        TakeAnimatedAction(AnimationStates.BlockingConst);
    }

    protected virtual void StopBlockConst()
    {
        TakeAnimatedAction(AnimationStates.Idle);
    }

    protected virtual void SetAnimationState(AnimationStates state, Vector3? direction = null)
    {
        CurrentAnimationState = state;
        _animationDirection = direction ?? _movementDirection;

        switch (state)
        {
            case AnimationStates.Attacking:
                _swordCombatComponent.SwordAttack(_queuedAttack);
                _queuedAttack = SwordCombatComponent.SwordAttacks.None;
                break;
            case AnimationStates.Parrying:
                _shieldCombatComponent.OneHandParryShield();
                break;
            case AnimationStates.BlockingConst:
                _currentSpeed = _attributeComponent.BlockingMovementSpeed;
                _shieldCombatComponent.OneHandConstBlockShield();
                break;
            case AnimationStates.Idle:
                _currentSpeed = _attributeComponent.MovementSpeed;
                _shieldCombatComponent.FinishShieldAction();
                break;
            case AnimationStates.DodgeRoll:
                _animationTree.Set(_dodgeRollOneShotRequestPath, (int)AnimationNodeOneShot.OneShotRequest.Fire);
                IsInvincible = true;
                break;
            case AnimationStates.SideDodgeRoll:
                _animationTree.Set(_sideDodgeRollOneShotRequestPath, (int)AnimationNodeOneShot.OneShotRequest.Fire);
                IsInvincible = true;
                break;
            case AnimationStates.Stagger:
                _movementDirection = Vector3.Zero;
                _animationTree.Set(_staggerOneShotRequestPath, (int)AnimationNodeOneShot.OneShotRequest.Fire);
                break;
        }
    }

    #region ON ANIMATION FINISHED

    protected virtual void OnAnimationFinished(StringName animName)
    {
        GD.Print("animation finished: " + animName);

        switch (CurrentAnimationState)
        {
            case AnimationStates.Attacking:
                _swordCombatComponent.FinishAttack();
                break;
            case AnimationStates.Parrying:
                _shieldCombatComponent.FinishShieldAction();
                break;
            case AnimationStates.DodgeRoll:
                OnDodgeRollFinished();
                break;
        }

        CurrentAnimationState = AnimationStates.Idle;
    }

    #endregion

    #region STAGGER

    public virtual void Stagger()
    {
        //_upperBodyStateMachinePlayback.Travel("stagger");
        //_locomotionStateMachinePlayback.Travel("stagger");
        //_animationTree.
        TakeAnimatedAction(AnimationStates.Stagger);

        //todo disable actions
    }

    public bool IsStaggered() => _animationTree.Get(_staggerOneShotIsActivePath).AsBool();

    #endregion

    #region DODGE ROLL

    protected static readonly StringName _dodgeRollOneShotRequestPath = "parameters/dodge_roll_oneshot/request";
    protected static readonly StringName _dodgeRollOneShotIsActivePath = "parameters/dodge_roll_oneshot/active";

    protected static readonly StringName _sideDodgeRollOneShotRequestPath = "parameters/side_dodge_roll_oneshot/request";
    protected static readonly StringName _sideDodgeRollOneShotIsActivePath = "parameters/side_dodge_roll_oneshot/active";

    public bool IsInvincible = false;

    protected Vector3 _animationDirection = Vector3.Zero;

    protected virtual void GatherDodgerollRequirements()
    {
        _animationTree.Set("parameters/dodge_roll_time_scale/scale", _attributeComponent.RollTimeScale);
        _animationTree.Set("parameters/side_dodge_roll_time_scale/scale", _attributeComponent.SideRollTimeScale);
    }

    protected virtual void DodgeRoll(Vector3? direction = null)
    {
        TakeAnimatedAction(AnimationStates.DodgeRoll, direction);
        GD.Print("dodge roll");
        //_hitboxArea.Monitoring = false; // wont work cause its the world collision shape that is being checked by swords area
        //Velocity = _movementDirection * _attributeComponent.RollSpeed;
    }

    protected virtual void SideDodgeRoll()
    {
        TakeAnimatedAction(AnimationStates.SideDodgeRoll);
        GD.Print("side dodge roll");
        //Velocity = _movementDirection * _attributeComponent.RollSpeed;
    }

    protected virtual void OnDodgeRollFinished()
    {
        GD.Print("dodge roll finished");
        //_hitboxArea.Monitoring = true;
        IsInvincible = false;
        _animationDirection = Vector3.Zero;
    }

    public bool IsRolling() => _animationTree.Get(_dodgeRollOneShotIsActivePath).AsBool() || _animationTree.Get(_sideDodgeRollOneShotIsActivePath).AsBool();

    #endregion

    //!public bool IsInAction() => IsStaggered() || IsRolling();
    public bool IsInAction() => CurrentAnimationState != AnimationStates.Idle;
    public bool IsBlockingConst() => CurrentAnimationState == AnimationStates.BlockingConst;

    #endregion

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


    protected HealthBar _healthBar;

    protected virtual void InitialiseGUIElements()
    {
        InitialiseHealthBar();
        InitialiseStaminaBar();
    }

    protected virtual void InitialiseHealthBar()
    {
        _healthBar = GetNode<Sprite3D>("HealthBar") as HealthBar;
        _healthBar.SetMaxHealth(_attributeComponent.MaxHealth);
    }

    protected StaminaBar _staminaBar;

    protected virtual void InitialiseStaminaBar()
    {
        _staminaBar = GetNode<Sprite3D>("StaminaBar") as StaminaBar;
        _staminaBar.SetMaxStamina(_attributeComponent.MaxStamina);
    }

    public virtual void TakeHealthDamage(int damage)
    {
        if (IsInvincible)
            return;

        _attributeComponent.Health -= damage;
        _healthBar.SetHealth(_attributeComponent.Health);

        if (Health <= 0)
        {
            //todo die
        }
    }

    public virtual void TakeStaminaDamage(int damage)
    {
        if (IsInvincible)
            return;

        _attributeComponent.Stamina -= damage;

        GD.Print(_attributeComponent.Stamina);
        _staminaBar.SetStamina(_attributeComponent.Stamina);

        if (Stamina <= 0)
        {
            Stagger();
            _attributeComponent.Stamina = _attributeComponent.MaxStamina;
        }
    }

    #endregion

    #region HURTBOXES

    protected enum HurtboxTypes
    {
        HeadHbox,
        BodyHbox,
        LeftArmHbox,
        RightArmHbox,
        LeftLegHbox,
        RightLegHbox,
        StomachHbox,
        HipsHbox,
    }

    protected Dictionary<Area3D, HurtboxTypes> _hurtboxes = new Dictionary<Area3D, HurtboxTypes>();

    protected virtual void InitialiseHurboxes()
    {
        Skeleton3D skeleton = GetNode<Skeleton3D>("CharacterRig/Armature_001/Skeleton3D");
        foreach (Node3D node in skeleton.GetChildren())
        {
            if (node is BoneAttachment3D)
            {
                Area3D area = node.GetNodeOrNull<Area3D>("Area3D");
                if (area != null)
                {
                    GD.Print("found hurtbox: " + node.Name);
                    _hurtboxes.Add(area, (HurtboxTypes)Enum.Parse(typeof(HurtboxTypes), node.Name));

                    area.AreaEntered += (otherArea) => OnHurtboxAreaEntered(otherArea, area);
                }
            }
        }
    }

    protected virtual void OnHurtboxAreaEntered(Area3D area, Area3D hurtbox) //todo bind the appropriate hurtbox to the signal
    {
        if(area is not WeaponHitboxArea weaponHitboxArea)
            return;

        if (weaponHitboxArea.Weapon.WeaponOwner == this)
            return;
        
        if (weaponHitboxArea.Weapon is Sword sword)
        {
            GD.Print("sword hit hurtbox: " + _hurtboxes[hurtbox].ToString() + " of " + this.GetType().Name);
        }
        /*
        if(area.GetParent() == this)
            return;
        if (_hurtboxes.ContainsKey(area))
        {
            GD.Print("hurtboxes entered each other");
            return;
        }
        if(area == _swordCombatComponent.EquippedWeapon.HitboxArea)
        {
            GD.Print("own sword hit hurtbox");
            return;
        }

        GD.Print("entity: " + this.GetType().Name + " had its hurtbox hit: " + hurtbox.Name + " " + _hurtboxes[hurtbox].ToString() + " by " + area.GetParent().GetType().Name);
        */
    }

    #endregion
}
