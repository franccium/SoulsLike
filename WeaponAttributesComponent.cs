using Godot;

public partial class WeaponAttributesComponent : Node
{
    public int Damage { get; set; } = 10;
    public int StaminaDamage { get; set; } = 10;
    public int StaminaCost { get; set; } = 10;
    
    public int BlockingAttackStaminaCost { get; set; } = 10;
    public int BlockedByOtherStaminaCost { get; set; } = 20;

    public float AttackRange { get; set; } = 5f;
}