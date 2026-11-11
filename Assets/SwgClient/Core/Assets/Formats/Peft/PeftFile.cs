using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Peft
{
    /// <summary>PEFT is SWG's IFF particle-effect format: timed groups of emitters, authored waveforms, particles and animated textures.</summary>
    /// <seealso href="https://github.com/swg-source/client-tools/tree/master/src/engine/client/library/clientParticle/src/shared"/>
    public sealed class PeftFile
    {
        public readonly List<PeftEmitter> Emitters = new List<PeftEmitter>();
        public float Scale = 1;
        public float PlaybackRate = 1;
        public float InitialPlaybackRate = 1;
        public float InitialPlaybackTime;
        public static PeftFile Parse(byte[] bytes) => Parse(IffNode.Parse(bytes).Single());
        public static PeftFile Parse(IffNode root)
        {
            if (root.Kind != "PEFT")
                throw new InvalidDataException("Expected PEFT");
            var version = Version(root, 0, 2);
            var result = new PeftFile();
            var data = new ByteReader(version.Chunk("0000"));
            int count = (int)data.U32();
            if (version.Kind == "0002")
            {
                result.InitialPlaybackRate = data.Float();
                result.InitialPlaybackTime = data.Float();
                result.PlaybackRate = data.Float();
                result.Scale = data.Float();
            }

            var groups = version.Children.Where(n => n.Kind == "EMGP").ToArray();
            if (groups.Length != count)
                throw new InvalidDataException("PEFT group count mismatch");
            var effectTiming = Timing(version);
            foreach (var group in groups)
            {
                var gv = Version(group, 0, 1);
                var groupTiming = Timing(gv);
                int emitters = (int)new ByteReader(gv.Chunk("0000")).U32();
                var children = gv.Children.Where(n => n.Kind == "EMTR").ToArray();
                if (children.Length != emitters)
                    throw new InvalidDataException("PEFT emitter count mismatch");
                foreach (var emitter in children)
                {
                    var parsed = PeftEmitter.Read(emitter);
                    parsed.Delay += effectTiming.Delay + groupTiming.Delay;
                    result.Emitters.Add(parsed);
                }
            }

            return result;
        }

        internal static IffNode Version(IffNode root, int minimum, int maximum)
        {
            var version = root.Children.First(n => n.Tag == "FORM");
            if (!int.TryParse(version.Kind, out int value) || value < minimum || value > maximum)
                throw new NotSupportedException(root.Kind + " version " + version.Kind);
            return version;
        }

        internal static (float Delay, float LoopDelay, int Loops) Timing(IffNode version)
        {
            var timing = version.Children.FirstOrDefault(n => n.Kind == "PTIM");
            if (timing == null)
                return (0, 0, -1);
            var reader = new ByteReader(timing.Children.Single().Data);
            float delay = (reader.Float() + reader.Float()) * .5f;
            float loopDelay = (reader.Float() + reader.Float()) * .5f;
            int loops = (int)reader.U32();
            reader.U32();
            return (delay, loopDelay, loops);
        }
    }
}
