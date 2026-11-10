using UnityEngine;

namespace Swg.Client.Runtime.UI.Screens
{
    /// <summary>Passive readiness information for editor diagnostics; screens own their Unity lifecycle.</summary>
    public abstract class ClientScreen : MonoBehaviour
    {
        public bool IsReady { get; protected set; }
    }
}
