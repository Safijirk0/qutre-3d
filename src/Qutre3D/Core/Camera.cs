using OpenTK.Mathematics;

namespace Qutre3D.Core;

/// <summary>
/// Орбитальная камера: цель, дистанция, рыскание/тангаж.
/// Всё просто и предсказуемо — идеально, чтобы потом прикрутить полёт/дорожки.
/// </summary>
public sealed class Camera
{
    public Vector3 Target { get; set; } = new(0f, 8f, 0f);
    public float Distance { get; set; } = 122f;
    public float Yaw { get; set; } = 35f;
    public float Pitch { get; set; } = 16f;
    public float Fov { get; set; } = 47f;
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 6000f;

    public Vector3 Position
    {
        get
        {
            float yaw = MathHelper.DegreesToRadians(Yaw);
            float pitch = MathHelper.DegreesToRadians(Pitch);
            var offset = new Vector3(
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch),
                MathF.Cos(pitch) * MathF.Cos(yaw));
            return Target + offset * Distance;
        }
    }

    public Vector3 Forward => Vector3.Normalize(Target - Position);

    public Matrix4 View => Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public Matrix4 Projection(float aspect) =>
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), MathF.Max(aspect, 0.0001f), Near, Far);

    public Matrix4 ViewProjection(float aspect) => View * Projection(aspect);

    public void Orbit(float dx, float dy)
    {
        Yaw -= dx * 0.32f;
        Pitch = Math.Clamp(Pitch + dy * 0.28f, -88f, 86f);
        Yaw = (Yaw % 360f + 360f) % 360f;
    }

    public void Pan(float dx, float dy)
    {
        Vector3 forward = Forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));
        float speed = Distance * 0.0016f;
        Target += (-right * dx + up * dy) * speed;
    }

    public void Zoom(float steps)
    {
        Distance = Math.Clamp(Distance * MathF.Pow(0.88f, steps), 1.5f, 1200f);
    }

    /// <summary>Кадрировать точку с учётом радиуса.</summary>
    public void Frame(Vector3 center, float radius)
    {
        Target = center;
        Distance = Math.Clamp(radius * 2.6f + 6f, 4f, 900f);
        Pitch = Math.Clamp(Pitch, 5f, 35f);
    }

    /// <summary>Луч из экранных координат (ndc: -1..1, Y вверх).</summary>
    public (Vector3 Origin, Vector3 Direction) Ray(Vector2 ndc, float aspect)
    {
        Matrix4 inv = Matrix4.Invert(ViewProjection(aspect));
        Vector4 near = Vector4.TransformRow(new Vector4(ndc.X, ndc.Y, -1f, 1f), inv);
        Vector4 far = Vector4.TransformRow(new Vector4(ndc.X, ndc.Y, 1f, 1f), inv);

        Vector3 p0 = new(near.X / near.W, near.Y / near.W, near.Z / near.W);
        Vector3 p1 = new(far.X / far.W, far.Y / far.W, far.Z / far.W);
        return (p0, Vector3.Normalize(p1 - p0));
    }
}
