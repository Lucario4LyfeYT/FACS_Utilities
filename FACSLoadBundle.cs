#if UNITY_EDITOR
#if (VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3)
using VRC.Core;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;


using Object = UnityEngine.Object;

namespace FACS01.Utilities
{
    [ExecuteAlways]
    public class FACSLoadBundle : MonoBehaviour
    {
        public string AssetSource;
        public string Name;

        public AssetBundle LoadedAssetBundle;
        private GameObject avatarInstance;
        private string worldSceneName;

        public bool ShaderUsage;

        public IEnumerator coroutine;

        private UnityWebRequest bundleWebRequest;
        private AssetBundleCreateRequest bundleFileRequest;

        public void OnEnable()
        {
            if (Application.isPlaying && !String.IsNullOrWhiteSpace(AssetSource))
            {
                StartLB();
            }
        }
        public void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && LoadedAssetBundle)
            {
                OnDisable();
            }
        }
        public void StartLB()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            coroutine = LoadBundle();
            StartCoroutine(coroutine);
        }

        public void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                if (bundleWebRequest != null)
                {
                    DownloadHandlerAssetBundle.GetContent(bundleWebRequest).Unload(true);
                }
                if (bundleFileRequest != null)
                {
                    bundleFileRequest.assetBundle.Unload(true);
                }
                coroutine = null; bundleWebRequest = null; bundleFileRequest = null;
                Debug.Log($"[<color=green>FACS Load Bundle</color>] Aborting <color=cyan>AssetBundle</color> from: {AssetSource}\n");
            }
            if (avatarInstance != null)
            {
                Object.DestroyImmediate(avatarInstance);
                avatarInstance = null;
            }
            if (!String.IsNullOrEmpty(worldSceneName))
            {
                SceneManager.UnloadSceneAsync(worldSceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                worldSceneName = null;
            }
            if (LoadedAssetBundle)
            {
                LoadedAssetBundle.Unload(true);
                LoadedAssetBundle = null;
                Debug.Log($"[<color=green>FACS Load Bundle</color>] Unloading <color=cyan>AssetBundle</color> from: {AssetSource}\n");
            }
        }
        public IEnumerator LoadBundle()
        {
            if (String.IsNullOrWhiteSpace(AssetSource))
            {
                Debug.LogWarning($"[<color=green>FACS Load Bundle</color>] Empty Bundle Source in <color=cyan>LoadBundle</color>\n");
                coroutine = null;
                yield break;
            }

            bool isURL = Uri.TryCreate(AssetSource, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeHttp);

            if (isURL)
            {
                Debug.Log($"[<color=green>FACS Load Bundle</color>] Getting <color=cyan>AssetBundle</color> from <color=green>URL</color>: {AssetSource}\nPlease wait a moment...");
                bundleWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(AssetSource);
                yield return bundleWebRequest.SendWebRequest();
                LoadedAssetBundle = DownloadHandlerAssetBundle.GetContent(bundleWebRequest);
            }
            else if (File.Exists(AssetSource))
            {
                Debug.Log($"[<color=green>FACS Load Bundle</color>] Getting <color=cyan>AssetBundle</color> from local <color=green>file</color>: {AssetSource}\n");

                bundleFileRequest = AssetBundle.LoadFromFileAsync(AssetSource);

                yield return bundleFileRequest;

                LoadedAssetBundle = bundleFileRequest.assetBundle;
            }
            else
            {
                Debug.LogWarning($"[<color=green>FACS Load Bundle</color>] Invalid URL or File path for <color=cyan>LoadBundle</color>: {AssetSource}");
                coroutine = null;
                yield break;
            }

            if (LoadedAssetBundle == null)
            {
                if (isURL) Debug.LogError($"[<color=green>FACS Load Bundle</color>] Failed to load <color=cyan>AssetBundle</color> from URL: {AssetSource}\n");
                else Debug.LogError($"[<color=green>FACS Load Bundle</color>] Failed to load <color=cyan>AssetBundle</color> from file: {AssetSource}\n");
                coroutine = null;
                yield break;
            }

            if (!LoadedAssetBundle.isStreamedSceneAssetBundle)
            {
                foreach (string asset in LoadedAssetBundle.GetAllAssetNames())
                {
                    if (asset.EndsWith(".prefab"))
                    {
                        avatarInstance = Instantiate((GameObject)LoadedAssetBundle.LoadAsset(asset), this.transform, false);
                        avatarInstance.transform.localPosition = new Vector3(0, 0, 0);

#if VRC_SDK_VRCSDK3
                        DestroyImmediate(avatarInstance.GetComponent<PipelineSaver>());
                        DestroyImmediate(avatarInstance.GetComponent<PipelineManager>());
#elif VRC_SDK_VRCSDK2
                        DestroyImmediate(avatarInstance.GetComponent<PipelineManager>());
#endif

                        if (!String.IsNullOrWhiteSpace(Name))
                        {
                            avatarInstance.name = Name;
                        }
                        else
                        {
                            string fileName = Path.GetFileNameWithoutExtension(AssetSource);
                            string prefabName = avatarInstance.name;

                            if (prefabName.EndsWith("(Clone)")) { prefabName = prefabName.Substring(0, prefabName.LastIndexOf("(Clone)")); }

                            if (isURL) avatarInstance.name = prefabName;
                            else avatarInstance.name = fileName + " (" + prefabName + ")";
                        }


                        Debug.Log($"[<color=green>FACS Load Bundle</color>] Prefab <color=green>{avatarInstance.name}</color> was loaded from <color=cyan>AssetBundle</color>!\n");
                        coroutine = null;
                        yield break;
                    }
                }

                Debug.LogWarning($"[<color=green>FACS Load Bundle</color>] Didn't find any .prefab or scene to load from <color=cyan>LoadBundle</color>: {AssetSource}");
                coroutine = null;
            }

            else
            {
                if (!Application.isPlaying)
                {
                    OnDisable();
                    Debug.LogWarning($"[<color=green>FACS Load Bundle</color>] Can't load scenes on Edit Mode. Please enter Play Mode.\n");
                    yield break;
                }

                string[] scenePaths = LoadedAssetBundle.GetAllScenePaths();
                worldSceneName = Path.GetFileNameWithoutExtension(scenePaths[0]);
                SceneManager.LoadScene(worldSceneName, LoadSceneMode.Additive);
                yield return null;

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
                List<GameObject> rootObjects = new List<GameObject>();
                Scene scene = SceneManager.GetSceneByName(worldSceneName);
                scene.GetRootGameObjects(rootObjects);
                foreach (var rootObject in rootObjects)
                {
                    PipelineManager[] removeblueprintid = rootObject.GetComponentsInChildren<PipelineManager>(true);
                    foreach (var blueprintid in removeblueprintid) { DestroyImmediate(blueprintid); }
                }
#endif

                Debug.Log($"[<color=green>FACS Load Bundle</color>] Scene <color=green>{worldSceneName}</color> was loaded from <color=cyan>AssetBundle</color>!\n");
                coroutine = null;
            }
        }

        public Dictionary<string, List<string>> getShaderUsage()
        {
            var shaderMaterialsMap = new Dictionary<string, HashSet<string>>();

            Action<Renderer[]> processRenderers = (renderers) =>
            {
                foreach (Renderer renderer in renderers)
                    foreach (Material material in renderer.sharedMaterials)

                        if (material != null)
                        {
                            string shaderName = material.shader.name;
                            if (!shaderMaterialsMap.TryGetValue(shaderName, out var materials))
                            {
                                materials = new HashSet<string>();
                                shaderMaterialsMap[shaderName] = materials;
                            }
                            materials.Add(material.name);
                        }
            };

            if (avatarInstance)
            {
                Renderer[] allRenderers = avatarInstance.GetComponentsInChildren<Renderer>(true);
                processRenderers(allRenderers);
            }
            else
            {
                GameObject[] rootObjects = SceneManager.GetSceneByName(worldSceneName).GetRootGameObjects();
                foreach (GameObject go in rootObjects)
                {
                    Renderer[] allRenderers = go.GetComponentsInChildren<Renderer>(true);
                    processRenderers(allRenderers);
                }
            }

             //Optional: Convert HashSet to List if you need an ordered list of material names for each shader
            var orderedShaderMaterialsMap = shaderMaterialsMap.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.OrderBy(name => name).ToList()
            );

            return orderedShaderMaterialsMap;
        }

    }
}
#endif
