using UnityEditor;
using UnityEngine;

namespace Tools
{
    public class FixAnimationPaths
    {
        [MenuItem("Tools/Fix Animation Paths")]
        static void Fix()
        {
            foreach (var obj in Selection.objects)
            {
                var clip = obj as AnimationClip;
                if (clip == null) continue;

                var bindings = AnimationUtility.GetCurveBindings(clip);

                foreach (var b in bindings)
                {
                    if (!b.path.StartsWith("Armature.003"))
                        continue;

                    var curve = AnimationUtility.GetEditorCurve(clip, b);

                    var nb = b;
                    nb.path = b.path.Replace("Armature.003", "Armature");

                    AnimationUtility.SetEditorCurve(clip, b, null);
                    AnimationUtility.SetEditorCurve(clip, nb, curve);
                }

                EditorUtility.SetDirty(clip);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
