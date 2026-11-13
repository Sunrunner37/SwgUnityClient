using System.Collections.Generic;
using UnityEngine;

namespace Swg.Client.Runtime.UI
{
    /// <summary>Prefabs a widget instantiates, so their artwork can be prewarmed before they are visible.</summary>
    public interface IUiPrefabSource
    {
        IEnumerable<GameObject> UiPrefabs { get; }
    }
}
