using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using jlinkdev.UnityUtilities.WaterSystem;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Development
{
    /// <summary>Copy only this script into a clean consumer project's Editor folder to verify UPM sample import.</summary>
    public static class WaterPackageImportValidation
    {
        private const string PackageName = "com.jlinkdev.water-system";
        private const string PackageVersion = "0.1.0";
        private const string PathRecord = "WaterImportedSamplePath.txt";
        private const string ReportFile = "WaterPackageImportValidation.txt";

        /// <summary>First batch invocation. Imported scripts compile before the separate verification invocation.</summary>
        public static void ImportSample()
        {
            var samples = Sample.FindByPackage(PackageName, PackageVersion)
                .Where(item => item.displayName == "Water Laboratory").ToArray();
            if (samples.Length != 1)
                throw new InvalidOperationException($"Expected one Water Laboratory sample in {PackageName}@{PackageVersion}, found {samples.Length}.");
            var sample = samples[0];
            if (!sample.Import(Sample.ImportOptions.OverridePreviousImports | Sample.ImportOptions.HideImportWindow))
                throw new InvalidOperationException("Package Manager did not import Water Laboratory.");
            string path = ProjectRelative(sample.importPath);
            if (!path.StartsWith("Assets/Samples/", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected Package Manager sample destination: " + path);
            File.WriteAllText(PathRecord, path + Environment.NewLine);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Water System] Package Manager imported Water Laboratory into " + path);
        }

        /// <summary>Second batch invocation, after Unity has compiled the imported sample controller.</summary>
        public static void VerifyImportedSample()
        {
            var report = new StringBuilder();
            var errors = new List<string>();
            report.AppendLine("Water System clean consumer sample verification");
            report.AppendLine("Unity: " + Application.unityVersion);
            try
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(WaterBody).Assembly);
                Check(package != null && package.name == PackageName && package.version == PackageVersion,
                    "Installed package identity and version", errors, report);
                if (package != null)
                {
                    report.AppendLine("Package source: " + package.source);
                    report.AppendLine("Package resolved path: " + package.resolvedPath);
                }
                string samplePath = ProjectRelative(File.ReadAllText(PathRecord).Trim());
                Check(samplePath.StartsWith("Assets/Samples/", StringComparison.Ordinal), "UPM sample destination", errors, report);
                string scenePath = samplePath + "/Scenes/Water Laboratory.unity";
                if (!File.Exists(scenePath))
                    throw new FileNotFoundException("Imported sample scene is missing.", scenePath);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                report.AppendLine("Scene: " + scenePath);
                var components = new List<Component>();
                int missingScripts = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                    components.AddRange(root.GetComponentsInChildren<Component>(true).Where(item => item != null));
                }
                Check(missingScripts == 0, "No missing MonoBehaviour scripts", errors, report);
                foreach (var component in components)
                    CheckObjectReferences(component, errors);

                var waterBodies = components.OfType<WaterBody>().ToArray();
                Check(waterBodies.Length == 1, "One WaterBody in laboratory", errors, report);
                WaterBody body = waterBodies.FirstOrDefault();
                if (body != null)
                {
                    var serialized = new SerializedObject(body);
                    Check(serialized.FindProperty("profile").objectReferenceValue != null,
                        "WaterBody has a persistent assigned profile", errors, report);
                    body.Rebuild();
                    Check(body.SurfaceMesh != null && body.SurfaceMesh.vertexCount > 0,
                        "WaterBody generates its surface mesh", errors, report);
                    Check(body.SurfaceMaterial != null && body.SurfaceMaterial.shader != null,
                        "WaterBody generates its surface material and shader", errors, report);
                    Check(body.HasSupportedTransform, "WaterBody uses supported transform", errors, report);
                }
                foreach (string resource in new[] { "WaterSurface", "WaterBoundary", "WaterComposite", "WaterParticles" })
                {
                    Shader shader = Resources.Load<Shader>(resource);
                    Check(shader != null, "Retained Resources shader: " + resource, errors, report);
                    if (shader != null)
                        report.AppendLine("  " + shader.name + " at " + AssetDatabase.GetAssetPath(shader));
                }

                var controller = components.FirstOrDefault(item => item.GetType().FullName ==
                    "jlinkdev.UnityUtilities.WaterSystem.Samples.WaterLaboratoryController");
                Check(controller != null, "Imported controller resolves without host authoring assembly", errors, report);
                if (controller != null)
                {
                    var serialized = new SerializedObject(controller);
                    Check(serialized.FindProperty("Water").objectReferenceValue == body,
                        "Controller Water reference", errors, report);
                    var camera = serialized.FindProperty("ViewCamera").objectReferenceValue as Camera;
                    Check(camera != null && camera.GetComponent<WaterCamera>() != null,
                        "Controller camera and WaterCamera reference", errors, report);
                    var presets = serialized.FindProperty("Presets");
                    Check(presets != null && presets.arraySize == 3, "Three profile presets", errors, report);
                    if (presets != null)
                    {
                        for (int i = 0; i < presets.arraySize; i++)
                        {
                            var profile = presets.GetArrayElementAtIndex(i).objectReferenceValue as WaterProfile;
                            Check(profile != null && profile.Waves != null && profile.Waves.Length > 0,
                                "Assigned wave profile " + i, errors, report);
                        }
                    }
                    var props = serialized.FindProperty("FloatingProps");
                    Check(props != null && props.arraySize == 3, "Three floating props", errors, report);
                    if (props != null)
                    {
                        for (int i = 0; i < props.arraySize; i++)
                        {
                            var rigidbody = props.GetArrayElementAtIndex(i).objectReferenceValue as Rigidbody;
                            Check(rigidbody != null && rigidbody.GetComponent<WaterBuoyancy>() != null,
                                "Floating rigidbody and buoyancy " + i, errors, report);
                        }
                    }
                    if (camera != null)
                    {
                        var local = camera.GetComponent<WaterCameraPass>();
                        Check(local && local.feature && local.feature.isActive, "Camera-local water feature", errors, report);
                        var data = camera.GetComponent<UniversalAdditionalCameraData>();
                        Check(data && data.requiresDepthTexture && data.requiresColorTexture, "Camera requests depth and color", errors, report);
                    }
                }
                var volume = components.OfType<Volume>().FirstOrDefault(item => item.isGlobal);
                Check(volume != null && volume.sharedProfile != null, "Global grading Volume and profile", errors, report);
                if (volume != null && volume.sharedProfile != null)
                    Check(volume.sharedProfile.TryGet<Tonemapping>(out var tonemapping) && tonemapping.mode.value == TonemappingMode.ACES,
                        "ACES tonemapping component", errors, report);

                var dependencies = AssetDatabase.GetDependencies(scenePath, true);
                var hostDependencies = dependencies.Where(path => path.StartsWith("Assets/PackageDevelopment/", StringComparison.Ordinal)).ToArray();
                Check(hostDependencies.Length == 0, "No host PackageDevelopment asset dependencies", errors, report);
                foreach (string path in hostDependencies) errors.Add("Unexpected host dependency: " + path);
                var outsideSample = dependencies.Where(path => path.StartsWith("Assets/", StringComparison.Ordinal)
                    && !path.StartsWith(samplePath + "/", StringComparison.Ordinal)).ToArray();
                Check(outsideSample.Length == 0, "All Assets dependencies contained in imported sample", errors, report);
                foreach (string path in outsideSample) errors.Add("Asset outside sample: " + path);
                report.AppendLine("Scene component count: " + components.Count);
                report.AppendLine("Scene dependency count: " + dependencies.Length);
            }
            catch (Exception exception)
            {
                errors.Add(exception.ToString());
            }
            report.AppendLine();
            report.AppendLine(errors.Count == 0 ? "PASS" : "FAIL: " + errors.Count + " issue(s)");
            foreach (string error in errors) report.AppendLine("ERROR: " + error);
            report.AppendLine("This verifies import, references, and setup; it does not establish rendering quality or performance.");
            File.WriteAllText(ReportFile, report.ToString());
            if (errors.Count != 0)
                throw new InvalidOperationException("Water Laboratory consumer verification failed. See " + ReportFile + Environment.NewLine + string.Join(Environment.NewLine, errors));
            Debug.Log("[Water System] Clean consumer sample verification passed. " + ReportFile);
        }

        private static void CheckObjectReferences(Component component, List<string> errors)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    errors.Add($"Unresolved reference: {component.gameObject.name}/{component.GetType().Name}.{property.propertyPath}");
            }
        }

        private static void Check(bool condition, string label, List<string> errors, StringBuilder report)
        {
            report.AppendLine((condition ? "PASS: " : "FAIL: ") + label);
            if (!condition) errors.Add(label);
        }

        private static string ProjectRelative(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (!Path.IsPathRooted(normalized)) return normalized.TrimEnd('/');
            string project = Path.GetDirectoryName(Application.dataPath).Replace('\\', '/').TrimEnd('/') + "/";
            if (!normalized.StartsWith(project, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Sample import path is outside the consumer project: " + path);
            return normalized.Substring(project.Length).TrimEnd('/');
        }
    }
}
