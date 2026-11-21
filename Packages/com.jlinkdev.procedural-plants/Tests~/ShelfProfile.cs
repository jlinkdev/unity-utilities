using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using JLink.Plants;
using JLink.Plants.Studio;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Copy into a validation player's Assets and launch it with -plantShelfProfile.
public sealed class ShelfProfile : MonoBehaviour
{
    const int Samples = 16;
    readonly ProceduralPlant[] shelf = new ProceduralPlant[18];
    ProfilerRecorder frameAllocations;
    byte[] calibration;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Environment.GetCommandLineArgs().Contains("-plantShelfProfile"))
            new GameObject("Shelf profile").AddComponent<ShelfProfile>();
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
        var bench = FindFirstObjectByType<WindowsillWorkshop>();
        if (bench) bench.enabled = false;
        for (int i = 0; i < shelf.Length; i++)
        {
            var go = new GameObject("Shelf specimen");
            go.transform.position = new Vector3(i % 6 - 2.5f, i / 6, 0);
            var plant = go.AddComponent<ProceduralPlant>();
            var recipe = PlantRecipe.Default;
            recipe.species = (PlantSpecies)(i % 6);
            recipe.seed = 40 + i;
            recipe.crowns = 1;
            plant.Generate(recipe, 240);
            plant.SetCareEnabled(true);
            shelf[i] = plant;
        }
        Debug.Log($"Shelf vertices={shelf.Sum(p => p.CurrentMesh.vertexCount)} CPU={SystemInfo.processorType}");
        frameAllocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        if (!frameAllocations.Valid) { Debug.LogError("Build a Development Player for allocation tracking."); Application.Quit(1); yield break; }
        yield return Measure("Idle", p => {}, p => {});
        yield return Measure("Allocation control", p => {}, p => calibration = new byte[4096]);
        yield return Measure("Hydrated drying", p => p.Water(), p => p.Advance(.125));
        yield return Measure("Wilt onset", p => { p.Advance(100); p.Water(.12f); }, p => p.Advance(.125));
        yield return Measure("Water wilted", p => p.Advance(100), p => p.Water());
        frameAllocations.Dispose();
        GC.KeepAlive(calibration);
        Application.Quit();
    }

    IEnumerator Measure(string label, Action<ProceduralPlant> prepare, Action<ProceduralPlant> update)
    {
        var times = new double[Samples];
        var allocations = new long[Samples];
        for (int sample = -2; sample < Samples; sample++)
        {
            foreach (var plant in shelf) prepare(plant);
            yield return null;
            long start = Stopwatch.GetTimestamp();
            foreach (var plant in shelf) update(plant);
            long ticks = Stopwatch.GetTimestamp() - start;
            yield return null;
            if (sample >= 0)
            {
                times[sample] = ticks * 1000.0 / Stopwatch.Frequency;
                allocations[sample] = frameAllocations.LastValue;
            }
            yield return null;
        }
        Array.Sort(times);
        Array.Sort(allocations);
        Debug.Log($"Shelf {label}: median_ms={(times[7]+times[8])*.5:F3} p95_ms={times[15]:F3} median_frame_bytes={(allocations[7]+allocations[8])/2}");
    }
}
