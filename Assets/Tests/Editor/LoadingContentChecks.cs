using System;
using System.IO;
using Swg.Client.Core.Assets.Loading;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Tests.Editor
{
    public static class LoadingContentChecks
    {
        public static void Run()
        {
            var settings = Swg.Client.Core.Configuration.ClientSettings.From(System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            foreach (string planet in new[]
            {
                "corellia",
                "tatooine",
                "naboo",
                "talus",
                "rori",
                "dantooine",
                "dathomir",
                "endor",
                "lok",
                "yavin4"
            }

            )
            {
                var content = LoadingScreenDataLoader.Load(loader, planet, "datatables/loading/{planet}.iff", "datatables/loading/general.iff", 0);
                Check(!string.IsNullOrWhiteSpace(content.Title) && !string.IsNullOrWhiteSpace(content.Description), "Localized native loading content for " + planet);
                Check(content.Picture.Width > 0 && content.Picture.Height > 0, "Native loading illustration for " + planet);
                Check(content.PlanetImage != null && content.FlagImage != null, "Native planet artwork for " + planet);
                Check(!content.Title.StartsWith("@") && !content.Description.StartsWith("@"), "STF references resolved for " + planet);
            }

            foreach (string planet in new[]
            {
                "corellia",
                "talus",
                "unknown"
            }

            )
                for (int row = 0; row < 75; row++)
                    Check(LoadingScreenDataLoader.Load(loader, planet, "datatables/loading/{planet}.iff", "datatables/loading/general.iff", row).Picture != null, "Blank and misspelled native card entries are skipped");
            var radial = LoadingScreenDataLoader.Load(loader, "corellia", "datatables/loading/space_{planet}.iff", "datatables/loading/general.iff", 7);
            Check(radial.Title == "Starfighter Pilot Abilities" && radial.PicturePath.EndsWith("space_load_pilotability.dds"), "Native radial table row linkage");
            var fallback = LoadingScreenDataLoader.Load(loader, "unknown", "datatables/loading/{planet}.iff", "datatables/loading/general.iff", int.MinValue);
            Check(fallback.TablePath == "datatables/loading/general.iff" && fallback.Picture != null, "Unknown destination uses native general tips");
            try
            {
                LoadingScreenDataLoader.Load(loader, "../corellia", "datatables/loading/{planet}.iff", "datatables/loading/general.iff", 0);
                throw new Exception("Unsafe planet path accepted");
            }
            catch (InvalidDataException)
            {
            }
        }

        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }
    }
}
