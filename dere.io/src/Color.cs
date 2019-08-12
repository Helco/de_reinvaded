namespace dere.io
{
    public struct Color
    {
        public byte r, g, b, a;
        public Color (byte _r, byte _g, byte _b, byte _a)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }

        public static bool operator == (Color a, Color b)
        {
            return
                a.r == b.r &&
                a.g == b.g &&
                a.b == b.b &&
                a.a == b.a;
        }

        public static bool operator != (Color a, Color b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Color)
                return this == (Color)obj;
            return false;
        }
    }
}
