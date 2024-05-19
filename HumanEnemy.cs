using Godot;
using System;

public partial class HumanEnemy : Human
{
    private Player _player;

    public override void _Ready()
    {
        base._Ready();

        _player = GameController.Instance.GetPlayer();
    }

    public override void _Process(double delta)
    {
        // follow player

        base._Process(delta);
    }
}
