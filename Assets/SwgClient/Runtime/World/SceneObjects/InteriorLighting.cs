using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Pob;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Authored cell ambient/directional lighting, with spatial point lights shared across the building.</summary>
    internal sealed class InteriorLighting
    {
        // Per-renderer shader budget, not an asset-format limit. Keep the strongest contributors.
        internal const int LightCapacity = 16;
        readonly Color ambient;
        readonly (Vector4 Position, Color Color, Vector3 Attenuation, Vector4 Direction)[] lights;

        public InteriorLighting(PortalLayout.Cell cell, Matrix4x4 building, IEnumerable<PortalLayout.RoomLight> pointLights)
        {
            ambient = Color.black;
            var direct = new List<(Vector4 Position, Color Color, Vector3 Attenuation, Vector4 Direction)>();
            foreach (var light in cell.Lights)
            {
                if (light.Type == PortalLayout.LightType.Ambient)
                    ambient += light.Diffuse;
                else if (light.Type == PortalLayout.LightType.Directional)
                {
                    // Native K is the light's outgoing direction (D3D9 Direction); shading uses -K.
                    Vector3 direction = -(Vector3)(building * light.Transform).GetColumn(2);
                    direction.Normalize();
                    direct.Add((new Vector4(direction.x, direction.y, direction.z, 0), light.Diffuse, Vector3.right, Vector4.zero));
                }
            }
            foreach (var light in pointLights)
            {
                Vector3 position = (building * light.Transform).GetColumn(3);
                Vector3 direction = -(Vector3)(building * light.Transform).GetColumn(2);
                direction.Normalize();
                // Ceiling-directed fixtures (within 60 degrees of down) use soft cosine emission.
                // This keeps a display's pool below it; un-oriented point lights stay omnidirectional.
                Vector3 down = -(Vector3)building.GetColumn(1).normalized;
                var emission = Vector3.Dot(direction, down) > .5f ? new Vector4(direction.x, direction.y, direction.z, 1) : Vector4.zero;
                direct.Add((new Vector4(position.x, position.y, position.z, 1), light.Diffuse, light.Attenuation, emission));
            }
            lights = direct.ToArray();
        }

        // Keep authored lighting in display space until after the shader applies its legacy
        // attenuation and accumulates it. Converting each source first brightens distant lights.
        public void Apply(GameObject body)
        {
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty("_SwgInteriorLighting")) continue;
                var positions = new Vector4[LightCapacity];
                var colors = new Vector4[LightCapacity];
                var attenuation = new Vector4[LightCapacity];
                var directions = new Vector4[LightCapacity];
                float Influence((Vector4 Position, Color Color, Vector3 Attenuation, Vector4 Direction) light)
                {
                    if (light.Position.w == 0) return float.PositiveInfinity;
                    float distance = Vector3.Distance(light.Position, renderer.bounds.center);
                    var falloff = light.Attenuation;
                    return light.Color.maxColorComponent / Mathf.Max(.001f, falloff.x + falloff.y * distance + falloff.z * distance * distance);
                }
                var selected = lights.Length <= LightCapacity ? lights : lights.OrderByDescending(Influence).Take(LightCapacity).ToArray();
                for (int i = 0; i < selected.Length; i++)
                {
                    positions[i] = selected[i].Position;
                    colors[i] = selected[i].Color;
                    attenuation[i] = selected[i].Attenuation;
                    directions[i] = selected[i].Direction;
                }
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_SwgInteriorLighting", 1);
                properties.SetVector("_SwgInteriorAmbient", (Vector4)ambient);
                properties.SetInt("_SwgInteriorLightCount", selected.Length);
                properties.SetVectorArray("_SwgInteriorLightPositions", positions);
                properties.SetVectorArray("_SwgInteriorLightColors", colors);
                properties.SetVectorArray("_SwgInteriorLightAttenuation", attenuation);
                properties.SetVectorArray("_SwgInteriorLightDirections", directions);
                renderer.SetPropertyBlock(properties);
            }
        }

        public static void Reset(GameObject body)
        {
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_SwgInteriorLighting", 0);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
