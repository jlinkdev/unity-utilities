# API guide

## Custom endpoint

```csharp
public sealed class SocketEndpoint : BeamEndpointProvider
{
    public Transform Socket;

    public override bool TryGetEndpoint(out BeamEndpoint endpoint)
    {
        if (Socket == null)
        {
            endpoint = default;
            return false;
        }

        endpoint = new BeamEndpoint(Socket.position, Socket.forward, Socket);
        return true;
    }
}
```

## Consume neutral contact ticks

```csharp
public sealed class BeamContactConsumer : MonoBehaviour
{
    [SerializeField] private BeamPhysicsContacts contacts;

    private void OnEnable() => contacts.ContactTicked += OnTick;
    private void OnDisable() => contacts.ContactTicked -= OnTick;

    private void OnTick(BeamPhysicsContacts sender, BeamContact contact)
    {
        // Interpret the contact in project code.
        Debug.Log($"{contact.Collider.name} at {contact.DistanceAlongStrand:0.00} m");
    }
}
```

## Custom path modifier

```csharp
public sealed class LiftBeamModifier : BeamPathModifier
{
    public float Height = 1f;

    public override void Modify(in BeamPathContext context, BeamPathBuffer paths)
    {
        for (int strandIndex = 0; strandIndex < paths.Count; strandIndex++)
        {
            BeamStrand strand = paths[strandIndex];
            for (int i = 1; i < strand.Count - 1; i++)
            {
                BeamPoint point = strand[i];
                point.Position += Vector3.up * Mathf.Sin(point.NormalizedDistance * Mathf.PI) * Height;
                strand[i] = point;
            }
            BeamPathUtility.RecalculateMetrics(strand, context.Source.Forward);
        }
    }
}
```

## Poll contacts into caller-owned storage

```csharp
private readonly List<BeamContact> contacts = new List<BeamContact>();

void ReadContacts(BeamPhysicsContacts source)
{
    source.GetContacts(contacts);
}
```

## Manual simulation time

```csharp
beam.TimeMode = BeamTimeMode.Manual;
beam.ManualTime = replayTime;
```
