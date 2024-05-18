using Godot;
using System;

public partial class Player : CharacterBody3D
{
    private Node3D _cameraContainer;
    
    private AnimationTree _animationTree;
    private const float TransitionSpeed = 0.1f;
    private const float PlayerSpeed = 5.0f;
    private const float RotationSpeed = 10;
    private const float JumpVelocity = 4.5f;

    public bool allowVelocityRotation = true;

    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private Vector2 currentInput = Vector2.Zero;
    private Vector2 currentVelocity = Vector2.Zero;

    private bool jumpQueued = false;
    private bool falling = false;


    private string _locomotionBlendPath = "parameters/LocomotionStateMachine";
    private string _locomotionStatePlaybackPath = "parameters/LocomotionStateMachine/playback";
    private string _jumpStateName = "jump";
    private string _fallingStateName = "fall";
    private string _walkingStateName = "walk";

    private string _upperBodyBlendPath = "parameters/UpperBodyStateMachine";
    private string _upperBodyStatePlaybackPath = "parameters/UpperBodyStateMachine/playback";
    
    private string sheathWeaponStateName = "sword_sheath_2";

    public override void _Ready()
    {
        _cameraContainer = GetNode<Node3D>("CameraController/CameraContainer");
        _animationTree = GetNode<AnimationTree>("AnimationTree");
    }

    public override void _Process(double delta)
    {
        Vector2 newDelta = currentInput - currentVelocity;
        if (newDelta.Length() > TransitionSpeed * (float)delta)
        {
            newDelta = newDelta.Normalized() * TransitionSpeed * (float)delta;
        }
        currentVelocity += newDelta;

        _animationTree.Set(_locomotionBlendPath, currentVelocity);
    }

    public override void _Input(InputEvent @event)
    {
        if (IsOnFloor())
        {
            if(Input.IsActionJustPressed("jump"))
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
            var playback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_upperBodyStatePlaybackPath);
            playback.Travel(sheathWeaponStateName);
        }

        if (Input.IsActionJustPressed("toggle_two_hand"))
        {
        }
    }

    private void DodgeRoll()
    {
        GD.Print("Dodge Roll");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y -= gravity * (float)delta;
            jumpQueued = false;
            if (!falling)
            {
                falling = true;
                var playback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_locomotionStatePlaybackPath);
                playback.Travel(_fallingStateName);
            }
        }
        else if (falling)
        {
            falling = false;
            var playback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_locomotionStatePlaybackPath);
            playback.Travel(_walkingStateName);
        }

        // Putting it after the falling handler makes sure that the transition doesn't
        // automatically force it into a falling animation instead of letting the jump animation
        // naturally finish.

        if (jumpQueued)
        {
            velocity.Y = JumpVelocity;
            jumpQueued = false;
            falling = true;
        }

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        currentInput = Input.GetVector("left", "right", "forward", "backwards");
        Vector3 direction = (_cameraContainer.Transform.Basis * new Vector3(currentInput.X, 0, currentInput.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * PlayerSpeed;
            velocity.Z = direction.Z * PlayerSpeed;
            Vector3 currentNormalizedVelocity = ToLocal(GlobalPosition + velocity);
            currentInput = new Vector2(currentNormalizedVelocity.X, currentNormalizedVelocity.Z).LimitLength(1);
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, PlayerSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, PlayerSpeed);

            currentInput = Vector2.Zero;
        }

        Velocity = velocity;

        if (allowVelocityRotation)
        {
            if (Velocity.Length() > 0.1f)
            {
                RotationDegrees = new Vector3(
                    RotationDegrees.X,
                    Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(RotationDegrees.Y), Mathf.Atan2(-Velocity.X, -Velocity.Z), (float)delta * RotationSpeed)),
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
        }

        MoveAndSlide();
    }

    public void DisableVelocityRotation()
    {
        allowVelocityRotation = false;
    }

    public void EnableVelocityRotation()
    {
        allowVelocityRotation = true;
    }

    private void BeginJump()
    {
        var playback = (AnimationNodeStateMachinePlayback)_animationTree.Get(_locomotionStatePlaybackPath);
        playback.Travel(_jumpStateName);
    }

    public void ExecuteJumpVelocity()
    {
        jumpQueued = true;
    }
}
