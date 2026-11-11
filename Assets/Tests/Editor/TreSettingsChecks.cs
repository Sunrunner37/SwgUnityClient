using System;
using System.IO;
using NUnit.Framework;
using Swg.Client.Core.Configuration;

namespace Swg.Client.Tests.Editor
{
    public sealed class TreSettingsChecks
    {
        [Test]
        public void TreFolderIsRelativeToIniAndOnlyCustomPrioritiesAreOptional()
        {
            string root = Path.Combine(Path.GetTempPath(), "swg-tre-settings-" + Guid.NewGuid());
            Directory.CreateDirectory(root);
            try
            {
                string ini = Path.Combine(root, "client.ini");
                File.WriteAllText(ini, "[Assets]\nTreDirectory=archives\n[Network]\nClientVersion=test\n");
                var settings = ClientSettings.From(ini);
                Assert.That(settings.TreDirectory, Is.EqualTo(Path.Combine(root, "archives")));
                Assert.That(settings.TreArchivePriorities, Is.Empty);
                Assert.That(File.Exists(Path.Combine(root, "swgemu.cfg")), Is.False);
                File.WriteAllText(ini, "[Assets]\nTreDirectory=archives\n");
                Assert.That(ClientSettings.From(ini).TreArchivePriorities, Is.Empty);
                File.WriteAllText(ini, "[Assets]\nTreDirectory=archives\n[TreArchivePriorities]\nmy_mod.tre=2000000\n");
                Assert.That(ClientSettings.From(ini).TreArchivePriorities["my_mod.tre"], Is.EqualTo(2000000));
                File.WriteAllText(ini, "[Assets]\nDirectory=old-game-folder\n");
                Assert.Throws<InvalidDataException>(() => ClientSettings.From(ini));
            }
            finally
            {
                string full = Path.GetFullPath(root);
                string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected test directory");
                Directory.Delete(full, true);
            }
        }
    }
}
