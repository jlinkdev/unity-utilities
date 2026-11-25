using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.HumanoidStudio.Editor
{
    internal sealed class HumanoidImport : AssetPostprocessor
    {
        internal const string Root = "Assets/Humanoid";
        internal const string JointSweep = Root + "/Animations/Joint Sweep.fbx";
        // Only these sources use the anatomical rig. Props can share the Models folder.
        static readonly HashSet<string> RigSources = new(StringComparer.Ordinal)
        {
            JointSweep,
            Root + "/Animations/Character Motion.fbx",
            Root + "/Animations/Step Turns.fbx",
            Root + "/Animations/Running Turns.fbx",
            Root + "/Animations/Character Actions.fbx",
            Root + "/Models/Anatomical Base/Anatomical Base.fbx",
            Root + "/Models/Reduction Study/Reduction Study.fbx",
            Root + "/Models/Sculpted Planes/Sculpted Planes.fbx",
            Root + "/Models/Humanoid/Humanoid.fbx"
        };

        void OnPreprocessModel()
        {
            if (!RigSources.Contains(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            var description = importer.humanDescription;
            var bones = new List<HumanBone>();
            void Map(string human, string model) => bones.Add(new HumanBone
            {
                humanName = human, boneName = model, limit = new HumanLimit { useDefaultValues = true }
            });
            Map("Hips", "pelvis");
            Map("Spine", "spine_01");
            Map("Chest", "spine_02");
            Map("UpperChest", "spine_03");
            Map("Neck", "neck_01");
            Map("Head", "head");
            foreach (var side in new[] { "Left", "Right" })
            {
                var suffix = side == "Left" ? "_l" : "_r";
                Map(side + "Shoulder", "clavicle" + suffix);
                Map(side + "UpperArm", "upperarm" + suffix);
                Map(side + "LowerArm", "lowerarm" + suffix);
                Map(side + "Hand", "hand" + suffix);
                Map(side + "UpperLeg", "thigh" + suffix);
                Map(side + "LowerLeg", "calf" + suffix);
                Map(side + "Foot", "foot" + suffix);
                Map(side + "Toes", "ball" + suffix);
                foreach (var digit in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
                {
                    var prefix = digit == "Little" ? "pinky" : digit.ToLowerInvariant();
                    var joint = 0;
                    foreach (var part in new[] { "Proximal", "Intermediate", "Distal" })
                        Map(side + " " + digit + " " + part, prefix + "_0" + (++joint) + suffix);
                }
            }
            description.human = bones.ToArray();
            importer.humanDescription = description;
        }
    }
}
