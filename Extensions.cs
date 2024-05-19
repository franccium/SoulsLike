using Godot;

public static class Extensions
{
    public static void SwitchNodeOwnership(this Node3D node, Node3D newParent)
    {
        node.GetParent().RemoveChild(node);
        newParent.AddChild(node);
        node.ResetTransform();
    }

    public static void ResetTransform(this Node3D node)
    {
        node.Position = Vector3.Zero;
        node.RotationDegrees = Vector3.Zero;
    }
}