using Godot;
using System;

public partial class StaminaBar : Sprite3D
{
    private TextureProgressBar _staminaBar;

    public override void _Ready()
    {
        _staminaBar = GetNode<TextureProgressBar>("SubViewport/StaminaBarProgressBar");
    }

    public override void _Process(double delta)
    {
    }

    public void SetMaxStamina(int maxStamina)
    {
        _staminaBar.MaxValue = maxStamina;
        SetStamina(maxStamina);
    }

    public void SetStamina(int stamina)
    {
        _staminaBar.Value = stamina;
    }
}
