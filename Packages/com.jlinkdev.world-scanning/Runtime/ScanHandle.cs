using System;

namespace jlinkdev.UnityUtilities.WorldScanning
{
    public readonly struct ScanHandle : IEquatable<ScanHandle>
    {
        internal ScanHandle(int id, uint generation)
        {
            Id = id;
            Generation = generation;
        }

        public static ScanHandle Invalid => default;
        public int Id { get; }
        internal uint Generation { get; }
        public bool IsValid => Id != 0 && ScanSystem.IsAlive(this);
        public float NormalizedTime => ScanSystem.GetNormalizedTime(this);
        public float Radius => ScanSystem.GetRadius(this);

        public bool Cancel()
        {
            return ScanSystem.Cancel(this);
        }

        public bool SetIntensity(float multiplier)
        {
            return ScanSystem.SetIntensity(this, multiplier);
        }

        public bool Equals(ScanHandle other)
        {
            return Id == other.Id && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is ScanHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (int)Generation;
            }
        }

        public static bool operator ==(ScanHandle left, ScanHandle right) => left.Equals(right);
        public static bool operator !=(ScanHandle left, ScanHandle right) => !left.Equals(right);
        public override string ToString() => Id == 0 ? "Invalid Scan" : $"Scan {Id}:{Generation}";
    }
}
