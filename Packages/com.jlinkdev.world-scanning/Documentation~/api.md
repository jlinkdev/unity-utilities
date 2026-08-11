# Runtime API guide

Namespace: `jlinkdev.UnityUtilities.WorldScanning`

## ScanSystem

- `Emit(Vector3 origin, ScanProfile profile)` emits a profile with default orientation and multipliers.
- `Emit(in ScanEmission emission)` emits with origin, axis, range, duration, intensity, and shape overrides.
- `Cancel(ScanHandle handle)` and `CancelAll()` stop scans.
- `SetIntensity(ScanHandle handle, float multiplier)` adjusts a live pulse.
- `IsAlive`, `GetRadius`, and `GetNormalizedTime` query live state.
- `ScanStarted` and `ScanEnded` report global lifecycle events.
- `ActiveCount` and `MaximumActiveScans` expose capacity information.

## ScanHandle

The value-type handle remains safe when internal pulse slots are reused. `IsValid`, `Radius`, and `NormalizedTime` reflect live state; `Cancel()` and `SetIntensity()` forward to `ScanSystem`.

Do not persist handles as save-game identifiers. They are runtime lifecycle tokens.

## ScanEmitter

`Emit()` uses the component's configured origin, axis, profile, and multipliers. `CancelLast()` cancels only its most recently emitted live pulse. Subscribe to `ScanStarted` and `ScanEnded` for typed callbacks, or use the inspector UnityEvents.

## ScanReceiver

Subscribe to `Scanned` to receive typed `ScanHit` data. `LastHit` retains the latest notification for pull-based consumers.

```csharp
private void OnEnable() => receiver.Scanned += OnScanned;
private void OnDisable() => receiver.Scanned -= OnScanned;

private void OnScanned(ScanHit hit)
{
    Debug.Log($"Reached after {hit.Distance:0.0} metres");
}
```

## Lifecycle result

`ScanEndedEvent.Reason` is `Completed`, `Cancelled`, or `Replaced`. Use this distinction when scans drive cooldowns, missions, audio, or pooled effects.
