using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimationBaker : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer smr;
    [SerializeField] private AnimationClip clip;
    [SerializeField] private string clipName;
    [SerializeField][Range(1, 60)] private int frame;

    private void Bake()
    {
        GameObject root = smr.transform.parent.gameObject;
        Animator tempAnimator = root.AddComponent<Animator>();
        AnimatorController tempController = AnimatorController.CreateAnimatorControllerAtPath($"Assets/Resources/GPUAnimationTexture/{clipName}.controller");
        tempController.AddMotion(clip);
        tempAnimator.runtimeAnimatorController = tempController;

        Mesh bakeMesh = new();
        int vertexCount = smr.sharedMesh.vertexCount;
        int frameCount = Mathf.CeilToInt(clip.length * frame);
        Texture2D tex = new(frameCount, vertexCount * 3, TextureFormat.RGBAFloat, false);
        smr.updateWhenOffscreen = true;
        tempAnimator.Play(clip.name);
        float deltaTime = 1 / frame;

        for (int f = 0; f < frameCount; f++)
        {
            tempAnimator.Update(deltaTime);
            smr.BakeMesh(bakeMesh, true);

            Debug.Log(bakeMesh.vertices[300]);

            for (int v = 0; v < vertexCount; v++)
            {
                Vector3 pos = bakeMesh.vertices[v];
                Vector3 normal = bakeMesh.normals[v];
                Vector4 tangent = bakeMesh.tangents[v];

                tex.SetPixel(f, v * 3, new Color(pos.x, pos.y, pos.z));
                tex.SetPixel(f, v * 3 + 1, new Color(normal.x, normal.y, normal.z));
                tex.SetPixel(f, v * 3 + 2, new Color(tangent.x, tangent.y, tangent.z, tangent.w));
            }
        }

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        string path = $"Assets/Resources/GPUAnimationTexture/{clipName}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(tex, path);
        AssetDatabase.SaveAssets();

        AssetDatabase.DeleteAsset($"Assets/Resources/GPUAnimationTexture/{clipName}.controller");
    }

    private void Awake()
    {
        Bake();
    }
}
