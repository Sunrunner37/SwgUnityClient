using UnityEngine;

namespace Swg.Client.Core.Characters.Animation
{
    /// <summary>Bind-space geometry and native skin influences for Unity GPU skinning.</summary>
    public sealed class SkeletalMeshData
    {
        public SkeletonData Skeleton;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public byte[] BonesPerVertex;
        public BoneWeight1[] Weights;
    }
}
