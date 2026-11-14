using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Core.Assets.Appearances
{
    /// <summary>Native mount tables attach a customized vehicle body and its client-data exhaust to a skeletal control appearance.</summary>
    internal static class VehicleBodyAppearance
    {
        sealed class Body
        {
            public string AppearancePath;
            public string RiderPose;
            public (string AppearancePath, string Hardpoint)[] Thrusters;
        }
        static readonly ConditionalWeakTable<TreAssetLoader, Dictionary<string, Body>> bodies = new ConditionalWeakTable<TreAssetLoader, Dictionary<string, Body>>();

        public static bool TryGet(TreAssetLoader loader, string skeletalAppearance, out string body)
        {
            body = null;
            if (skeletalAppearance == null || !bodies.GetValue(loader, Load).TryGetValue(skeletalAppearance, out var definition)) return false;
            body = definition.AppearancePath;
            return true;
        }

        public static bool TryLoad(TreAssetLoader loader, string skeletalAppearance, IReadOnlyDictionary<string, short> customization, out AppearanceData body)
        {
            body = null;
            if (skeletalAppearance == null || !bodies.GetValue(loader, Load).TryGetValue(skeletalAppearance, out var definition)) return false;
            body = AppearanceLoader.LoadDetailed(loader, definition.AppearancePath, customization: customization);
            if (body.Hardpoints.TryGetValue("player", out var seat))
                body.RiderSeat = new RiderSeatData(seat, definition.RiderPose);
            foreach (var thruster in definition.Thrusters)
            {
                if (!body.Hardpoints.TryGetValue(thruster.Hardpoint, out var transform))
                {
                    Debug.LogWarning("Vehicle exhaust hardpoint not found: " + thruster.Hardpoint + " in " + definition.AppearancePath);
                    continue;
                }
                var effect = AppearanceLoader.LoadDetailed(loader, thruster.AppearancePath);
                body.Particles.AddRange(effect.Particles.Select(particle => particle.WithTransform(transform, vehicleExhaust: true)));
            }
            return true;
        }

        static Dictionary<string, Body> Load(TreAssetLoader loader)
        {
            var poses = DataTableFile.Parse(loader.Read("datatables/mount/rider_pose_map.iff")).Rows
                .Where(row => row.Get<int>("seat_index") == 1).ToDictionary(row => row.Get<string>("saddle_appearance_name"), row => row.Get<string>("rider_pose"), StringComparer.OrdinalIgnoreCase);
            var appearances = DataTableFile.Parse(loader.Read("datatables/mount/saddle_appearance_map.iff")).Rows
                .Where(row => row.Get<string>("client_data_filename").StartsWith("clientdata/vehicle/", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(row => row.Get<string>("logical_saddle_name"), row => new Body
                {
                    AppearancePath = row.Get<string>("saddle_appearance_name"),
                    RiderPose = poses[row.Get<string>("saddle_appearance_name")],
                    Thrusters = ReadThrusters(loader.Read(row.Get<string>("client_data_filename")))
                }, StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, Body>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in DataTableFile.Parse(loader.Read("datatables/mount/logical_saddle_name_map.iff")).Rows)
                if (appearances.TryGetValue(row.Get<string>("logical_saddle_name"), out var body))
                    result.Add(row.Get<string>("sat_or_skt_name"), body);
            return result;
        }

        // CLDF/VTHR INFO stores the minimum damage fraction; HOBJ stores appearance and hardpoint names.
        // Use the undamaged exhaust set. Damage-dependent replacements can use the same definitions later.
        // Reference: SWG-Source/client-tools ClientDataFile_VehicleThrusterData.cpp and _HardpointChildObject.cpp.
        static (string AppearancePath, string Hardpoint)[] ReadThrusters(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "CLDF");
            var thrusters = root?.Children.SelectMany(version => version.Children).Where(node => node.Kind == "VTHR")
                .OrderBy(node => new ByteReader(node.Chunk("INFO")).Float()).FirstOrDefault();
            if (thrusters == null) return Array.Empty<(string, string)>();
            return thrusters.Children.Where(node => node.Tag == "HOBJ").Select(node =>
            {
                var reader = new ByteReader(node.Data);
                return (TreAssetLoader.Normalize(reader.CString()), reader.CString());
            }).ToArray();
        }
    }
}
