using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public static class CharacterBuilder {
    [Serializable] class Data { public Part[] meshes; public Mat[] materials; }
    [Serializable] class Part { public string name; public int material, vertices, indices, offset; }
    [Serializable] class Mat { public string name; public int texture; public float[] color; }
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    [MenuItem("Teja's Impact/Rebuild Eula rig and animations")]
    public static void Build() {
        const string folder = "Assets/Resources/Eula";
        Directory.CreateDirectory(folder); Directory.CreateDirectory(folder + "/Animations"); AssetDatabase.Refresh();
        var data = JsonUtility.FromJson<Data>(File.ReadAllText("Assets/CharacterSource/eula.json"));
        var root = new GameObject("Eula — user supplied model");
        var rig = root.AddComponent<TejaRig>();
        int[] parents = {-1, 0, 1, 2, 3, 2, 5, 6, 2, 8, 9, 0, 11, 12, 0, 14, 15, 2, 17, 18};
        // Joint centers measured against the supplied A-pose, in converted world coordinates.
        Vector3[] pos = {
            V(0, 1.16f, 0), V(0, 1.37f, 0), V(0, 1.63f, 0), V(0, 1.815f, .015f), V(0, 1.935f, .03f),
            V(-.225f, 1.735f, .01f), V(-.441f, 1.545f, .01f), V(-.627f, 1.382f, .015f),
            V(.225f, 1.735f, .01f), V(.441f, 1.545f, .01f), V(.627f, 1.382f, .015f),
            V(-.105f, 1.15f, 0), V(-.117f, .61f, .012f), V(-.12f, .135f, .018f),
            V(.105f, 1.15f, 0), V(.117f, .61f, .012f), V(.12f, .135f, .018f),
            V(.20f, 1.73f, -.12f), V(.24f, 1.28f, -.205f), V(.29f, .80f, -.29f)
        };
        rig.bones = new Transform[pos.Length]; rig.restPositions = new Vector3[pos.Length];
        for (int i = 0; i < pos.Length; i++) {
            var bone = new GameObject(TejaRig.Names[i]).transform;
            bone.SetParent(parents[i] < 0 ? root.transform : rig.bones[parents[i]], false);
            bone.position = pos[i]; rig.bones[i] = bone; rig.restPositions[i] = bone.localPosition;
        }
        rig.socket = new GameObject("WeaponSocket").transform;
        rig.socket.SetParent(rig.bones[10], false);
        rig.socket.localPosition = V(.057f, -.038f, .020f);
        var materials = new Material[data.materials.Length];
        for (int i = 0; i < materials.Length; i++) {
            var src = data.materials[i]; var m = new Material(Shader.Find("Teja/CharacterToon")); m.name = src.name;
            m.color = new Color(src.color[0], src.color[1], src.color[2], src.color[3]);
            if (src.texture >= 0) {
                string p = "Assets/CharacterSource/texture-" + src.texture + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(p);
                importer.alphaIsTransparency = true; importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            Save(m, folder + "/material-" + i + ".mat");
            materials[i] = AssetDatabase.LoadAssetAtPath<Material>(folder + "/material-" + i + ".mat");
        }
        int vertexCount = 0;
        using (var reader = new BinaryReader(File.OpenRead("Assets/CharacterSource/eula-mesh.bytes"))) {
            for (int part = 0; part < data.meshes.Length; part++) {
                var src = data.meshes[part]; reader.BaseStream.Position = src.offset;
                var vertices = new Vector3[src.vertices]; var normals = new Vector3[src.vertices];
                var uvs = new Vector2[src.vertices]; var weights = new BoneWeight[src.vertices];
                for (int i = 0; i < src.vertices; i++) {
                    vertices[i] = V(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    normals[i] = V(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    uvs[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    weights[i] = Weight(vertices[i], src.name, pos);
                    Validate(weights[i], pos.Length, src.name, i);
                }
                int[] triangles = new int[src.indices];
                for (int i = 0; i < triangles.Length; i++) triangles[i] = reader.ReadInt32();
                var mesh = new Mesh { name = src.name };
                mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uvs; mesh.triangles = triangles; mesh.boneWeights = weights;
                var bind = new Matrix4x4[pos.Length];
                for (int i = 0; i < pos.Length; i++) bind[i] = rig.bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
                mesh.bindposes = bind; mesh.RecalculateBounds(); Save(mesh, folder + "/mesh-" + part + ".asset");
                var child = new GameObject(src.name); child.transform.SetParent(root.transform, false);
                var renderer = child.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/mesh-" + part + ".asset");
                renderer.sharedMaterial = materials[src.material]; renderer.bones = rig.bones; renderer.rootBone = rig.bones[0];
                renderer.localBounds = new Bounds(V(0, 1.1f, 0), V(3, 3.5f, 3));
                renderer.updateWhenOffscreen = false; renderer.quality = SkinQuality.Bone2;
                vertexCount += src.vertices;
            }
        }
        rig.animationPlayer = root.AddComponent<Animation>();
        // All clips are prototype motions authored here; the supplied source model was unrigged.
        string[] states = {"Idle", "Run", "Sprint", "GunIdle", "GunRun", "Jump", "Land", "Sword", "Sword2", "Sword3", "Shoot", "Reload", "Dodge", "Hurt", "Death"};
        foreach (string state in states) {
            var clip = new AnimationClip { name = state, legacy = true, frameRate = 60, wrapMode = TejaRig.Loops(state) ? WrapMode.Loop : WrapMode.ClampForever };
            float duration = TejaRig.Duration(state); int frames = Mathf.CeilToInt(duration * 60);
            var curves = new AnimationCurve[pos.Length, 4]; var hipCurves = new AnimationCurve[3];
            for (int b = 0; b < pos.Length; b++) for (int c = 0; c < 4; c++) curves[b, c] = new AnimationCurve();
            for (int c = 0; c < 3; c++) hipCurves[c] = new AnimationCurve();
            Quaternion[] initialRotations = new Quaternion[pos.Length];
            for (int f = 0; f <= frames; f++) {
                float t = duration * f / frames;
                // Sampling the first pose at the loop endpoint makes position and rotation seams exact.
                rig.Pose(state, TejaRig.Loops(state) && f == frames ? 0 : t);
                for (int b = 0; b < pos.Length; b++) {
                    Quaternion q = rig.bones[b].localRotation;
                    if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w))
                        throw new InvalidOperationException("Invalid joint rotation: " + state + "/" + TejaRig.Names[b]);
                    if (f == 0) initialRotations[b] = q;
                    if (TejaRig.Loops(state) && f == frames && Quaternion.Angle(initialRotations[b], q) > .01f)
                        throw new InvalidOperationException("Animation loop mismatch: " + state);
                    curves[b, 0].AddKey(t, q.x); curves[b, 1].AddKey(t, q.y);
                    curves[b, 2].AddKey(t, q.z); curves[b, 3].AddKey(t, q.w);
                }
                for (int c = 0; c < 3; c++) hipCurves[c].AddKey(t, rig.bones[0].localPosition[c]);
            }
            for (int b = 0; b < pos.Length; b++) {
                string path = AnimationUtility.CalculateTransformPath(rig.bones[b], root.transform);
                for (int c = 0; c < 4; c++) {
                    LinearTangents(curves[b, c]);
                    clip.SetCurve(path, typeof(Transform), "localRotation." + "xyzw"[c], curves[b, c]);
                }
            }
            string hipPath = AnimationUtility.CalculateTransformPath(rig.bones[0], root.transform);
            for (int c = 0; c < 3; c++) { LinearTangents(hipCurves[c]); clip.SetCurve(hipPath, typeof(Transform), "localPosition." + "xyz"[c], hipCurves[c]); }
            clip.EnsureQuaternionContinuity(); Save(clip, folder + "/Animations/" + state + ".anim");
            rig.animationPlayer.AddClip(AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/Animations/" + state + ".anim"), state);
        }
        for (int i = 0; i < rig.bones.Length; i++) { rig.bones[i].localRotation = Quaternion.identity; rig.bones[i].localPosition = rig.restPositions[i]; }
        rig.animationPlayer.clip = rig.animationPlayer.GetClip("Idle"); rig.animationPlayer.playAutomatically = true;
        PrefabUtility.SaveAsPrefabAsset(root, folder + "/Eula.prefab"); UnityEngine.Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
        Debug.Log("TEJA_EULA_READY: " + data.meshes.Length + " skinned meshes; " + vertexCount + " normalized vertices; 20 bones; " + states.Length + " authored clips.");
    }
    static void LinearTangents(AnimationCurve curve) {
        for (int i = 0; i < curve.length; i++) {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }
    static void Save(UnityEngine.Object obj, string path) {
        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (existing) { EditorUtility.CopySerialized(obj, existing); UnityEngine.Object.DestroyImmediate(obj); }
        else AssetDatabase.CreateAsset(obj, path);
    }
    static BoneWeight Single(int bone) { return new BoneWeight { boneIndex0 = bone, weight0 = 1 }; }
    static BoneWeight Blend(int a, int b, float value) {
        float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
        return new BoneWeight { boneIndex0 = a, boneIndex1 = b, weight0 = 1 - t, weight1 = t };
    }
    static BoneWeight Torso(float y) {
        if (y < 1.24f) return Single(0);
        if (y < 1.40f) return Blend(0, 1, Mathf.InverseLerp(1.24f, 1.40f, y));
        if (y < 1.60f) return Blend(1, 2, Mathf.InverseLerp(1.40f, 1.60f, y));
        if (y < 1.76f) return Single(2);
        if (y < 1.83f) return Blend(2, 3, Mathf.InverseLerp(1.76f, 1.83f, y));
        if (y < 1.90f) return Blend(3, 4, Mathf.InverseLerp(1.83f, 1.90f, y));
        return Single(4);
    }
    static BoneWeight Cape(float y) {
        if (y >= 1.66f) return Single(2);
        if (y > 1.52f) return Blend(2, 17, Mathf.InverseLerp(1.66f, 1.52f, y));
        if (y > 1.13f) return Blend(17, 18, Mathf.InverseLerp(1.52f, 1.13f, y));
        return Blend(18, 19, Mathf.InverseLerp(1.13f, .68f, y));
    }
    static BoneWeight Arm(Vector3 p, Vector3[] bones) {
        int upper = p.x < 0 ? 5 : 8;
        Vector3 axis = (bones[upper + 2] - bones[upper]).normalized;
        float along = Vector3.Dot(p - bones[upper], axis);
        float elbow = Vector3.Dot(bones[upper + 1] - bones[upper], axis);
        float wrist = Vector3.Dot(bones[upper + 2] - bones[upper], axis);
        if (along < .07f) return Blend(2, upper, Mathf.InverseLerp(-.025f, .07f, along));
        if (along < elbow - .055f) return Single(upper);
        if (along < elbow + .055f) return Blend(upper, upper + 1, Mathf.InverseLerp(elbow - .055f, elbow + .055f, along));
        if (along < wrist - .04f) return Single(upper + 1);
        return Blend(upper + 1, upper + 2, Mathf.InverseLerp(wrist - .04f, wrist + .035f, along));
    }
    static BoneWeight Leg(Vector3 p) {
        int thigh = p.x < 0 ? 11 : 14;
        if (p.y > 1.07f) return Blend(0, thigh, Mathf.InverseLerp(1.19f, 1.07f, p.y));
        if (p.y > .68f) return Single(thigh);
        if (p.y > .54f) return Blend(thigh, thigh + 1, Mathf.InverseLerp(.68f, .54f, p.y));
        if (p.y > .19f) return Single(thigh + 1);
        return Blend(thigh + 1, thigh + 2, Mathf.InverseLerp(.19f, .09f, p.y));
    }
    static BoneWeight Weight(Vector3 p, string name, Vector3[] bones) {
        if (name.Contains("Face") || name.Contains("Hair") || name.Contains("Iris") || name.Contains("Mouth") || name.Contains("Eye")) return Single(4);
        // The long common mesh-name prefix also contains Cape: match the actual part suffix.
        if (name.Contains("_21_+Cape") || name.Contains("_10_+Cape.CapeMets")) return Cape(p.y);
        if (name.Contains("_10_+Cape") || name.Contains("_10_+Vision") || name.Contains("_10_+Tie")) return Torso(p.y);
        if (name.Contains("Hands")) return Arm(p, bones);
        if (name.Contains("Boots")) return Leg(p);
        // A plain x/y cutoff also catches the waist and ribs. Test the actual A-pose arm
        // capsule so a bent elbow cannot pull torso-side vertices out with the sleeve.
        int shoulder = p.x < 0 ? 5 : 8;
        Vector3 axis = bones[shoulder + 2] - bones[shoulder];
        float along = Mathf.Clamp01(Vector3.Dot(p - bones[shoulder], axis) / axis.sqrMagnitude);
        float armDistance = Vector3.Distance(p, bones[shoulder] + along * axis);
        if (p.y > 1.26f && p.y < 1.80f && Mathf.Abs(p.x) > .205f && armDistance < .145f) return Arm(p, bones);
        if (name.Contains("SuitMetals")) return p.y < 1.08f ? Leg(p) : Torso(p.y);
        if (p.y < 1.18f) return Leg(p);
        return Torso(p.y);
    }
    static void Validate(BoneWeight w, int count, string part, int vertex) {
        float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
        if (float.IsNaN(sum) || Mathf.Abs(sum - 1) > .0001f || w.weight0 < 0 || w.weight1 < 0 || w.boneIndex0 < 0 || w.boneIndex0 >= count || w.boneIndex1 < 0 || w.boneIndex1 >= count)
            throw new InvalidOperationException("Invalid skin weights at " + part + " vertex " + vertex);
    }
}
