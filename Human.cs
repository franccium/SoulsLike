using Godot;
using System;

public partial class Human : CharacterBody3D
{
    protected AnimationTree _animationTree;
    protected const float TransitionSpeed = 0.1f;
    protected const float HumanSpeed = 5.0f;
    protected const float RotationSpeed = 10;
    protected const float JumpVelocity = 4.5f;
    protected float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    protected StringName _locomotionStatePlaybackPath = "parameters/LocomotionStateMachine/playback";
    protected StringName _locomotionBlendPath = "parameters/LocomotionStateMachine/walk/blend_position";

    protected StringName _jumpStateName = "jump";
    protected StringName _fallingStateName = "fall";
    protected StringName _walkingStateName = "walk";

    protected StringName _upperBodyStatePlaybackPath = "parameters/UpperBodyStateMachine/playback";

    AnimationNodeStateMachinePlayback _upperBodyStateMachinePlayback;
    AnimationNodeStateMachinePlayback _locomotionStateMachinePlayback;

    protected Vector2 _current2DVelocity = Vector2.Zero;
    protected Vector2 _current2DDirection = Vector2.Zero;

    protected bool jumpQueued = false;

    public override void _Ready()
    {
        _animationTree = GetNode<AnimationTree>("AnimationTree");

        _upperBodyStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_upperBodyStatePlaybackPath);
        _locomotionStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_locomotionStatePlaybackPath);

        GatherCombatRequirements();
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

        Velocity = velocity;

        MoveAndSlide();
    }


    #region MOVEMENT

    public enum GroundedStates
    {
        Grounded,
        Jumping,
        Falling,
    }
    public GroundedStates GroundedState = GroundedStates.Grounded;

    protected virtual void DodgeRoll()
    {
        GD.Print("Dodge Roll");
    }

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
            velocity.Y = JumpVelocity;
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

    protected StringName _staggerStateName = "stagger";
    protected StringName _staggerOneShotRequestPath = "parameters/stagger_oneshot/request";
    protected StringName _staggerOneShotIsActivePath = "parameters/stagger_oneshot/active";

    protected virtual void GatherCombatRequirements()
    {
        _rightHandContainer = GetNode<Node3D>("CharacterRig/GeneralSkeleton/RightHandAttachment/RightHandContainer");
        _leftHandContainer = GetNode<Node3D>("CharacterRig/GeneralSkeleton/LeftHandAttachment/LeftHandContainer");
        _rightHipContainer = GetNode<Node3D>("CharacterRig/GeneralSkeleton/RightHipAttachment/RightHipContainer");
        _leftHipContainer = GetNode<Node3D>("CharacterRig/GeneralSkeleton/LeftHipAttachment/LeftHipContainer");

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
    }

    public void Stagger()
    {
        //_upperBodyStateMachinePlayback.Travel("stagger");
        //_locomotionStateMachinePlayback.Travel("stagger");
        //_animationTree.
        _animationTree.Set(_staggerOneShotRequestPath, (int)AnimationNodeOneShot.OneShotRequest.Fire);

        //todo disable actions
    }

    public bool isStaggered() => _animationTree.Get(_staggerOneShotIsActivePath).AsBool();

    #region HITBOX

    private Area3D _hitboxArea;

    protected virtual void InitialiseHitbox()
    {
        _hitboxArea = GetNode<Area3D>("HitboxArea");
        _hitboxArea.BodyEntered += OnHitboxAreaBodyEntered;
    }
    
    public void OnHitboxAreaBodyEntered(Node3D body)
    {
    }

    #endregion

    #endregion
}
