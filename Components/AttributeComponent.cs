using Godot;

public partial class AttributeComponent : Node
{
    public int MaxHealth { get; set; } = 100;
    public int Health { get; set; } = 100;

    public int MaxStamina { get; set; } = 100;
    public int Stamina { get; set; } = 100;
    
    public float MovementSpeed { get; set; } = 5.0f;
    public float SprintSpeed { get; set; } = 8.0f;
    public float BlockingMovementSpeed { get; set; } = 2f;
    public float RollSpeed { get; set; } = 2f;
    public float RollTimeScale { get; set; } = 1.3f;
    public float SideRollTimeScale { get; set; } = 2.7f;

    public float JumpVelocity { get; set; } = 5.0f;


    public float GetHealthPercentage() => (float)Health / MaxHealth;

    public float GetStaminaPercentage() => (float)Stamina / MaxStamina;
}