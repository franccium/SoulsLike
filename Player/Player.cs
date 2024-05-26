using Godot;
using System;
using System.Collections;

public partial class Player : Human
{
    private Node3D _cameraContainer;
    private Node3D _cameraPivot;
    
    private bool _shiftModifier = false;
    private bool _altModifier = false;

    private Vector2 _currentInput = Vector2.Zero;

    AnimationNodeStateMachinePlayback _upperBodyStateMachinePlayback;
    AnimationNodeStateMachinePlayback _locomotionStateMachinePlayback;

    public override void _Ready()
    {
        base._Ready();

        _cameraContainer = GetNode<Node3D>("CameraController/CameraContainer");
        _cameraPivot = _cameraContainer.GetNode<Node3D>("CameraPivot");
        _animationTree = GetNode<AnimationTree>("AnimationTree");

        _upperBodyStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_upperBodyStatePlaybackPath);
        _locomotionStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_locomotionStatePlaybackPath);

        GatherCombatRequirements();

        GameController.Instance.SetPlayer(this);
    }

    public override void _Process(double delta)
    {
        _current2DDirection = _currentInput;

        base._Process(delta);

        if(!IsInAction() && _queuedAttack != SwordCombatComponent.SwordAttacks.None)
        {
            Attack();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        //GD.Print("current animation state: " + CurrentAnimationState);
        Vector3 velocity = Velocity;

        UpdateGroundedStateMovement(ref velocity, (float)delta);

        UpdateRotation((float)delta);

        if(IsInAction() && !IsBlockingConst())
        {
            UpdateAnimationRootMotion(ref velocity, (float)delta);
        }
        else
        {
            UpdateInputMovement(ref velocity, (float)delta);
        }

        Velocity = velocity;

        MoveAndSlide();
    }

    private void UpdateMovementRoot(float delta)
    {
        //! have to turn of _process
        Vector3 velocity = Velocity;
        var direction = new Vector3(Input.GetActionStrength("left") - Input.GetActionStrength("right"), 0, Input.GetActionStrength("forward") - Input.GetActionStrength("backwards"));
        direction = direction.Rotated(Vector3.Up, _cameraContainer.GlobalTransform.Basis.GetEuler().Y).Normalized();

        _animationTree.Set(_locomotionBlendPath, new Vector2(0, -1));

        if (direction != Vector3.Zero)
            Rotation = new Vector3(Rotation.X, Mathf.Atan2(-direction.X, -direction.Z), Rotation.Z);

        Quaternion currentRotation = Transform.Basis.GetRotationQuaternion();
        GD.Print("current rotation: " + currentRotation);
        GD.Print("root motion position: " + _animationTree.GetRootMotionPosition());

        velocity = -(currentRotation.Normalized() * _animationTree.GetRootMotionPosition()) / delta;

        Velocity = velocity;

        GD.Print("Velocity: " + Velocity);
    }

    #region INPUT

    public override void _Input(InputEvent @event)
    {
        _shiftModifier = Input.IsActionPressed("shift_modifier");
        _altModifier = Input.IsActionPressed("alt_modifier");

        if (IsOnFloor())
        {
            if(Input.IsActionJustPressed("jump"))
            {
                BeginJump();
            }
            else if (Input.IsActionJustPressed("dodge"))
            {
                if(_shiftModifier)
                {
                    SideDodgeRoll();
                }
                else
                {
                    DodgeRoll();
                }
            }
            else if(Input.IsActionPressed("v"))
            {
                BeginSprint();
            }
            else if(Input.IsActionJustReleased("v"))
            {
                EndSprint();
            }
        }

        if (Input.IsActionJustPressed("sheath_weapon"))
        {
            if(_swordCombatComponent.CombatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
            {
                _swordCombatComponent.UnequipWeaponRightHand();
            }
            else if(_swordCombatComponent.CombatState == CombatComponent.CombatStates.SwordSheathed)
            {
                _swordCombatComponent.EquipWeaponRightHand();
            }
        }
        if (Input.IsActionJustPressed("sheath_weapon_2"))
        {
            if (_shieldCombatComponent.CombatState == CombatComponent.CombatStates.ShieldDrawnOneHanded)
            {
                _shieldCombatComponent.UnequipShieldLeftHand();
            }
            else if (_shieldCombatComponent.CombatState == CombatComponent.CombatStates.ShieldSheathed)
            {
                _shieldCombatComponent.EquipShieldLeftHand();
            }
        }

        if (Input.IsActionJustPressed("toggle_two_hand"))
        {
        }

        if(Input.IsActionJustPressed("lmb"))
        {
            if(_swordCombatComponent.CombatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
            {
                StringName currentAnimation = _upperBodyStateMachinePlayback.GetCurrentNode().ToString();
                if(currentAnimation == SwordCombatComponent.OneHandAttackName)
                {
                    QueueAttack(SwordCombatComponent.SwordAttacks.AltOneHandAttack);
                    return;
                }
                else if(currentAnimation == SwordCombatComponent.AltOneHandAttackName)
                {
                    QueueAttack(SwordCombatComponent.SwordAttacks.StrongOneHandAttack);
                    return;
                }

                if (_shiftModifier)
                {
                    if (_altModifier)
                    {
                        QueueAttack(SwordCombatComponent.SwordAttacks.StrongAltOneHandAttack);
                    }
                    else
                    {
                        QueueAttack(SwordCombatComponent.SwordAttacks.StrongOneHandAttack);
                    }
                }
                else if (_altModifier)
                {
                    QueueAttack(SwordCombatComponent.SwordAttacks.AltOneHandAttack); 
                }
                else
                {
                    QueueAttack(SwordCombatComponent.SwordAttacks.OneHandAttack);
                }
            }
        }

        if (_shieldCombatComponent.CombatState == CombatComponent.CombatStates.ShieldDrawnOneHanded)
        {
            if (IsBlockingConst() && Input.IsActionJustReleased("rmb"))
            {
                GD.Print("Stop Block");
                StopBlockConst();
            }
            else if (!IsBlockingConst() && Input.IsActionPressed("rmb"))
            {
                GD.Print("Block");
                BlockConst();
            }

            if (Input.IsActionJustPressed("rmb") && _shiftModifier)
            {
                Parry();
            }
        }
    }

    #endregion

    #region MOVEMENT

    private void UpdateInputMovement(ref Vector3 velocity, float delta)
    {
        if (IsInAction() && !IsBlockingConst())
        {
            return;
        }

        _currentInput = Input.GetVector("left", "right", "forward", "backwards");
        // input direction responsive to the direction the player's camera is facing
        _movementDirection = -(_cameraPivot.Transform.Basis * new Vector3(_currentInput.X, 0, _currentInput.Y)).Normalized();
        if (_movementDirection != Vector3.Zero)
        {
            velocity.X = _movementDirection.X * _currentSpeed;
            velocity.Z = _movementDirection.Z * _currentSpeed;
            Vector3 currentNormalizedVelocity = ToLocal(GlobalPosition + velocity);
            _currentInput = new Vector2(currentNormalizedVelocity.X, currentNormalizedVelocity.Z).LimitLength(1);
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, _currentSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, _currentSpeed);

            _currentInput = Vector2.Zero;
        }
    }

    private void UpdateRotation(float delta)
    {
        if (Velocity.Length() > 0.1f)
        {
            RotationDegrees = new Vector3(
                RotationDegrees.X,
                Mathf.Floor(Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(RotationDegrees.Y), Mathf.Atan2(-Velocity.X, -Velocity.Z), delta * RotationSpeed))),
                RotationDegrees.Z
            );
        }
    }

    #endregion

    #region COMBAT

    #region SIGNALS

    [Signal] public delegate void PlayerAttackEventHandler();
    [Signal] public delegate void PlayerBlockEventHandler();
    [Signal] public delegate void PlayerDodgeRollEventHandler();
    [Signal] public delegate void PlayerStaggeredEventHandler();

    #endregion

    protected override void GatherCombatRequirements()
    {
        base.GatherCombatRequirements();
    }

    protected override void Attack(Vector3? direction = null)
    {
        base.Attack();

        EmitSignal(nameof(PlayerAttack));
    }

    protected override void BlockConst()
    {
        base.BlockConst();

        EmitSignal(nameof(PlayerBlock));
    }

    protected override void DodgeRoll(Vector3? direction = null)
    {
        base.DodgeRoll();

        EmitSignal(nameof(PlayerDodgeRoll));
    }

    public override void Stagger()
    {
        base.Stagger();

        EmitSignal(nameof(PlayerStaggered));
    }

    #endregion
}
