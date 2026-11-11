using Swg.Client.Core;
using Swg.Client.Core.Assets;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.World;

namespace Swg.Client.Tests.Editor
{
    public static class WorldAssetChecks
    {
        public static void Run()
        {
            var settings = Swg.Client.Core.Configuration.ClientSettings.From(System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var snapshot = WorldSnapshot.Load(loader, "corellia");
            Check(snapshot.Entries.Count == 7074, "Native Corellia snapshot placement count");
            Check(snapshot.ById.Count == snapshot.Entries.Count, "Unique snapshot object IDs");
            Check(snapshot.Entries.Count(entry => entry.ParentSwgId == 0) == 5415, "Native root count");
            Check(snapshot.Entries.All(entry => Finite(entry.WorldPosition)), "Finite world positions");
            var center = new Vector3(-168.84143f, 36.617496f, -5050.737f);
            var terrain = Swg.Client.Core.World.Terrain.WorldTerrain.Load(loader, "terrain/corellia.trn");
            Check(Math.Abs(terrain.SampleHeight(center.x, center.z) - center.y) < .001f, "Native terrain matches live server spawn height");
            var patch = terrain.BuildPatch(center, 256, 8);
            Check(patch.Surfaces.Length > 1 && patch.Vertices.Length > 1000, "Native terrain geometry and texture families");
            var nearby = snapshot.Query(center, 500, 256).ToArray();
            Check(nearby.Length == 1205, "Native Misty nearby placements");
            Check(nearby.Select(entry => entry.TemplatePath).Distinct().Count() == 196, "Native nearby template diversity");
            var narrow = snapshot.Query(center, 400, 0).ToArray();
            Check(narrow.Length > 0 && narrow.Length < nearby.Length, "Radius query filters placements");
            foreach (var entry in snapshot.Entries.Where(entry => entry.ParentSwgId != 0))
            {
                if (!snapshot.ById.TryGetValue(entry.ParentSwgId, out var parent))
                    continue;
                var local = (parent.WorldRotation * entry.Position);
                Check(Close(entry.WorldPosition.x, parent.WorldPosition.x + local.x) && Close(entry.WorldPosition.y, parent.WorldPosition.y + local.y) && Close(entry.WorldPosition.z, parent.WorldPosition.z + local.z), "Parent-relative snapshot pose");
            }

            var samples = new[]
            {
                "object/static/structure/general/shared_debris_deathstar_conduit.iff",
                "object/building/corellia/shared_skyscraper_corellia_style_01.iff",
                "object/building/corellia/shared_starport_corellia.iff",
                "object/building/corellia/shared_cantina_corellia.iff",
                "object/tangible/terminal/shared_terminal_bazaar.iff"
            };
            foreach (string path in samples)
            {
                var model = StaticModel.LoadDetailed(loader, path);
                Check(model.Parts.Count > 0 && model.Parts.Count == model.Sources.Count, "Native render geometry " + path);
                Check(model.Warnings.Count == 0, "Supported native materials " + path);
                Check(model.Radius > 0 && Finite(model.BoundsMin) && Finite(model.BoundsMax), "Model bounds " + path);
                foreach (var part in model.Parts)
                {
                    Check(part.Vertices.Length > 0 && part.Vertices.All(Finite), "Finite model vertices");
                    Check(part.Normals.Length == part.Vertices.Length && part.Uvs.Length == part.Vertices.Length, "Vertex attribute alignment");
                    Check(part.Triangles.Length % 3 == 0 && part.Triangles.All(index => index >= 0 && index < part.Vertices.Length), "Triangle indices");
                    Check(part.Diffuse != null && part.Diffuse.Pixels.Length > 0, "Native diffuse texture");
                }
            }

            var catalog = new ObjectTemplateCatalog(loader);
            const string human = "object/creature/player/shared_human_female.iff";
            Check(catalog.Resolve(3567725168) == human, "Known native shared-template CRC");
            Check(catalog.Resolve(ObjectTemplateNames.TemplateCrc("object/creature/player/human_female.iff")) == human, "Server unshared-template CRC alias");
            var native = loader.Read("snapshot/corellia.ws");
            Reject(() => WorldSnapshot.Parse(Array.Empty<byte>()));
            Reject(() => WorldSnapshot.Parse(native.Take(native.Length - 1).ToArray()));
            var wrongKind = (byte[])native.Clone();
            wrongKind[8] = (byte)'X';
            Reject(() => WorldSnapshot.Parse(wrongKind));
        }

        static bool Finite(Vector3 point) => !float.IsNaN(point.x) && !float.IsInfinity(point.x) && !float.IsNaN(point.y) && !float.IsInfinity(point.y) && !float.IsNaN(point.z) && !float.IsInfinity(point.z);
        static bool Close(float first, float second) => Math.Abs(first - second) < 0.001f;
        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception("World asset check failed: " + message);
        }

        static void Reject(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }
            catch (EndOfStreamException)
            {
                return;
            }

            throw new Exception("Malformed snapshot was accepted");
        }
    }
}
