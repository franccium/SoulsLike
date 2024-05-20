using Godot;
using System;

public partial class RootMotionPlayer : RootMotionHuman
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

        //todoGameController.Instance.SetPlayer(this);
    }

    public override void _Process(double delta)
    {
        _current2DDirection = _currentInput;

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        UpdateGroundedStateMovement(ref velocity, (float)delta);

        UpdateInputMovement(ref velocity, (float)delta);

        UpdateRotation((float)delta);
        /*
                var lookat = get_tree().get_nodes_in_group("CameraController")[0].get_node("LookAt")

            if not is_on_floor():
                velocity.y -= gravity * delta

            var h_rot = get_tree().get_nodes_in_group("CameraController")[0].global_transform.basis.get_euler().y

            if Input.is_action_just_pressed("ui_accept") and is_on_floor():
                velocity.y = JUMP_VELOCITY

            var direction = Vector3(Input.get_action_strength("ui_left") - Input.get_action_strength("ui_right"), 0, Input.get_action_strength("ui_up") - Input.get_action_strength("ui_down"))

            if !Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
                direction = direction.rotated(Vector3.UP, h_rot).normalized()
            $AnimationTree.set("parameters/conditions/moving", direction != Vector3.ZERO)
            $AnimationTree.set("parameters/conditions/idle", direction == Vector3.ZERO)

            print(direction)
            $AnimationTree.set("parameters/BlendSpace2D/blend_position", Vector2(0, -1))

            if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
                $AnimationTree.set("parameters/BlendSpace2D/blend_position", Vector2(-direction.x, -direction.z))


            if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
                look_at(Vector3(lookat.global_position.x, global_position.y, lookat.global_position.z))

            elif direction != Vector3.ZERO:
                rotation = Vector3(rotation.x, atan2(-direction.x, -direction.z), rotation.z)


            var currentRotation = transform.basis.get_rotation_quaternion()

            velocity = (currentRotation.normalized() * $AnimationTree.get_root_motion_position()) / delta
            */
        /*
        var direction = new Vector3(Input.GetActionStrength("left") - Input.GetActionStrength("right"), 0, Input.GetActionStrength("forward") - Input.GetActionStrength("backwards"));
        _animationTree.Set(_locomotionBlendPath, new Vector2(0, -1));
        if(direction != Vector3.Zero) 
            Rotation = new Vector3(Rotation.X, Mathf.Atan2(-direction.X, -direction.Z), Rotation.Z);
        var currentRotation = Transform.Basis.GetRotationQuaternion();
        velocity = (currentRotation.Normalized() * _animationTree.GetRootMotionPosition()) / (float)delta;
        */

        Velocity = velocity;
        MoveAndSlide();
    }

    public override void _Input(InputEvent @event)
    {
        _shiftModifier = Input.IsActionPressed("shift_modifier");
        _altModifier = Input.IsActionPressed("alt_modifier");

        if (IsOnFloor())
        {
            if (Input.IsActionJustPressed("jump"))
            {
                BeginJump();
            }
            else if (Input.IsActionJustPressed("dodge"))
            {
                DodgeRoll();
            }
        }

        if (Input.IsActionJustPressed("sheath_weapon"))
        {
            if (_combatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
            {
                _swordCombatComponent.UnequipWeaponRightHand();
            }
            else if (_combatState == CombatComponent.CombatStates.SwordSheathed)
            {
                _swordCombatComponent.EquipWeaponRightHand();
            }
        }

        if (Input.IsActionJustPressed("toggle_two_hand"))
        {
        }

        if (Input.IsActionJustPressed("lmb"))
        {
            if (isInAction())
            {
                return;
            }

            if (_combatState == CombatComponent.CombatStates.SwordDrawnOneHanded)
            {
                StringName currentAnimation = _upperBodyStateMachinePlayback.GetCurrentNode().ToString();
                if (currentAnimation == _swordCombatComponent.OneHandAttackName)
                {
                    _swordCombatComponent.OneHandAltAttackSword();
                    return;
                }
                else if (currentAnimation == _swordCombatComponent.AltOneHandAttackName)
                {
                    _swordCombatComponent.StrongOneHandAttackSword();
                    return;
                }

                if (_shiftModifier)
                {
                    if (_altModifier)
                    {
                        _swordCombatComponent.StrongAltOneHandAttackSword();
                    }
                    else
                    {
                        _swordCombatComponent.StrongOneHandAttackSword();
                    }
                }
                else if (_altModifier)
                {
                    _swordCombatComponent.OneHandAltAttackSword();
                }
                else
                {
                    _swordCombatComponent.OneHandAttackSword();
                }
            }
        }
    }

    #region MOVEMENT

    private void UpdateInputMovement(ref Vector3 velocity, float delta)
    {
        if (isInAction())
        {
            return;
        }

        _currentInput = Input.GetVector("left", "right", "forward", "backwards");
        // input direction responsive to the direction the player's camera is facing
        _movementDirection = -(_cameraPivot.Transform.Basis * new Vector3(_currentInput.X, 0, _currentInput.Y)).Normalized();
        if (_movementDirection != Vector3.Zero)
        {
            velocity.X = _movementDirection.X * MovementSpeed;
            velocity.Z = _movementDirection.Z * MovementSpeed;
            Vector3 currentNormalizedVelocity = ToLocal(GlobalPosition + velocity);
            _currentInput = new Vector2(currentNormalizedVelocity.X, currentNormalizedVelocity.Z).LimitLength(1);
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, MovementSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MovementSpeed);

            _currentInput = Vector2.Zero;
        }
    }

    private void UpdateRotation(float delta)
    {
        /*
        if (allowVelocityRotation)
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
        else
        {
            RotationDegrees = new Vector3(
                RotationDegrees.X,
                _cameraContainer.RotationDegrees.Y,
                RotationDegrees.Z
            );
        }*/

        
    }

    private bool allowVelocityRotation = true;

    #endregion

    #region COMBAT

    protected override void GatherCombatRequirements()
    {
        base.GatherCombatRequirements();

        _swordCombatComponent.EquippedSword.IsOwnedByPlayer = true;
    }

    #endregion
}
