# Optional Sample Framework installation

**Selection Gallery** uses the framework's header, captions, and studio grid. Importing the gallery offers to install the framework. Choose **Install** to add it to this project or **Later** to use the gallery's built-in captions. Object Highlighting works in either case.

## Manual installation

Choose **Tools > jlinkdev > Object Highlighting > Install Sample Framework** to reopen the prompt. Package Manager also supports **Install package from disk** using `Packages/com.jlinkdev.sample-framework/package.json` from a checkout. Once the framework release tag is published, **Install package from git URL** accepts:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.sample-framework#sample-framework-v0.1.0
```

The installer accepts package version **0.1.0** from any source, including an embedded or local package. If another version is present, the installer reports it in the Console and leaves version selection to the project owner. A failed Git operation reports the UPM error and the manual installation path. Fix the URL, credentials, network connection, or missing tag as indicated, then retry from the Tools menu.

## Import and reload behavior

The public `UnityEditor.PackageManager.UI.Sample` in editor **6000.3.21f1** exposes `Import`, `FindByPackage`, and import paths. Its public surface has no sample-import completion event. The package's Editor assembly observes `AssetPostprocessor.OnPostprocessAllAssets` and checks the published README GUIDs for the registered samples under `Assets/`. The check also runs after a domain reload. The repository's editable README has a separate GUID. [Unity sample API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PackageManager.UI.Sample.html) · [Asset import callback](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html)

The interactive prompt waits for compilation, asset refresh, and Play mode to finish. The editor remembers the decision for this project and release. Batch imports log the manual option and continue. If someone copied a sample without its metadata, use the Tools menu.

After approval the helper starts an asynchronous UPM request. A session-wide owner prevents two utility packages from installing the same framework concurrently. Unity serializes the request through script reloads. If the editor loses the request, the helper checks the registered packages and reports how to retry. [UPM Client.Add](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PackageManager.Client.Add.html)

Unity can load a scene's panel before it finishes importing the theme stylesheet. The framework resolves that reference after asset import, including when the sample is already open. The scene and project settings stay as authored.

## Opt another utility into the workflow

1. Register the sample in the package manifest and publish its editable copy, including the README metadata.
2. Run **python scripts/sample_installers.py** in the source repository. This synchronizes the helper from Object Highlighting and generates each utility's sample markers, release settings, and unique manual menu item. A utility with no Editor assembly receives an Editor-only asmdef.
3. Keep shared assets and their original `.meta` files in the framework. Reference them from the sample scene using stable Unity object types such as `GUISkin`, `Font`, `Material`, or `GameObject`. The sample publisher preserves references outside the sample tree.
4. Let the sample render a fallback while those objects are unavailable. Keep framework types out of the core utility, the bootstrap assembly, and any sample script that must compile before installation. If a future integration needs framework C# code, put that integration in the framework or a separate optional assembly and test a clean import with the framework absent.
5. Import into a disposable project containing only the utility and its normal dependencies. Exercise **Later**, retry, an unavailable Git revision, and the supported release. Reopen the gallery after installation and inspect **Caption Skin** on the gallery controller. It should resolve to the asset in the framework package.

The embedded framework makes this repository convenient for authoring. A standalone consumer project exercises the missing-package path.
