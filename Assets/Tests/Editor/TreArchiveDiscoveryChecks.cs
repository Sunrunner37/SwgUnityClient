using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Tests.Editor
{
    public sealed class TreArchiveDiscoveryChecks
    {
        string directory;
        [SetUp]
        public void CreateDirectory()
        {
            directory = Path.Combine(Path.GetTempPath(), "swg-tre-discovery-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void RemoveDirectory()
        {
            string full = Path.GetFullPath(directory);
            string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected test directory");
            Directory.Delete(full, true);
        }

        void Touch(params string[] names)
        {
            foreach (string name in names)
                File.WriteAllBytes(Path.Combine(directory, name), Array.Empty<byte>());
        }

        [Test]
        public void UsesNumericPatchVersionsAndThePreCuSkuInterleave()
        {
            Touch("data_sku1_06.tre", "patch_11_01.tre", "patch_9.tre", "patch_10.tre", "data_sku1_05.tre", "patch_11_00.tre", "patch_11_02.tre", "data_sku1_07.tre", "patch_11_03.tre", "patch_12_00.tre", "patch_sku1_12_00.tre", "default_patch.tre", "bottom.tre", "options.cfg");
            Assert.That(TreArchiveDiscovery.Discover(directory), Is.EqualTo(new[] { "default_patch.tre", "patch_sku1_12_00.tre", "patch_12_00.tre", "patch_11_03.tre", "data_sku1_07.tre", "patch_11_02.tre", "data_sku1_06.tre", "patch_11_01.tre", "patch_11_00.tre", "data_sku1_05.tre", "patch_10.tre", "patch_9.tre", "bottom.tre" }));
        }

        [Test]
        public void HotfixesOverrideTheirPatchButNotNewerReleases()
        {
            Touch("patch_13_00.tre", "hotfix_13_1_00.tre", "patch_14_00.tre", "patch_sku1_12_00.tre", "hotfix_sku1_12_1_00.tre");
            Assert.That(TreArchiveDiscovery.Discover(directory), Is.EqualTo(new[] { "patch_14_00.tre", "hotfix_13_1_00.tre", "patch_13_00.tre", "hotfix_sku1_12_1_00.tre", "patch_sku1_12_00.tre" }));
        }

        [Test]
        public void ExplicitCustomPriorityWinsAndMissingOverridesAreRejected()
        {
            Touch("default_patch.tre", "my_mod.tre");
            Assert.That(TreArchiveDiscovery.Discover(directory, new Dictionary<string, int> { ["MY_MOD.TRE"] = 2000000 }), Is.EqualTo(new[] { "my_mod.tre", "default_patch.tre" }));
            Assert.Throws<InvalidDataException>(() => TreArchiveDiscovery.Discover(directory, new Dictionary<string, int> { ["missing.tre"] = 20 }));
        }

        [Test]
        public void UnknownNamesWarnInsteadOfPretendingToHaveKnownPriority()
        {
            Touch("custom.tre", "bottom.tre");
            LogAssert.Expect(LogType.Warning, "Unknown TRE archive 'custom.tre': only nonconflicting assets can be mounted without a [TreArchivePriorities] override.");
            Assert.That(TreArchiveDiscovery.Discover(directory), Is.EqualTo(new[] { "bottom.tre", "custom.tre" }));
        }

        [Test]
        public void NoArchivesIsAnError()
        {
            Touch("options.cfg");
            Assert.Throws<InvalidDataException>(() => TreArchiveDiscovery.Discover(directory));
        }
    }
}
