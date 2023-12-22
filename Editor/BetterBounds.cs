#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BetterBounds : EditorWindow
{
    private GameObject selectedObject;
    private List<string> skinnedMeshRendererNames = new List<string>();
    private List<string> boundsInfo = new List<string>();

    [MenuItem("FACS Utils/Misc/Better Bounds")]
    static void Init()
    {
        BetterSMRBounds window = (BetterSMRBounds)EditorWindow.GetWindow(typeof(BetterSMRBounds));
        window.Show();
    }

    void OnGUI()
    {
        selectedObject = EditorGUILayout.ObjectField("Select GameObject", selectedObject, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Exact Bounds for all SMR's"))
        {
            skinnedMeshRendererNames.Clear();
            boundsInfo.Clear();
            ScanForSkinnedMeshRenderers();

        }

        GUILayout.Space(10);

        GUILayout.Label("SkinnedMeshRenderers Found:");
        for (int i = 0; i < skinnedMeshRendererNames.Count; i++)
        {
            GUILayout.Label(skinnedMeshRendererNames[i]);
            GUILayout.Label("Old Bounds: " + boundsInfo[i * 2]);
            GUILayout.Label("New Bounds: " + boundsInfo[i * 2 + 1]);
        }
    }

    void ScanForSkinnedMeshRenderers()
    {
        if (selectedObject == null)
        {
            Debug.LogWarning("No object selected. Please select a GameObject.");
            return;
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (skinnedMeshRenderers.Length > 0)
            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                Debug.Log("Found SkinnedMeshRenderer: " + renderer.name);
                ExactBounds(renderer);
                skinnedMeshRendererNames.Add(renderer.gameObject.name);
            }
        else
            Debug.Log("No SkinnedMeshRenderer found in " + selectedObject.name + " or its children.");
    }

    //Method made by FACS01
    private void ExactBounds(SkinnedMeshRenderer smr)
    {
        Bounds oldBounds = smr.localBounds;
        smr.updateWhenOffscreen = false;

        var rootBone = smr.rootBone;
        if (!rootBone || !smr.bones.Contains(rootBone))
            rootBone = smr.bones[0];

        var deltaPos = smr.transform.position - rootBone.position;
        var deltaRot = smr.transform.rotation;
        var smrMatrix = Matrix4x4.TRS(deltaPos, deltaRot, Vector3.one);
        var rootBoneMatrix = rootBone.worldToLocalMatrix;
        Mesh tempMesh = new Mesh();
        smr.BakeMesh(tempMesh);
        tempMesh.vertices = tempMesh.vertices.Select(v => (Vector3)(rootBoneMatrix * smrMatrix.MultiplyPoint3x4(v))).ToArray();
        tempMesh.RecalculateBounds();
        smr.localBounds = tempMesh.bounds;
        smr.rootBone = rootBone;

        boundsInfo.Add(oldBounds.ToString());
        boundsInfo.Add(smr.localBounds.ToString());
    }
}
#endif
