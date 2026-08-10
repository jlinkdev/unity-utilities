using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals
{
    /// <summary>Implement this on a CharacterController motor to preserve its velocity through portals.</summary>
    public interface IPortalVelocityProvider
    {
        Vector3 PortalVelocity { get; set; }
    }
}
