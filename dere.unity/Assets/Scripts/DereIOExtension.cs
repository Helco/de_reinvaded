using System;

public static class DereIOExtension
{
    public static UnityEngine.Color32 ToUnity(this dere.io.Color c)
    {
        return new UnityEngine.Color32(c.r, c.g, c.b, c.a);
    }

    public static UnityEngine.Color ToUnityColor(this dere.io.Vector3 v)
    {
        return new UnityEngine.Color(v.x, v.y, v.z, 1.0f);
    }

    public static UnityEngine.Vector3 ToUnity(this dere.io.Vector3 v)
    {
        return new UnityEngine.Vector3(v.x, v.y, v.z);
    }

    public static UnityEngine.Vector2 ToUnity(this dere.io.Vector2 v)
    {
        return new UnityEngine.Vector2(v.x, v.y);
    }

    public static UnityEngine.Quaternion ToUnity(this dere.io.Quaternion q)
    {
        return new UnityEngine.Quaternion(q.x, q.y, q.z, q.w);
    }

    public static UnityEngine.Matrix4x4 ToUnity(this dere.io.Matrix4 m)
    {
        return new UnityEngine.Matrix4x4(
            new UnityEngine.Vector4(m.a.x, m.b.x, m.c.x, 0.0f),
            new UnityEngine.Vector4(m.a.y, m.b.y, m.c.y, 0.0f),
            new UnityEngine.Vector4(m.a.z, m.b.z, m.c.z, 0.0f),
            new UnityEngine.Vector4(m.t.x, m.t.y, m.t.z, 1.0f)
        );
    }
}
