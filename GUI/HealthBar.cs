using Godot;
using System;

public partial class HealthBar : Sprite3D
{
    private TextureProgressBar _healthBar;

	public override void _Ready()
	{
        _healthBar = GetNode<TextureProgressBar>("SubViewport/HealthBarProgressBar");
	}

	public override void _Process(double delta)
	{
	}

    public void SetMaxHealth(int maxHealth)
    {
        _healthBar.MaxValue = maxHealth;
        SetHealth(maxHealth);
    }

    public void SetHealth(int health)
    {
        _healthBar.Value = health;
    }
}
