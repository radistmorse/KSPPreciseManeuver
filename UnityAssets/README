How to import the unity project:

1. Download and install unity editor (obviously). Preferably use the same version of the unity engine that is currently used by KSP itself.
2. Create a new 2D project.
3. Close the unity editor.
4. Copy the content of the "UnityAssets" folder into the "Assets" folder of the newly created project.
5. !!!IMPORTANT!!! Copy the compiled PreciseManeuver.Unity.dll into the "Assets/Plugins". Do it before you launch unity or your project is screwed!
6. Launch unity. You can do it by opening the "main.unity" scene. Or you can open the scene in the unity itself.
7. Close unity and launch again. For some reason, the prefabs are not imported correctly after the first launch. Re-launching unity without changing
anything _seems_ to fix this problem.

At this point the assets should be opened and you can work with them. The prefab will look like a white rectangle with text. That's ok, the KSP skin
is applied during the runtime (see StyleApplicator & GUIComponentManager).

To export the prefabs, the BuildAssetBundles script is used (located in "Assets/Scripts"). This script adds a new menu item in unity editor:
"Build/Asset Bundles". Selecting it will create the prefabs file in the "output" folder inside the unity project. The file "atmosphereautopilotprefabs"
should go into the KSP plugin folder.