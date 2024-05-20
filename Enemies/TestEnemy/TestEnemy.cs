using Godot;
using System;

public partial class TestEnemy : HumanEnemy
{
    protected override void GatherAttributeRequirements()
    {
        base.GatherAttributeRequirements();

        _attributeComponent.MovementSpeed = 2.5f;
    }
}
