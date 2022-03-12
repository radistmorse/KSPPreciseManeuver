// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2016 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//  

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class BuildAssetBundles : MonoBehaviour
{
#if UNITY_EDITOR
  [MenuItem("Build/Asset Bundles")]
#endif
  public static void Build()
    {
#if UNITY_EDITOR
    BuildPipeline.BuildAssetBundles (Application.dataPath + "/../output/", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
#endif
  }
}