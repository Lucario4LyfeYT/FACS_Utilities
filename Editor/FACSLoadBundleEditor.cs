#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace FACS01.Utilities
{
    [CustomEditor(typeof(FACSLoadBundle))]
    public class FACSLoadBundleEditor : Editor
    {
        private static FACSGUIStyles FacsGUIStyles;
        private Dictionary<string, List<string>> shaderMaterialsMap;
        private List<List<string>> MaterialUsageIn;
        private readonly string[] filters = { "VRChat Files (vrca, vrcw)", "vrca,vrcw", "All files", "*" };
        private FACSLoadBundle LoadBundleScript;

        private void Awake()
        {
            LoadBundleScript = (FACSLoadBundle)target;
        }
        public override void OnInspectorGUI()
        {
            if (FacsGUIStyles == null) { FacsGUIStyles = new FACSGUIStyles(); FacsGUIStyles.helpboxSmall.alignment = TextAnchor.MiddleLeft; }
            EditorGUIUtility.labelWidth = 94;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Bundle Source", FacsGUIStyles.buttonSmall, GUILayout.Height(20), GUILayout.Width(93)))
            {
                string path = EditorUtility.OpenFilePanelWithFilters("Select Asset Bundle", "", filters);
                if (path.Length != 0)
                {
                    GUIUtility.keyboardControl = 0;
                    LoadBundleScript.AssetSource = path;
                }
            }
            LoadBundleScript.AssetSource = EditorGUILayout.TextField("", LoadBundleScript.AssetSource);
            EditorGUILayout.EndHorizontal();

            LoadBundleScript.Name = EditorGUILayout.TextField(" Bundle Name", LoadBundleScript.Name);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (LoadBundleScript.LoadedAssetBundle)
            {
                if (GUILayout.Button($"Reload Bundle", FacsGUIStyles.button))
                {
                    LoadBundleScript.OnDisable();
                    LoadBundleScript.StartLB();
                }
                if (GUILayout.Button($"Unload Bundle", FacsGUIStyles.button))
                {
                    LoadBundleScript.OnDisable();
                }
            }
            else if (LoadBundleScript.gameObject.activeInHierarchy && GUILayout.Button($"Load Bundle", FacsGUIStyles.button))
            {
                LoadBundleScript.OnDisable();
                LoadBundleScript.StartLB();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            LoadBundleScript.ShaderUsage = EditorGUILayout.Foldout(LoadBundleScript.ShaderUsage, "Shaders used in Asset", true);

            if (LoadBundleScript.ShaderUsage)
                if (LoadBundleScript.LoadedAssetBundle)
                {
                    if (shaderMaterialsMap == null || shaderMaterialsMap.Count == 0)
                        shaderMaterialsMap = LoadBundleScript.getShaderUsage();

                    EditorGUILayout.LabelField($"<color=cyan><b>{shaderMaterialsMap.Count}</b> different shaders</color> were found:", FacsGUIStyles.helpboxSmall);

                    foreach (var shaderMaterials in shaderMaterialsMap)
                    {
                        string shaderName = shaderMaterials.Key;
                        string materials = String.Join("\n\t", shaderMaterials.Value);
                        EditorGUILayout.LabelField($"<color=cyan><b>{shaderName}</b></color>\n\t{materials}", FacsGUIStyles.helpboxSmall);
                    }
                }
                else
                    EditorGUILayout.LabelField("Load a Bundle first!", FacsGUIStyles.helpboxSmall);
            else if (shaderMaterialsMap != null)
                shaderMaterialsMap = null;
        }
    }
}
#endif
