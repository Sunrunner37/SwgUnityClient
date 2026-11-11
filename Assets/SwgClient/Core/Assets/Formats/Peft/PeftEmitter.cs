using System;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Peft
{
    public sealed class PeftEmitter
    {
        public string Name;
        public PeftWaveform[] Waves;
        public float Delay;
        public float LoopDelay;
        public int Loops;
        public int Direction;
        public int GenerationType;
        public int Shape;
        public bool LoopImmediately;
        public float LifetimeMinimum;
        public float LifetimeMaximum;
        public float MaximumParticles;
        public bool OneShot;
        public int BurstMinimum;
        public int BurstMaximum;
        public bool RandomRotation;
        public int Orientation;
        public bool Visible;
        public string SoundPath;
        public bool LocalSpace;
        public bool GroundCollision;
        public bool KillOnCollision;
        public float CollisionHeight;
        public Vector2 ForwardVelocityMaintained;
        public Vector2 UpVelocityMaintained;
        public float WindResistance;
        public float LodMinimum;
        public float LodMaximum;
        public int FlockingType;
        public float SeparationDistance;
        public float AlignmentGain;
        public float CohesionGain;
        public float SeparationGain;
        public float CageWidth;
        public float CageHeight;
        public int CageShape;
        public int CageEdgeType;
        public float HeadingChangeRadians;
        public float HeadingChangeTime;
        public float TimeOfDayColor;
        public bool SnapToTerrain;
        public bool AlignToTerrain;
        public float SnapHeight;
        public float ChangeDirectionRadians;
        public float ChangeDirectionTime;
        public bool FirstImmediately;
        public bool Priority;
        public string ParticleName;
        public PeftAttachment[] Attachments;
        public bool RandomRotationDirection;
        public PeftWaveform Alpha;
        public PeftWaveform SpeedScale;
        public PeftWaveform[] RelativeRotation;
        public PeftWaveform[] ParticleWaves;
        public GradientColorKey[] Colors;
        public string MeshPath;
        public string ShaderPath;
        public int FrameCount = 1;
        public int FirstFrame;
        public int LastFrame;
        public float FrameUvSize = 1;
        public int FramesPerColumn = 1;
        public float FramesPerSecond;
        public bool TextureVisible = true;
        public bool LinkedDimensions;
        internal static PeftEmitter Read(IffNode root)
        {
            var version = PeftFile.Version(root, 11, 14);
            var result = new PeftEmitter();
            var timing = PeftFile.Timing(version);
            result.Delay = timing.Delay;
            result.LoopDelay = timing.LoopDelay;
            result.Loops = timing.Loops;
            result.Waves = version.Children.Where(n => n.Kind == "WVFM").Select(PeftWaveform.Read).ToArray();
            if (result.Waves.Length != 16)
                throw new InvalidDataException("EMTR must have 16 waveforms");
            var reader = new ByteReader(version.Chunk("0000"));
            result.Name = reader.CString();
            result.Direction = (int)reader.U32();
            result.GenerationType = (int)reader.U32();
            result.Shape = (int)reader.U32();
            result.LoopImmediately = reader.Byte() != 0;
            result.LifetimeMinimum = reader.Float();
            result.LifetimeMaximum = reader.Float();
            result.MaximumParticles = reader.Float();
            result.OneShot = reader.Byte() != 0;
            result.BurstMinimum = (int)reader.U32();
            result.BurstMaximum = (int)reader.U32();
            result.RandomRotation = reader.Byte() != 0;
            result.Orientation = (int)reader.U32();
            result.Visible = reader.Byte() != 0;
            if (reader.U32() != 1)
                throw new InvalidDataException("Expected one particle description per emitter");
            int type = (int)reader.U32();
            result.SoundPath = reader.CString();
            result.LocalSpace = reader.Byte() != 0;
            result.GroundCollision = reader.Byte() != 0;
            result.KillOnCollision = reader.Byte() != 0;
            if (version.Kind == "0011")
                reader.Float(); // Removed collision offset.
            result.CollisionHeight = reader.Float();
            result.ForwardVelocityMaintained = new Vector2(reader.Float(), reader.Float());
            result.UpVelocityMaintained = new Vector2(reader.Float(), reader.Float());
            result.WindResistance = reader.Float();
            result.LodMinimum = reader.Float();
            result.LodMaximum = reader.Float();
            result.FlockingType = version.Kind == "0011" ? (reader.Byte() == 0 ? 0 : 1) : (int)reader.U32();
            result.SeparationDistance = reader.Float();
            result.AlignmentGain = reader.Float();
            result.CohesionGain = reader.Float();
            result.SeparationGain = reader.Float();
            result.CageWidth = reader.Float();
            if (version.Kind == "0011")
            {
                float minimum = reader.Float();
                result.CageHeight = reader.Float() - minimum;
                result.TimeOfDayColor = reader.Float();
            }
            else
            {
                result.CageHeight = reader.Float();
                result.CageShape = (int)reader.U32();
                result.CageEdgeType = (int)reader.U32();
                result.HeadingChangeRadians = reader.Float();
                result.HeadingChangeTime = reader.Float();
                result.TimeOfDayColor = reader.Float();
                result.SnapToTerrain = reader.Byte() != 0;
                result.AlignToTerrain = reader.Byte() != 0;
                result.SnapHeight = reader.Float();
                result.ChangeDirectionRadians = reader.Float();
                result.ChangeDirectionTime = reader.Float();
                if (version.Kind != "0012")
                    result.FirstImmediately = reader.Byte() != 0;
                if (version.Kind == "0014")
                    result.Priority = reader.Byte() != 0;
            }

            if (reader.Remaining != 0)
                throw new InvalidDataException("Trailing EMTR fields");
            var particle = version.Children.Single(n => n.Kind == (type == 0 ? "PTQD" : "PTMH"));
            var common = PeftFile.Version(particle.Children.Single(n => n.Kind == "PTCL"), 0, 3);
            var commonData = new ByteReader(common.Chunk("0000"));
            result.ParticleName = commonData.CString();
            result.RandomRotationDirection = commonData.Byte() != 0;
            int attachmentCount = int.Parse(common.Kind) >= 2 ? (int)commonData.U32() : 0;
            result.Attachments = common.Children.Where(n => n.Kind == "PATT").Select(PeftAttachment.Read).ToArray();
            if (result.Attachments.Length != attachmentCount)
                throw new InvalidDataException("Particle attachment count mismatch");
            var waves = common.Children.Where(n => n.Kind == "WVFM").Select(PeftWaveform.Read).ToArray();
            result.Alpha = waves[0];
            result.SpeedScale = waves[1];
            result.RelativeRotation = waves.Skip(2).ToArray();
            var color = common.Children.Single(n => n.Kind == "CLRR").Children.Single();
            var colors = new ByteReader(color.Data);
            colors.U32();
            if (color.Tag == "0001")
                colors.U32();
            result.Colors = new GradientColorKey[colors.U32()];
            for (int i = 0; i < result.Colors.Length; i++)
            {
                float time = colors.Float();
                result.Colors[i] = new GradientColorKey(new Color(colors.Float(), colors.Float(), colors.Float()), time);
            }

            if (type == 1)
            {
                // PTMH stores its version as a chunk, followed by four sibling waveforms.
                result.MeshPath = new ByteReader(particle.Chunk("0000")).CString();
                result.ParticleWaves = particle.Children.Where(n => n.Kind == "WVFM").Select(PeftWaveform.Read).ToArray();
            }
            else
            {
                var pv = particle.Children.Last(n => n.Tag == "FORM");
                result.ParticleWaves = pv.Children.Where(n => n.Kind == "WVFM").Select(PeftWaveform.Read).ToArray();
                var texture = new ByteReader(pv.Children.Single(n => n.Kind == "PTEX").Chunk("0000"));
                result.ShaderPath = texture.CString();
                result.FrameCount = (int)texture.U32();
                result.FirstFrame = (int)texture.U32();
                result.LastFrame = (int)texture.U32();
                result.FrameUvSize = texture.Float();
                result.FramesPerColumn = (int)texture.U32();
                result.FramesPerSecond = texture.Float();
                result.TextureVisible = texture.Byte() != 0;
                if (pv.Kind == "0001")
                    result.LinkedDimensions = new ByteReader(pv.Chunk("0000")).Byte() != 0;
            }

            return result;
        }
    }
}
