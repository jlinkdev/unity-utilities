using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    // Separate X/Z solver resources. No 3D velocity, pressure or transported-density grid.
    internal sealed class FogPlanarSimulation
    {
        readonly ComputeShader compute;
        readonly int width, height;
        readonly Vector3Int lightResolution;
        RenderTexture restVolume;
        readonly Dictionary<string,int> kernels = new();
        readonly List<RenderTexture> owned = new();
        RenderTexture velocity, velocityNext, mass, massNext, pressure, pressureNext;
        RenderTexture divergence, solid, rest, budget, flux, coordinatesA, coordinatesANext, coordinatesB, coordinatesBNext, lighting;
        public RenderTexture Density => mass;
        public RenderTexture Velocity => velocity;
        public RenderTexture Solid => solid;
        public RenderTexture Coordinates => coordinatesA;
        public RenderTexture ProceduralSources { get; set; }
        public long TextureBytes => (long)width*height*88+(long)lightResolution.x*lightResolution.y*lightResolution.z*8;
        public FogPlanarSimulation(ComputeShader compute,int width,int height,int verticalResolution)
        {
            this.compute=compute;this.width=width;this.height=height;lightResolution=new Vector3Int((width+1)/2,verticalResolution,(height+1)/2);
            velocity=Vector("Velocity");velocityNext=Vector("Velocity next");
            mass=Scalar("Column mass");massNext=Scalar("Column mass next");rest=Scalar("Resting column mass");
            pressure=Scalar("Pressure");pressureNext=Scalar("Pressure next");divergence=Scalar("Divergence");solid=Scalar("Solid");budget=Scalar("Donor budget");
            flux=Texture("Face flux",GraphicsFormat.R32G32_SFloat);
            coordinatesA=Vector("Coordinates A");coordinatesANext=Vector("Coordinates A next");coordinatesB=Vector("Coordinates B");coordinatesBNext=Vector("Coordinates B next");
            lighting=new RenderTexture(lightResolution.x,lightResolution.y,0){name="Interactive Fog 2D volumetric lighting",dimension=UnityEngine.Rendering.TextureDimension.Tex3D,volumeDepth=lightResolution.z,graphicsFormat=GraphicsFormat.R16G16B16A16_SFloat,enableRandomWrite=true,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            lighting.Create();owned.Add(lighting);
        }
        RenderTexture Vector(string name)=>Texture(name,GraphicsFormat.R16G16B16A16_SFloat);
        RenderTexture Scalar(string name)=>Texture(name,GraphicsFormat.R32_SFloat);
        RenderTexture Texture(string name,GraphicsFormat format)
        {
            var t=new RenderTexture(width,height,0){name="Interactive Fog 2D "+name,graphicsFormat=format,enableRandomWrite=true,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            t.Create();owned.Add(t);return t;
        }
        public void Reset(RenderTexture restingVolume)
        {
            restVolume=restingVolume;
            Dispatch("PInitialize",restingVolume);Swap(ref mass,ref massNext);Swap(ref velocity,ref velocityNext);Swap(ref pressure,ref pressureNext);
            Swap(ref coordinatesA,ref coordinatesANext);Swap(ref coordinatesB,ref coordinatesBNext);
            Graphics.CopyTexture(mass,massNext);Graphics.CopyTexture(coordinatesA,coordinatesANext);Graphics.CopyTexture(coordinatesB,coordinatesBNext);
        }
        public void RebuildRest(RenderTexture restingVolume)=>Dispatch("PBuildRest",restingVolume);
        public void Step(int iterations,bool obstaclesDirty)
        {
            if(obstaclesDirty)Dispatch("PObstacles");
            Dispatch("PAdvect");Swap(ref velocity,ref velocityNext);
            Dispatch("PForces");Swap(ref velocity,ref velocityNext);
            Dispatch("PDivergence");Swap(ref pressure,ref pressureNext);
            for(int i=0;i<iterations;i++){Dispatch("PPressure");Swap(ref pressure,ref pressureNext);}
            Dispatch("PProject");Swap(ref velocity,ref velocityNext);
            Dispatch("PFlux");Dispatch("PBudget");Dispatch("PDensity");Swap(ref mass,ref massNext);
            Dispatch("PCoordinates");Swap(ref coordinatesA,ref coordinatesANext);Swap(ref coordinatesB,ref coordinatesBNext);
        }
        public void Light(RenderTexture shape,RenderTexture puffs)
        {
            if(!kernels.TryGetValue("PLight",out int k)) { k=compute.FindKernel("PLight"); kernels.Add("PLight",k); }
            compute.SetTexture(k,"_CumulusShape",shape?shape:restVolume);
            compute.SetTexture(k,"_PuffDensity",puffs?puffs:restVolume);
            Dispatch("PLight");
        }
        void Dispatch(string name,RenderTexture restingVolume=null)
        {
            if(!kernels.TryGetValue(name,out int k)){k=compute.FindKernel(name);kernels.Add(name,k);}
            compute.SetTexture(k,"_PVelocity",velocity);compute.SetTexture(k,"_PVelocityOut",velocityNext);
            compute.SetTexture(k,"_PMass",mass);compute.SetTexture(k,"_PMassOut",massNext);compute.SetTexture(k,"_PRestMass",rest);
            compute.SetTexture(k,"_ProceduralSources",ProceduralSources?ProceduralSources:restVolume);
            compute.SetTexture(k,"_PPressure",pressure);compute.SetTexture(k,"_PPressureOut",pressureNext);compute.SetTexture(k,"_PDivergence",divergence);
            compute.SetTexture(k,"_PSolid",solid);compute.SetTexture(k,"_PBudget",budget);compute.SetTexture(k,"_PFlux",flux);compute.SetTexture(k,"_PFluxRead",flux);
            compute.SetTexture(k,"_PCoordA",coordinatesA);compute.SetTexture(k,"_PCoordAOut",coordinatesANext);
            compute.SetTexture(k,"_PCoordB",coordinatesB);compute.SetTexture(k,"_PCoordBOut",coordinatesBNext);compute.SetTexture(k,"_PLighting",lighting);
            if(restingVolume)compute.SetTexture(k,"_RestDensity",restingVolume);
            if(name=="PLight")
            {
                compute.SetInts("_PLightResolution",lightResolution.x,lightResolution.y,lightResolution.z);compute.SetTexture(k,"_PRestVolume",restVolume);
                compute.Dispatch(k,(lightResolution.x+7)/8,(lightResolution.y+3)/4,(lightResolution.z+7)/8);
            }
            else compute.Dispatch(k,(width+7)/8,(height+7)/8,1);
        }
        public void Apply(Material material)
        {
            material.SetTexture("_FogPlanarMass",mass);material.SetTexture("_FogPlanarPreviousMass",massNext);material.SetTexture("_FogPlanarRestMass",rest);
            material.SetTexture("_FogPlanarCoordinatesA",coordinatesA);material.SetTexture("_FogPlanarPreviousCoordinatesA",coordinatesANext);
            material.SetTexture("_FogPlanarCoordinatesB",coordinatesB);material.SetTexture("_FogPlanarPreviousCoordinatesB",coordinatesBNext);
            material.SetTexture("_FogPlanarLighting",lighting);material.SetTexture("_FogPlanarSolid",solid);
        }
        static void Swap(ref RenderTexture a,ref RenderTexture b)=>(a,b)=(b,a);
        public void Release()
        {
            foreach(var t in owned)if(t){t.Release();if(Application.isPlaying)Object.Destroy(t);else Object.DestroyImmediate(t);}
            owned.Clear();kernels.Clear();
        }
    }
}
