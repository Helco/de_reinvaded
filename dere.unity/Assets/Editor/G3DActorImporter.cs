using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using dere.io;
using System;

internal class G3DActor_BitmapFactory : dere.io.IBitmapFactory
{
    public IBitmap CreateNew(int width, int height)
    {
        return new G3DActor_Bitmap(width, height);
    }
}

internal class G3DActor_Bitmap : IBitmap
{
    private Texture2D texture;
    public Texture2D Texture
    {
        get
        {
            texture.Apply(true, true);
            return texture;
        }
    }

    public G3DActor_Bitmap(int width, int height)
    {
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
    }

    public void SetPixel(int x, int y, dere.io.Color c)
    {
        texture.SetPixel(x, y, new Color32(c.r, c.g, c.b, c.a));
    }
}

[ScriptedImporter(1, new string[] { "act" })]
public class G3DActorImporter : ScriptedImporter
{
    public UnityEngine.Material material = null;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        try
        {
             FileStream fileStream = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read);
            var rootVFile = dere.io.VFile.LoadFromStream(fileStream);
            verifyHeader(ctx, rootVFile);

            var bodyVFile = dere.io.VFile.LoadFromStream(rootVFile.OpenFile("Body"));
            GeGeometry geo = GeGeometry.LoadFromStream(bodyVFile.OpenFile("Geometry"));
            Texture2D[] textures = loadTextures(ctx, bodyVFile);
            createMainPrefab(ctx, geo, textures);
        }
        catch (Exception e)
        {
            ctx.LogImportError(e.Message);
            return;
        }
    }

    private void createMainPrefab(AssetImportContext ctx, GeGeometry geo, Texture2D[] textures)
    {
        GameObject root = new GameObject("");

        var ren = root.AddComponent<SkinnedMeshRenderer>();
        ren.sharedMesh = convertMesh(ctx, geo);
        ren.sharedMaterials = convertMaterials(ctx, geo, textures);
        createSkeleton(ctx, geo, ren);

        ctx.AddObjectToAsset("Main", root);
        ctx.SetMainObject(root);
    }

    private void verifyHeader(AssetImportContext ctx, VFile rootVFile)
    {
        // discard result, only look out for exceptions
        ActorHeader.LoadFromStream(rootVFile.OpenFile("Header"));
    }

    private Mesh convertMesh(AssetImportContext ctx, GeGeometry geo)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = geo.vertices.Select(v => v.pos.ToUnity()).ToArray();

        var normals = Enumerable.Repeat(UnityEngine.Vector3.zero, mesh.vertexCount).ToArray();
        foreach (var t in geo.levels[0])
        {
            for (int i = 0; i < 3; i++)
            {
                var normal = geo.normals[t.normalIndices[i]].normal.ToUnity();
                var vertexI = t.vertexIndices[i];
                if (normals[vertexI].sqrMagnitude == 0.0f)
                    normals[vertexI] = normal;
                else
                    normals[vertexI] = (normals[vertexI] + normal) * 0.5f;
            }
        }
        mesh.normals = normals;

        mesh.uv = geo.vertices.Select(v => v.uv.ToUnity()).ToArray();

        mesh.bindposes = geo.bones.Select(b => b.attachmentMatrix.ToUnity()).ToArray();

        mesh.boneWeights = geo.vertices.Select(v => new BoneWeight {
            boneIndex0 = v.boneIndex,
            weight0 = 1.0f
        }).ToArray();

        mesh.subMeshCount = geo.materials.Length;
        var subMeshes = geo.levels[0].GroupBy(t => t.materialIndex);
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = subMeshes
                .Where(g => g.Key == i)
                .SelectMany(g => g.SelectMany(t => new int[] {
                    t.vertexIndices[0],
                    t.vertexIndices[1],
                    t.vertexIndices[2]
                })).ToArray();
            mesh.SetTriangles(triangles, i, true);
        }

        ctx.AddObjectToAsset("Geometry", mesh);
        return mesh;
    }

    private Transform[] createSkeleton(AssetImportContext ctx, GeGeometry geo, SkinnedMeshRenderer ren)
    {
        Transform[] boneObjects = Enumerable.Repeat(0, geo.bones.Length)
            .Select((_) => new GameObject().transform).ToArray();
        for (int i = 0; i < geo.bones.Length; i++) {
            boneObjects[i].name = geo.bones[i].name;
            if (geo.bones[i].parentBoneIndex >= 0) {
                boneObjects[i].parent = boneObjects[geo.bones[i].parentBoneIndex];
                boneObjects[i].gameObject.AddComponent<BoneGizmo>();
            }
            boneObjects[i].localPosition = ren.sharedMesh.bindposes[i].GetColumn(3);
            boneObjects[i].localRotation = ren.sharedMesh.bindposes[i].rotation;
            ren.sharedMesh.bindposes[i] = Matrix4x4.identity;
        }

        ren.bones = boneObjects;
        ren.rootBone = boneObjects.Single(b => b.parent == null);
        ren.rootBone.parent = ren.transform;
        return boneObjects;
    }

    private Texture2D[] loadTextures(AssetImportContext ctx, VFile bodyVFile)
    {
        var textures = Enumerable.Empty<Texture2D>();
        var bitmapFactory = new G3DActor_BitmapFactory();
        var textureNames = bodyVFile.FileNames
            .Where(f => f.StartsWith("Bitmaps/"))
            .OrderBy(s => s);
        foreach (var filename in textureNames) {
            var texture = (GeBitmap.LoadFromStream(bodyVFile.OpenFile(filename), bitmapFactory)[0] as G3DActor_Bitmap).Texture;
            ctx.AddObjectToAsset(filename.Replace("/", ""), texture);
            textures = textures.Append(texture);
        }
        return textures.ToArray();
    }

    private UnityEngine.Material[] convertMaterials(AssetImportContext ctx, GeGeometry geo, Texture2D[] textures)
    {
        return geo.materials.Select((dereMat, i) => {
            var unityMat = new UnityEngine.Material(material);
            unityMat.name = dereMat.name;
            unityMat.mainTexture = textures[i];
            ctx.AddObjectToAsset(dereMat.name, unityMat);
            return unityMat;
        }).ToArray();
    }
}
