using Godot;

public partial class AttributeComponent : Node
{
    public int MaxHealth { get; set; } = 100;
    public int Health { get; set; } = 100;

    public int MaxStamina { get; set; } = 100;
    public int Stamina { get; set; } = 100;
    

    public float MovementSpeed { get; set; } = 5.0f;
}