using System;
using UnityEngine;

namespace jlinkdev.ProceduralNebulae
{
    public enum NebulaShape { CloudsAndLanes, PainterlyCavity, Plumes, BacklitBank, SculptedBank, SweptArc }
    public enum NebulaStructureWarp { None, Vortex, Switchback, OverturningBillows, CarapacePlates, BranchingGills, PoreCups }
    public enum NebulaBaseWarp { None, Vortex, Switchback }
    public enum NebulaBankFormation
    {
        // Profiles serialize these numeric IDs. Retired formation IDs stay reserved.
        Original = 0, OverturningBillows = 1, CarapacePlates = 7, BranchingGills = 8, PoreCups = 9
    }

    [CreateAssetMenu(menuName="jlinkdev/Procedural Nebulae/Style Profile",fileName="Nebula Style")]
    public sealed class NebulaStyleProfile : ScriptableObject, ISerializationCallbackReceiver
    {
        public int defaultFormSeed = 91;
        public int defaultDetailSeed = 1049;
        public NebulaStyleSettings settings = NebulaStyleSettings.Painterly;

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Older profiles stored the pattern and the large-scale bend separately.
            if (settings.bankFormation == NebulaBankFormation.Original) return;
            NebulaStructureWarp pattern = settings.bankFormation switch
            {
                NebulaBankFormation.OverturningBillows => NebulaStructureWarp.OverturningBillows,
                NebulaBankFormation.CarapacePlates => NebulaStructureWarp.CarapacePlates,
                NebulaBankFormation.BranchingGills => NebulaStructureWarp.BranchingGills,
                NebulaBankFormation.PoreCups => NebulaStructureWarp.PoreCups,
                _ => NebulaStructureWarp.None
            };
            if (pattern != NebulaStructureWarp.None)
            {
                settings.baseWarp = (NebulaBaseWarp)settings.structureWarp;
                settings.baseWarpStrength = settings.structureWarpStrength;
                settings.structureWarp = pattern;
                settings.structureWarpStrength = 1;
            }
            settings.bankFormation = NebulaBankFormation.Original;
        }
    }

    [Serializable]
    public struct NebulaStyleSettings
    {
        public NebulaShape shape;
        [Tooltip("Shape the rear gas of Backlit Bank into overlapping dust shelves with illuminated edges.")]
        public bool layeredCurtain;
        [Tooltip("Add a lower foreground shelf to the Sculpted Bank composition.")]
        public bool foregroundShelf;
        [Tooltip("Let the form seed reposition, turn, stretch and bend each bank independently in Sculpted Bank and Swept Arc.")]
        public bool varyBankLayout;
        [Tooltip("Surface displacement for Sculpted Bank or a layered Backlit Bank. Mid-gray is neutral.")]
        public Texture3D shapeTexture;
        [Tooltip("Maximum displacement as a fraction of the box width."), Range(0,.15f)]
        public float shapeDisplacement;
        [Tooltip("Breaks Sculpted Bank, Swept Arc or a layered Backlit Bank into smaller folds and recesses."), Range(0,1)]
        public float surfaceDetail;
        [Tooltip("Adds overlapping lips and undercuts to Surface Detail along the sculpted ridge and right bank."), Range(0,1)]
        public float foldRelief;
        [Tooltip("Join the shoulders of the rear sculpted cloud along a single curved lip."), Range(0,1)]
        public float rearCrease;
        [HideInInspector] // Read only to migrate pre-domain-warp profiles.
        public NebulaBankFormation bankFormation;
        [Tooltip("Deform the full dust and gas field in Painterly Cavity, Sculpted Bank, or Swept Arc.")]
        public NebulaStructureWarp structureWarp;
        [Tooltip("Signed deformation strength. Reverse the sign to reverse the bend."), Range(-2,2)]
        public float structureWarpStrength;
        [Tooltip("Optional underlying bend for Billows, Plates, Gills, and Pore Cups.")]
        public NebulaBaseWarp baseWarp;
        [Range(-2,2)] public float baseWarpStrength;
        [Tooltip("Use an independent warp pattern instead of following the form seed.")]
        public bool overrideWarpSeed;
        public int warpSeed;
        [Range(2,12)] public float foldFrequency;
        [Range(0,1)] public float coverage, cloudBody, wisps;
        [Tooltip("Relative size of the plume lobes."), Range(.5f,1.6f)] public float plumeScale;
        [Tooltip("Thickness of a plume's soft margin, relative to one box width."), Range(.006f,.08f)] public float edgeSoftness;
        [Range(0,80)] public float extinction;
        [ColorUsage(false,true)] public Color shadowFill, litDust, outerGas, innerGas;
        [Tooltip("Intermediate gas color for Backlit Bank, scattered-light palette for Sculpted Bank, or cavity gas color for Swept Arc. Alpha controls its strength."), ColorUsage(true,true)]
        public Color transitionGas;
        [Range(0,4)] public float gasBrightness;

        public static NebulaStyleSettings Painterly => new()
        {
            shape=NebulaShape.PainterlyCavity, foldFrequency=4.8f, coverage=.6f, cloudBody=.94f, wisps=.98f,
            plumeScale=1, edgeSoftness=.035f, extinction=28, gasBrightness=.73f,
            structureWarpStrength=1.25f, baseWarpStrength=1.25f,
            shadowFill=new Color(.23f,.20f,.22f), litDust=new Color(.95f,.59f,.38f),
            outerGas=new Color(.61f,.50f,.87f), innerGas=new Color(1,.78f,.49f)
        };

        internal static NebulaStyleSettings FromVolume(NebulaVolume v) => new()
        {
            shape=v.emissionCavity ? NebulaShape.PainterlyCavity : NebulaShape.CloudsAndLanes,
            foldFrequency=v.banksAcross, coverage=v.coverage, cloudBody=v.cloudBody, wisps=v.wisps,
            extinction=v.extinction, gasBrightness=v.gasBrightness, shadowFill=v.shadowFill,
            litDust=v.litDust, outerGas=v.outerGas, innerGas=v.innerGas
        };
    }
}
