using System.IO;
using jlinkdev.UnityUtilities.InteractiveFog.EditableSamples;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.InteractiveFog.Development
{
    public static class InteractiveFogValidation
    {
        public static void Capture()
        {
            InteractiveFogDevelopmentBuilder.BuildAll();
            ShaderUtil.allowAsyncCompilation=false;
            var rig=Object.FindFirstObjectByType<MistSanctuary>();
            string output=Path.GetFullPath("../InteractiveFogCaptures"); Directory.CreateDirectory(output);
            try
            {
                var interactor=rig.actor.GetComponent<FogInteractor>(); rig.fog.Initialize();
                var watch=System.Diagnostics.Stopwatch.StartNew();
                for(int i=0;i<=180;i++)
                {
                    float t=i/30f;
                    rig.actor.position=new Vector3(-3+t,0,-1+Mathf.Sin(t)*1.5f); interactor.SampleMotion(1f/30);
                    if(i==105) rig.fog.AddGust(rig.actor.position+Vector3.up*.45f, new Vector3(5,1,2),1.3f,.3f);
                    rig.fog.Advance(1f/30);
                    if(i==0||i==90||i==120||i==180) Image(rig.view,Path.Combine(output,"editor-"+i.ToString("D3")+".png"));
                }
                rig.view.transform.position=new Vector3(0,1.2f,-10); rig.view.transform.LookAt(new Vector3(0,.4f,0));
                Image(rig.view,Path.Combine(output,"editor-low.png"));
                Debug.Log("FOG_CAPTURE_OK "+output+" steps="+rig.fog.StepCount+" elapsed="+watch.Elapsed.TotalSeconds+" GPU="+SystemInfo.graphicsDeviceName);
                InteractiveFogDevelopmentBuilder.Publish();
            }
            finally
            {
                rig.fog.enabled=false;
            }
        }
        static void Image(Camera camera,string path)
        {
            var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGBHalf); target.Create();
            var old=camera.targetTexture; var active=RenderTexture.active;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false,false);
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {camera.targetTexture=old; RenderTexture.active=active; Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target);}
        }
    }
}
