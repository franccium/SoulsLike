using Godot;
using System;

public partial class GameController : Node
{
	public override void _Ready()
	{
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
}
