using Godot;
using System;

public partial class WeaponHitboxArea : Area3D
{
    public Weapon Weapon { get; set; }

	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}
}
