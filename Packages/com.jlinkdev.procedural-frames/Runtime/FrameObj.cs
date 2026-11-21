using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace JLink.Frames
{
    public static class FrameObj
    {
        static string F(float n) => n.ToString("R",CultureInfo.InvariantCulture);

        /// <summary>Write a Y-up, right-handed OBJ with UVs, vertex normals and named material groups.</summary>
        public static void Write(Mesh mesh, string path)
        {
            if (!mesh) throw new ArgumentNullException(nameof(mesh));
            path = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temp = Path.Combine(directory,Guid.NewGuid().ToString("N")+".tmp");
            try
            {
                var library = WriteMaterialLibrary(directory,mesh.subMeshCount>(int)FrameSlot.EaselBack);
                using (var w = new StreamWriter(temp,false,new UTF8Encoding(false)))
                {
                    w.WriteLine("mtllib "+library);
                    w.WriteLine("o PictureFrame");
                    foreach (var v in mesh.vertices) w.WriteLine($"v {F(-v.x)} {F(v.y)} {F(v.z)}");
                    foreach (var t in mesh.uv) w.WriteLine($"vt {F(t.x)} {F(t.y)}");
                    foreach (var n in mesh.normals) w.WriteLine($"vn {F(-n.x)} {F(n.y)} {F(n.z)}");
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        var indices = mesh.GetTriangles(i);
                        if (indices.Length == 0) continue;
                        w.WriteLine("usemtl "+(FrameSlot)i);
                        for (int j = 0; j < indices.Length; j += 3)
                        {
                            int a = indices[j]+1, b = indices[j+1]+1, c = indices[j+2]+1;
                            w.WriteLine($"f {a}/{a}/{a} {c}/{c}/{c} {b}/{b}/{b}");
                        }
                    }
                }
                if (File.Exists(path)) File.Replace(temp,path,null);
                else File.Move(temp,path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        static string WriteMaterialLibrary(string directory,bool easel)
        {
            string contents = "newmtl Moulding\nKd 1 1 1\nnewmtl Mat\nKd 1 1 1\nnewmtl Artwork\nKd 1 1 1\n";
            if (easel) contents+="newmtl EaselBack\nKd 1 1 1\n";
            string temp = Path.Combine(directory,Guid.NewGuid().ToString("N")+".tmp");
            try
            {
                for (int i = 0; ; i++)
                {
                    string name = i == 0 ? "FrameSlots.mtl" : $"FrameSlots_{i}.mtl";
                    string destination = Path.Combine(directory,name);
                    if (File.Exists(destination))
                    {
                        if (new FileInfo(destination).Length == contents.Length && File.ReadAllText(destination) == contents) return name;
                        continue;
                    }
                    if (!File.Exists(temp)) File.WriteAllText(temp,contents,new UTF8Encoding(false));
                    try { File.Move(temp,destination); return name; }
                    catch (IOException) when (File.Exists(destination)) { }
                }
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
