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
        if (material == null)
            material = new UnityEngine.Material(Shader.Find("Standard"));

        FileStream fileStream = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read);
        var rootVFile = dere.io.VFile.LoadFromStream(fileStream);
        verifyHeader(ctx, rootVFile);

        var bodyVFile = dere.io.VFile.LoadFromStream(rootVFile.OpenFile("Body"));
        GeGeometry geo = GeGeometry.LoadFromStream(bodyVFile.OpenFile("Geometry"));
        Texture2D[] textures = loadTextures(ctx, bodyVFile);
        GeMotion[] motions = loadMotions(ctx, rootVFile);
        var rootObject = createMainPrefab(ctx, geo, textures);
        var clips = createAnimations(ctx, geo, motions);
        createAnimator(ctx, rootObject, clips);
    }

    private GameObject createMainPrefab(AssetImportContext ctx, GeGeometry geo, Texture2D[] textures)
    {
        GameObject root = new GameObject("");

        var ren = root.AddComponent<SkinnedMeshRenderer>();
        ren.sharedMesh = convertMesh(ctx, geo);
        ren.sharedMaterials = convertMaterials(ctx, geo, textures);
        createSkeleton(ctx, geo, ren);

        ctx.AddObjectToAsset("Main", root);
        ctx.SetMainObject(root);
        return root;
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

        mesh.uv2 = geo.vertices.Select(v => v.boneIndex).Select(i => new UnityEngine.Vector2(i, i)).ToArray();

        mesh.bindposes = geo.bones.Select(b => b.attachmentMatrix.ToUnity()).ToArray();

        mesh.boneWeights = geo.vertices.Select(v => new BoneWeight {
            boneIndex0 = v.boneIndex,
            weight0 = 1.0f,
            weight1 = 0.0f,
            weight2 = 0.0f,
            weight3 = 0.0f
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

    private void createSkeleton(AssetImportContext ctx, GeGeometry geo, SkinnedMeshRenderer ren)
    {
        Transform[] boneObjects = Enumerable
            .Repeat(0, geo.bones.Length + 1) // we need to add a custom root bone
            .Select((_) => new GameObject().transform)
            .ToArray();
        for (int i = 0; i < geo.bones.Length; i++) {
            boneObjects[i].name = geo.bones[i].name;
            if (geo.bones[i].parentBoneIndex >= 0) {
                boneObjects[i].parent = boneObjects[geo.bones[i].parentBoneIndex];
                boneObjects[i].gameObject.AddComponent<BoneGizmo>();
            }
            boneObjects[i].localPosition = ren.sharedMesh.bindposes[i].GetColumn(3);
            boneObjects[i].localRotation = ren.sharedMesh.bindposes[i].rotation.normalized;
        }

        var prevPositions = boneObjects.Select(b => b.position).ToArray();
        ren.sharedMesh.bindposes = geo.bones
            .Select((b,i) => Matrix4x4.Rotate(boneObjects[i].localToWorldMatrix.rotation))
            .Append(Matrix4x4.identity) // no transform on the rot bone
            .ToArray();
        for (int i = 0; i < geo.bones.Length; i++) {
            boneObjects[i].localRotation = UnityEngine.Quaternion.identity;
            boneObjects[i].position = prevPositions[i];
        }
        ren.bones = boneObjects;

        // add the (perhabs multiple) root bones to our real one
        ren.rootBone = boneObjects.Last();
        ren.rootBone.name = "RootBone";
        ren.rootBone.parent = ren.transform;
        foreach(var subRootBone in boneObjects.Where(t => t.parent == null && t != ren.rootBone))
            subRootBone.parent = ren.rootBone;
        ren.rootBone.transform.Rotate(-90.0f, 0.0f, 0.0f, Space.World);
    }

    private Texture2D[] loadTextures(AssetImportContext ctx, VFile bodyVFile)
    {
        var textures = new List<Texture2D>();
        var bitmapFactory = new G3DActor_BitmapFactory();
        var textureNames = bodyVFile.FileNames
            .Where(f => f.StartsWith("Bitmaps/"))
            .OrderBy(s => Int32.Parse(s.Substring("Bitmaps/".Length)));
        foreach (var filename in textureNames) {
            var texture = (GeBitmap.LoadFromStream(bodyVFile.OpenFile(filename), bitmapFactory)[0] as G3DActor_Bitmap).Texture;
            ctx.AddObjectToAsset(filename.Replace("/", ""), texture);
            textures.Add(texture);
        }
        return textures.ToArray();
    }

    private UnityEngine.Material[] convertMaterials(AssetImportContext ctx, GeGeometry geo, Texture2D[] textures)
    {
        int textureI = 0;
        return geo.materials.Select((dereMat, i) => {
            var unityMat = new UnityEngine.Material(material);
            unityMat.name = dereMat.name;
            if (dereMat.hasBitmap)
                unityMat.mainTexture = textures[textureI++];
            ctx.AddObjectToAsset(dereMat.name, unityMat);
            return unityMat;
        }).ToArray();
    }

    private GeMotion[] loadMotions(AssetImportContext ctx, VFile rootVFile)
    {
        return rootVFile.FileNames
            .Where(f => f.StartsWith("Motions/"))
            .OrderBy(s => Int32.Parse(s.Substring("Motions/".Length)))
            .Select(rootVFile.OpenFile)
            .Select(GeMotion.LoadFromStream)
            .ToArray();
    }

    private AnimationClip[] createAnimations(AssetImportContext ctx, GeGeometry geo, GeMotion[] motions)
    {
        return motions.Select(m => convertAnimation(ctx, geo, m)).ToArray();
    }

    private AnimationClip convertAnimation(AssetImportContext ctx, GeGeometry geo, GeMotion motion)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = motion.name;
        foreach (GeMotionPath path in motion.paths)
            convertAnimationPathTo(ctx, geo, path, clip);
        ctx.AddObjectToAsset(clip.name, clip);
        return clip;
    }

    private void convertAnimationPathTo(AssetImportContext ctx, GeGeometry geo, GeMotionPath motionPath, AnimationClip clip)
    {
        // Find full transform path from root bone
        var boneChain = new List<string>() { motionPath.name };
        Bone curBone = geo.bones.Single(b => b.name == motionPath.name);
        while (curBone.parentBoneIndex >= 0)
        {
            curBone = geo.bones[curBone.parentBoneIndex];
            boneChain.Add(curBone.name);
        }
        boneChain.Add("RootBone");
        var transformPath = String.Join("/", (boneChain as IEnumerable<string>).Reverse());

        if (motionPath.vkFrames != null)
        {
            //TODO: Add Interpolation type
            var vectorFrames = motionPath.vkFrames.frameTimes.Zip(motionPath.vkFrames.frames, (time, value) => (time, value));
            var xKeyFrames = vectorFrames.Select(t => new Keyframe(t.time, t.value.x)).ToArray();
            var yKeyFrames = vectorFrames.Select(t => new Keyframe(t.time, t.value.y)).ToArray();
            var zKeyFrames = vectorFrames.Select(t => new Keyframe(t.time, t.value.z)).ToArray();
            clip.SetCurve(transformPath, typeof(Transform), "localPosition.x", new AnimationCurve(xKeyFrames));
            clip.SetCurve(transformPath, typeof(Transform), "localPosition.y", new AnimationCurve(yKeyFrames));
            clip.SetCurve(transformPath, typeof(Transform), "localPosition.z", new AnimationCurve(zKeyFrames));
        }

        if (motionPath.qkFrames != null)
        {
            //TODO Add Interpolation type
            var quatFrames = motionPath.qkFrames.frameTimes.Zip(motionPath.qkFrames.frames, (time, value) => (time, value));
            var xKeyFrames = quatFrames.Select(t => new Keyframe(t.time, t.value.x)).ToArray();
            var yKeyFrames = quatFrames.Select(t => new Keyframe(t.time, t.value.y)).ToArray();
            var zKeyFrames = quatFrames.Select(t => new Keyframe(t.time, t.value.z)).ToArray();
            var wKeyFrames = quatFrames.Select(t => new Keyframe(t.time, t.value.w)).ToArray();
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.x", new AnimationCurve(xKeyFrames));
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.y", new AnimationCurve(yKeyFrames));
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.z", new AnimationCurve(zKeyFrames));
            clip.SetCurve(transformPath, typeof(Transform), "localRotation.w", new AnimationCurve(wKeyFrames));
        }
    }

    private void createAnimator(AssetImportContext ctx, GameObject rootObject, AnimationClip[] clips)
    {
        var avatar = AvatarBuilder.BuildGenericAvatar(rootObject, "RootBone");
        ctx.AddObjectToAsset("Avatar_" + rootObject.name, avatar);

        var controller = new UnityEditor.Animations.AnimatorController();
        ctx.AddObjectToAsset("AniController_" + rootObject.name, controller);

        var animator = rootObject.AddComponent<Animator>();
        animator.avatar = avatar;
    }
}
