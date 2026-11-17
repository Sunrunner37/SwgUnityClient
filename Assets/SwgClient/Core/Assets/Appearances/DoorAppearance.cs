using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.Pob;
using Swg.Client.Core.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Core.Assets.Appearances
{
    /// <summary>Portal doors use the shared door_style table for meshes, sliding offsets and timing.</summary>
    public sealed class DoorAppearance
    {
        public PortalLayout.Door Portal;
        public AppearanceData Frame;
        public AppearanceData FirstPanel;
        public AppearanceData SecondPanel;
        public bool FlipSecondPanel;
        public Vector3 Movement;
        public float OpenSeconds;
        public float CloseSeconds;
        public float Spring;
        public float Smoothness;
        public float TriggerRadius;

        internal static List<DoorAppearance> Load(TreAssetLoader loader, PortalLayout layout, int detailLevel, IReadOnlyDictionary<string, short> customization)
        {
            var result = new List<DoorAppearance>();
            if (layout.Doors.Count == 0) return result;
            var table = DataTableFile.Parse(loader.Read("datatables/appearance/door_style.iff"));
            foreach (var portal in layout.Doors)
            {
                var row = table.Rows.FirstOrDefault(value => value.Get<string>("doorStyleName") == portal.Style);
                if (row == null)
                {
                    Debug.LogWarning("Unknown portal door style: " + portal.Style);
                    continue;
                }
                if (row.Get<bool>("isForceField"))
                {
                    Debug.LogWarning("Portal force-field appearance is not supported: " + portal.Style);
                    continue;
                }
                AppearanceData Read(string column)
                {
                    string path = row.Get<string>(column);
                    return string.IsNullOrEmpty(path) ? null : AppearanceLoader.LoadDetailed(loader, path, detailLevel, customization);
                }
                result.Add(new DoorAppearance { Portal = portal, Frame = Read("frameAppearance"),
                    FirstPanel = Read("doorAppearance"), SecondPanel = Read("doorAppearance2"),
                    FlipSecondPanel = row.Get<bool>("doorFlip2"),
                    Movement = new Vector3(row.Get<float>("doorMoveX"), row.Get<float>("doorMoveY"), row.Get<float>("doorMoveZ")),
                    OpenSeconds = row.Get<float>("openTime"), CloseSeconds = row.Get<float>("closeTime"),
                    Spring = row.Get<float>("springiness"), Smoothness = row.Get<float>("smoothness"), TriggerRadius = row.Get<float>("triggerRadius") });
            }
            return result;
        }
    }
}
