using Swg.Client.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Inventory
{
    public sealed class InventoryItem : MonoBehaviour
    {
        public Button button;
        public RawImage image;
        public TMP_Text itemName;
        public TMP_Text quantity;
        public GameObject selection;
        public GameObject equipped;
        public SwgId SwgId { get; private set; }
        public void Bind(InventoryWindow window, SwgId swgId)
        {
            SwgId = swgId;
            button.onClick.AddListener(() => window.Select(swgId));
        }
    }
}
