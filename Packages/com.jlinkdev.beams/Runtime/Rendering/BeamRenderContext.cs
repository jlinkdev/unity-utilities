namespace jlinkdev.UnityUtilities.Beams
{
    public readonly struct BeamRenderContext
    {
        public BeamRenderContext(float time, float deltaTime, float age, int seed)
        {
            Time = time;
            DeltaTime = deltaTime;
            Age = age;
            Seed = seed;
        }

        public float Time { get; }
        public float DeltaTime { get; }
        public float Age { get; }
        public int Seed { get; }
    }
}
