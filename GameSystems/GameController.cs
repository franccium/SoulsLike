using Godot;
using System;

public partial class GameController : Node
{
    public static GameController Instance { get; private set; }

    private Player _player;

	public override void _Ready()
	{
        Instance = this;

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
	{
	}

    public override void _Input(InputEvent inputEvent)
    {
        if (Input.IsActionJustPressed("escape"))
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }

    public void SetPlayer(Player player) => _player = player;

    public Player GetPlayer() => _player;
}
