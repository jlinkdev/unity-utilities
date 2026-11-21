using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace JLink.Pots.Editor
{
    public static class PotObjExporter
    {
        static readonly string[] Slots = { "Exterior","Interior","Handles","Saucer" };
        static readonly Encoding Utf8 = new UTF8Encoding(false);
        static string F(float value) => value.ToString("R",CultureInfo.InvariantCulture);

        /// <summary>Write a Y-up OBJ in metres and companion material definitions for Painter texture sets.</summary>
        public static void Write(Mesh mesh,string path)
        {
            path = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(path);
            string temporary = Path.Combine(directory,Guid.NewGuid().ToString("N")+".tmp");
            try
            {
                string library = MaterialLibrary(directory);
                using (var writer = new StreamWriter(temporary,false,Utf8))
                {
                    writer.WriteLine("mtllib "+library);
                    writer.WriteLine("o Pot");
                    // Reflect X for the right-handed interchange mesh and reverse triangle winding.
                    foreach (var p in mesh.vertices) writer.WriteLine($"v {F(-p.x)} {F(p.y)} {F(p.z)}");
                    foreach (var t in mesh.uv) writer.WriteLine($"vt {F(t.x)} {F(t.y)}");
                    foreach (var n in mesh.normals) writer.WriteLine($"vn {F(-n.x)} {F(n.y)} {F(n.z)}");
                    for (int group = 0; group < mesh.subMeshCount; group++)
                    {
                        var triangles = mesh.GetTriangles(group);
                        if (triangles.Length == 0) continue;
                        writer.WriteLine("usemtl "+(mesh.subMeshCount == 1 ? "Pot" : Slots[group]));
                        for (int i = 0; i < triangles.Length; i += 3)
                        {
                            int a = triangles[i]+1, b = triangles[i+1]+1, c = triangles[i+2]+1;
                            writer.WriteLine($"f {a}/{a}/{a} {c}/{c}/{c} {b}/{b}/{b}");
                        }
                    }
                }
                if (File.Exists(path)) File.Replace(temporary,path,null);
                else File.Move(temporary,path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        static string MaterialLibrary(string directory)
        {
            var definitions = new StringBuilder("newmtl Pot\nKd 1 1 1\n");
            foreach (string slot in Slots) definitions.Append("newmtl ").Append(slot).Append("\nKd 1 1 1\n");
            string expected = definitions.ToString();
            string temporary = Path.Combine(directory,Guid.NewGuid().ToString("N")+".tmp");
            try
            {
                for (int i = 0; ; i++)
                {
                    string name = i == 0 ? "PotterySlots.mtl" : $"PotterySlots_{i}.mtl";
                    string file = Path.Combine(directory,name);
                    if (File.Exists(file))
                    {
                        if (new FileInfo(file).Length == Utf8.GetByteCount(expected) && File.ReadAllText(file) == expected) return name;
                        continue;
                    }
                    if (!File.Exists(temporary)) File.WriteAllText(temporary,expected,Utf8);
                    try { File.Move(temporary,file); return name; }
                    catch (IOException) when (File.Exists(file)) { continue; }
                }
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
