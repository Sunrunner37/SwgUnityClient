using System;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Sound
{
    /// <summary>SD2D/SD3D sound templates describe samples, repetition, fades, volume and distance attenuation.</summary>
    /// <seealso href="https://github.com/SWG-Source/client-tools/blob/master/src/engine/client/library/clientAudio/src/win32/Sound2dTemplate.cpp"/>
    public sealed class SoundTemplate
    {
        public string[] Samples;
        public Vector2 StartDelay;
        public Vector2 FadeIn;
        public Vector2 FadeOut;
        public Vector2 LoopDelay;
        public Vector2 Volume;
        public Vector2 Pitch;
        public int LoopCountMin;
        public int LoopCountMax;
        public int PlayOrder;
        public bool Spatial;
        public float MinimumDistance;
        public bool Infinite => LoopCountMin == -1 || LoopCountMax == -1;
        // The native default falloff power is 3: distanceMin * 2^(3-1).
        public float MaximumDistance => MinimumDistance * 4;
        public static string ReadSamplePath(byte[] bytes) => Parse(bytes).Samples[0];
        public static SoundTemplate Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            var sound = IffNode.Form(roots, "SD2D") ?? throw new InvalidDataException("Expected SD2D sound settings");
            var version = sound.Children.FirstOrDefault(node => node.Tag == "0003" || node.Tag == "0002")
                ?? throw new InvalidDataException("Unsupported sound template version");
            var reader = new ByteReader(version.Data);
            var result = new SoundTemplate();
            int count = checked((int)reader.U32());
            if (count <= 0) throw new InvalidDataException("Sound template has no samples");
            result.Samples = new string[count];
            for (int i = 0; i < count; i++) result.Samples[i] = reader.CString();
            Vector2 Range() => new Vector2(reader.Float(), reader.Float());
            result.StartDelay = Range(); result.FadeIn = Range();
            result.LoopCountMin = unchecked((int)reader.U32());
            result.LoopCountMax = unchecked((int)reader.U32());
            if (version.Tag == "0002" && result.LoopCountMin >= 0) { result.LoopCountMin++; result.LoopCountMax++; }
            result.LoopDelay = Range(); result.FadeOut = Range();
            reader.U32(); // Sound category.
            result.PlayOrder = (int)reader.U32();
            reader.Skip(20); // Play count and loop/fade/volume sampling modes.
            reader.Float(); // Volume sampling rate.
            result.Volume = Range();
            reader.Float(); // Volume interpolation rate.
            reader.U32(); reader.Float(); // Pitch sampling mode and rate.
            result.Pitch = Range();
            reader.Float(); reader.U32(); // Pitch interpolation and priority.
            result.MinimumDistance = reader.Float();
            var spatial = IffNode.Form(roots, "SD3D");
            result.Spatial = spatial != null;
            var legacy = spatial?.Children.FirstOrDefault(node => node.Tag == "0000");
            if (legacy != null) result.MinimumDistance = new ByteReader(legacy.Data).Float();
            if (result.MinimumDistance > 0 || result.Spatial) result.MinimumDistance = Math.Max(2, result.MinimumDistance);
            return result;
        }
    }
}
