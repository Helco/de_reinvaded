using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using dere.io.prv;

namespace dere.io
{
    public struct Vector3
    {
        public float x, y, z;
        public float LengthSqr => x*x + y*y + z*z;
        public float Length => (float)Math.Sqrt(LengthSqr);
        public Vector3 Normalized => new Vector3(x / Length, y / Length, z / Length);

        public Vector3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public static Vector3 LoadFromReader(BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float _x, float _y)
        {
            x = _x;
            y = _y;
        }

        public static Vector2 LoadFromReader(BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }

    public struct Quaternion
    {
        public float x, y, z, w;

        public static Quaternion LoadFromReader(BinaryReader reader)
        {
            return new Quaternion
            {
                w = reader.ReadSingle(),
                x = reader.ReadSingle(),
                y = reader.ReadSingle(),
                z = reader.ReadSingle(),
            };
        }

        public static Quaternion FromAxisAngle(Vector3 axis, float angle)
        {
            axis = axis.Normalized;
            float s = (float)Math.Sin(angle / 2.0);
            return new Quaternion
            {
                x = axis.x * s,
                y = axis.y * s,
                z = axis.z * s,
                w = (float)Math.Cos(angle / 2.0)
            };
        }
    }

    public struct Matrix4
    {
        public Vector3 a, b, c, t;

        public static Matrix4 LoadFromReader(BinaryReader reader)
        {
            return new Matrix4
            {
                a = Vector3.LoadFromReader(reader),
                b = Vector3.LoadFromReader(reader),
                c = Vector3.LoadFromReader(reader),
                t = Vector3.LoadFromReader(reader)
            };
        }
    }

    public struct Vertex
    {
        public Vector3 pos;
        public Vector2 uv;
        public int boneIndex;

        public static Vertex LoadFromReader(BinaryReader reader)
        {
            Vertex vertex = new Vertex();
            vertex.pos = Vector3.LoadFromReader(reader);
            vertex.uv = Vector2.LoadFromReader(reader);
            reader.BaseStream.Seek(2, SeekOrigin.Current);
            vertex.boneIndex = reader.ReadUInt16();
            return vertex;
        }
    }

    public struct Normal
    {
        public Vector3 normal;
        public byte boneIndex;

        public static Normal LoadFromReader(BinaryReader reader)
        {
            var result = new Normal
            {
                normal = Vector3.LoadFromReader(reader),
                boneIndex = reader.ReadByte()
            };
            reader.BaseStream.Seek(3, SeekOrigin.Current);
            return result;
        }
    }

    public struct Bone
    {
        public Vector3 bbMin, bbMax;
        public Matrix4 attachmentMatrix;
        public int parentBoneIndex;
        public string name;

        public static Bone LoadFromReader(BinaryReader reader)
        {
            var result = new Bone
            {
                bbMin = Vector3.LoadFromReader(reader),
                bbMax = Vector3.LoadFromReader(reader),
                attachmentMatrix = Matrix4.LoadFromReader(reader),
                parentBoneIndex = reader.ReadInt16()
            };
            reader.BaseStream.Seek(2, SeekOrigin.Current);
            return result;
        }
    }

    public struct Material
    {
        public bool hasBitmap;
        public uint bitmapId;
        public Vector3 color;
        public string name;

        public static Material LoadFromReader(BinaryReader reader)
        {
            uint bitmapId;
            return new Material
            {
                hasBitmap = (bitmapId = reader.ReadUInt32()) != 0,
                bitmapId = bitmapId,
                color = Vector3.LoadFromReader(reader)
            };
        }
    }

    public struct Triangle
    {
        public int[] vertexIndices, normalIndices;
        public int materialIndex;

        public static Triangle LoadFromReader(BinaryReader reader)
        {
            return new Triangle
            {
                vertexIndices = new int[3]
                {
                    reader.ReadInt16(),
                    reader.ReadInt16(),
                    reader.ReadInt16()
                },
                normalIndices = new int[3]
                {
                    reader.ReadInt16(),
                    reader.ReadInt16(),
                    reader.ReadInt16()
                },
                materialIndex = reader.ReadInt16()
            };
        }
    }

    public class GeGeometry
    {
        public Vector3 bbMin, bbMax;
        public Vertex[] vertices = new Vertex[0];
        public Normal[] normals = new Normal[0];
        public Bone[] bones = new Bone[0];
        public Material[] materials = new Material[0];
        public Triangle[][] levels = new Triangle[0][];

        private static T[] readArray<T>(BinaryReader reader, Func<BinaryReader, T> readFunc)
        {
            int count = reader.ReadUInt16();
            return Enumerable.Range(0, count)
                .Select((_) => readFunc(reader))
                .ToArray();
        }

        public static GeGeometry LoadFromStream(Stream stream)
        {
            GeGeometry thiz = new GeGeometry();
            BinaryReader reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != 0x5E444F42)
                throw new InvalidDataException("Invalid geometry signature");
            if (reader.ReadUInt32() != 0xF1)
                throw new InvalidDataException("Unsupported geometry version");
            thiz.bbMin = Vector3.LoadFromReader(reader);
            thiz.bbMax = Vector3.LoadFromReader(reader);

            thiz.vertices = readArray(reader, Vertex.LoadFromReader);
            thiz.normals = readArray(reader, Normal.LoadFromReader);
            thiz.bones = readArray(reader, Bone.LoadFromReader);
            var boneNames = StrBlock.LoadFromReader(reader);
            thiz.materials = readArray(reader, Material.LoadFromReader);
            var materialNames = StrBlock.LoadFromReader(reader);

            thiz.levels = new Triangle[reader.ReadInt32()][];
            for (int i = 0 ; i < thiz.levels.Length; i++)
            {
                thiz.levels[i] = Enumerable.Range(0, reader.ReadInt32())
                    .Select((_) => Triangle.LoadFromReader(reader))
                    .ToArray();
            }

            for (int i = 0; i < thiz.bones.Length; i++)
                thiz.bones[i].name = boneNames[i];
            for (int i = 0; i < thiz.materials.Length; i++)
                thiz.materials[i].name = materialNames[i];

            return thiz;
        }
    }
}
