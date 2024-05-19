using Godot;
using System;

public partial class Sword : Node3D
{
    private MeshInstance3D _sheathMesh;
    private MeshInstance3D _sheathMesh2;

    private Area3D _hitboxArea;

    public bool IsOwnedByPlayer = false;

	public override void _Ready()
	{
        _sheathMesh = GetNode<MeshInstance3D>("Sheath1");
        _sheathMesh2 = GetNode<MeshInstance3D>("Sheath2");

        _hitboxArea = GetNode<Area3D>("HitboxArea");

        _hitboxArea.BodyEntered += OnHitboxAreaBodyEntered;
    }

	public override void _Process(double delta)
	{
	}

    public void DrawWeapon()
    {
        _sheathMesh.Visible = false;
        _sheathMesh2.Visible = false;
    }

    public void SheathWeapon()
    {
        _sheathMesh.Visible = true;
        _sheathMesh2.Visible = true;
    }

    public void OnHitboxAreaBodyEntered(Node3D body)
    {
        if(body is Sword sword)
        {
            GD.Print("Sword hit sword");
        }
        else
        {
            if(IsOwnedByPlayer)
            {
                if(body is HumanEnemy enemy)
                {
                    GD.Print("Sword hit enemy");
                    enemy.Stagger();
                }
            }
            else
            {
                if(body is Player player)
                {
                    GD.Print("Sword hit player");
                    player.Stagger();
                }
            }
        }
    }
}
