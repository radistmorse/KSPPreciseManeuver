using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

public class ReimportUnityEngineUI {
#if UNITY_EDITOR
  [MenuItem ("Assets/Reimport UI Assemblies", false, 100)]
#endif
  public static void ReimportUI () {
#if UNITY_EDITOR
    var path = EditorApplication.applicationContentsPath + "/UnityExtensions/Unity/GUISystem/{1}";
    var version = string.Empty;
    string engineDll = string.Format( path, version, "UnityEngine.UI.dll");
    string editorDll = string.Format( path, version, "Editor/UnityEditor.UI.dll");
    ReimportDll (engineDll);
    ReimportDll (editorDll);
#endif
  }
  static void ReimportDll (string path) {
#if UNITY_EDITOR
    if (File.Exists (path))
      AssetDatabase.ImportAsset (path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
    else
      Debug.LogError (string.Format ("DLL not found {0}", path));
#endif
  }
}