using Godot;
using System;

public partial class HealthBar : ProgressBar
{
	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}

    public void SetMaxHealth(int maxHealth)
    {
        MaxValue = maxHealth;
        SetHealth(maxHealth);
    }

    public void SetHealth(int health)
    {
        Value = health;
    }
}
