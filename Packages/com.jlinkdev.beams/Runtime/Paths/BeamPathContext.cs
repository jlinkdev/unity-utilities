namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Frame data shared by path providers and modifiers.</summary>
    public readonly struct BeamPathContext
    {
        public BeamPathContext(BeamEndpoint source, BeamEndpoint target, float time, float deltaTime, int seed)
        {
            Source = source;
            Target = target;
            Time = time;
            DeltaTime = deltaTime;
            Seed = seed;
        }

        public BeamEndpoint Source { get; }
        public BeamEndpoint Target { get; }
        public float Time { get; }
        public float DeltaTime { get; }
        public int Seed { get; }
    }
}
