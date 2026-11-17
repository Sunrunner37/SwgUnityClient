using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Pob;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Sht;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.World.SceneObjects;

namespace Swg.Client.Tests.Editor
{
    public sealed class InteriorAssetChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            write(writer); return stream.ToArray();
        }
        static byte[] Chunk(string tag, byte[] bytes) => Bytes(writer =>
        {
            writer.Write(Encoding.ASCII.GetBytes(tag)); writer.Write(System.Net.IPAddress.HostToNetworkOrder(bytes.Length)); writer.Write(bytes);
        });
        static byte[] Form(string kind, params byte[][] children) => Chunk("FORM", Encoding.ASCII.GetBytes(kind).Concat(children.SelectMany(child => child)).ToArray());
        [Test]
        public void InteriorLayoutDecodesNamedCellsAndAffineTransforms()
        {
            var transform = Matrix4x4.TRS(new Vector3(2, 3, 4), Quaternion.Euler(0, 90, 0), Vector3.one);
            var bytes = Form("INLY", Form("0000", Chunk("NODE", Bytes(writer =>
            {
                writer.Write(Encoding.ASCII.GetBytes("object/test.iff\0room_a\0"));
                for (int row = 0; row < 3; row++) for (int column = 0; column < 4; column++) writer.Write(transform[row, column]);
            }))));
            var entry = IlfFile.Parse(bytes).Entries.Single();
            Assert.That(entry.Template, Is.EqualTo("object/test.iff"));
            Assert.That(entry.Cell, Is.EqualTo("room_a"));
            Assert.That(Vector3.Distance(entry.Transform.MultiplyPoint3x4(Vector3.forward), new Vector3(3, 3, 4)), Is.LessThan(.0001f));
        }
        [Test]
        public void NativeStarportLoadsClientFurnishingsAndRoomLights()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "object/building/corellia/shared_starport_corellia.iff");
            Assert.That(appearance.Parts.Where((part, index) => appearance.Sources[index].ShaderPath == "shader/intr_tech_floor_c_cs8.sht").All(part => part.VertexColorAmbientOnly), Is.True);
            var cell = appearance.PortalLayout.Cells.Single(value => value.Number == 13);
            Assert.That(cell.Lights.Count, Is.EqualTo(4));
            Assert.That(cell.Lights.Count(light => light.Type == PortalLayout.LightType.Ambient), Is.EqualTo(1));
            Assert.That(cell.Lights[0].Diffuse.b, Is.GreaterThan(cell.Lights[0].Diffuse.r));
            Assert.That(cell.Lights[0].Transform.GetColumn(2).y, Is.GreaterThan(0), "Native exporter correction makes the overhead light face the room.");
            var decorations = appearance.Parts.Select((part, index) => (part, index)).Where(value => value.part.InteriorDecoration).ToArray();
            Assert.That(decorations.Length, Is.GreaterThan(0));
            Assert.That(decorations.All(value => value.part.PortalCellNumber.HasValue), Is.True);
            Assert.That(decorations.Any(value => appearance.Sources[value.index].AppearancePath.Contains("terminal")), Is.True);
            Assert.That(decorations.Any(value => appearance.Sources[value.index].AppearancePath.Contains("banner")), Is.True);
        }
        [TestCase("imp")]
        [TestCase("rebel")]
        public void InheritedHeadquartersAppearanceLoadsTheDerivedFactionLayout(string faction)
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            string layoutPath = "interiorlayout/faction_perk/hq/hq_s04_" + faction + ".ilf";
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "object/building/faction_perk/hq/shared_hq_s04_" + faction + ".iff");
            Assert.That(appearance.PortalLayout, Is.Not.Null);
            Assert.That(assets.Loader.Sources.ContainsKey(layoutPath), Is.True, "The derived layout must be read even when geometry comes from its parent.");
            var entries = IlfFile.Parse(assets.Loader.Read(layoutPath)).Entries;
            Assert.That(appearance.Parts.Any(part => part.InteriorDecoration), Is.True);
            Assert.That(entries.All(entry => assets.Loader.Sources.ContainsKey(entry.Template)), Is.True, "All faction furnishings must be traversed.");
            string otherFaction = faction == "imp" ? "rebel" : "imp";
            Assert.That(assets.Loader.Sources.ContainsKey("interiorlayout/faction_perk/hq/hq_s04_" + otherFaction + ".ilf"), Is.False);
        }

        [TestCase(true,5)]
        [TestCase(false,5)]
        [TestCase(true,4)]
        [TestCase(false,3)]
        [TestCase(false,1)]
        public void PortalDoorsPreserveHardpointsAndDeduplicateTheirTwoCellSides(bool hardpoint,int portalVersion)
        {
            var authored = Matrix4x4.TRS(new Vector3(4,5,6), Quaternion.Euler(0,90,0), Vector3.one);
            byte[] Cell(string name, uint target) => Form("CELL", Form("0005",
                Chunk("DATA", Bytes(writer => { writer.Write(1u); writer.Write((byte)0); writer.Write(Encoding.ASCII.GetBytes(name + "\0\0")); writer.Write((byte)0); })),
                Form("PRTL", Chunk(portalVersion.ToString("D4"), Bytes(writer =>
                {
                    if (portalVersion >= 5) writer.Write(false);
                    if (portalVersion >= 2) writer.Write(true);
                    writer.Write(0u); writer.Write(true); writer.Write(target);
                    if (portalVersion >= 3) writer.Write(Encoding.ASCII.GetBytes("test_style\0"));
                    if (portalVersion >= 4)
                    {
                        writer.Write(hardpoint);
                        for (int row=0; row<3; row++) for (int column=0; column<4; column++) writer.Write(authored[row,column]);
                    }
                })))));
            var bytes = Form("PRTO", Form("0004", Form("PRTS", Chunk("PRTL", Bytes(writer =>
            {
                writer.Write(4u);
                foreach (var vertex in new[]{new Vector3(-1,0,0),new Vector3(1,0,0),new Vector3(1,3,0),new Vector3(-1,3,0)})
                { writer.Write(vertex.x); writer.Write(vertex.y); writer.Write(vertex.z); }
            }))), Form("CELS", Cell("outside",1), Cell("inside",0))));
            var layout = PortalLayout.Parse(IffNode.Parse(bytes).Single(), _ => throw new InvalidOperationException());
            Assert.That(layout.Doors.Count, Is.EqualTo(portalVersion < 3 ? 0 : 1), "The two cell records describe one physical door; old portals have no door styles.");
            Assert.That(layout.Entrances.Count, Is.EqualTo(1));
            if (portalVersion < 3) return;
            var door = layout.Doors.Single();
            Assert.That(door.CellNumber, Is.Zero); Assert.That(door.TargetCellNumber, Is.EqualTo(1));
            Assert.That(Vector3.Distance(door.Transform.GetColumn(3),hardpoint ? new Vector3(4,5,6) : Vector3.zero), Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(door.Transform.MultiplyVector(Vector3.forward),hardpoint ? Vector3.right : Vector3.forward), Is.LessThan(.0001f));
        }

        [Test]
        public void NativeCantinaDoorsUseTheSharedStyleTable()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader,"object/building/corellia/shared_cantina_corellia.iff");
            Assert.That(appearance.Doors.Count, Is.EqualTo(5));
            Assert.That(appearance.Parts.Where((part,index) => appearance.Sources[index].ShaderPath == "shader/corl_metl_cantina_floor_cds16.sht").All(part => part.PrecalculatedVertexLighting), Is.True);
            Assert.That(appearance.Parts.Where((part,index) => appearance.Sources[index].ShaderPath == "shader/thm_frn_chair_s03_as9.sht").All(part => !part.PrecalculatedVertexLighting), Is.True);
            Assert.That(appearance.Doors.All(door => door.FirstPanel.Parts.Count > 0), Is.True);
            var sideways = appearance.Doors.First(door => door.Portal.Style == "door_corl_cantina_s01");
            var upward = appearance.Doors.First(door => door.Portal.Style == "door_corl_cantina_s02");
            Assert.That(sideways.Movement.x, Is.EqualTo(-2.1f).Within(.001f));
            Assert.That(upward.Movement, Is.EqualTo(Vector3.up * 3));
            Assert.That(sideways.OpenSeconds, Is.EqualTo(.2f));
            Assert.That(sideways.TriggerRadius, Is.EqualTo(3));
        }

        [TestCase(-2f,0f)]
        [TestCase(0f,3f)]
        public void DoorPanelsSlideInEitherAuthoredAxisAndReturnClosed(float x,float y)
        {
            var root = new GameObject("Native sliding door test");
            try
            {
                var first = new GameObject("First panel").transform; first.SetParent(root.transform,false);
                var second = new GameObject("Second panel").transform; second.SetParent(root.transform,false);
                var appearance = new DoorAppearance { Portal = new PortalLayout.Door { Passable = true }, Movement = new Vector3(x,y,0), OpenSeconds = .2f, CloseSeconds = .4f, Spring = 1 };
                var door = root.AddComponent<BuildingDoor>(); door.Initialize(appearance,first,second);
                door.Advance(.1f,true);
                Assert.That(Vector3.Distance(first.localPosition,appearance.Movement * .5f), Is.LessThan(.0001f));
                Assert.That(second.localPosition, Is.EqualTo(-first.localPosition));
                door.Advance(.1f,true);
                Assert.That(first.localPosition, Is.EqualTo(appearance.Movement));
                door.Advance(.2f,false);
                Assert.That(Vector3.Distance(first.localPosition,appearance.Movement * .5f), Is.LessThan(.0001f));
                door.Advance(.2f,false);
                Assert.That(first.localPosition, Is.EqualTo(Vector3.zero));
                appearance.Portal.Passable = false;
                door.Advance(1,true);
                Assert.That(first.localPosition, Is.EqualTo(Vector3.zero));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase("SWG/World", false)]
        [TestCase("SWG/World", true)]
        [TestCase("SWG/Character", false)]
        public void InteriorLightingPreservesLegacyAttenuationAndDoesNotRelightBakedGeometry(string shader, bool baked)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find(shader));
            var target = RenderTexture.GetTemporary(64,64,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);
            Mesh bakedMesh = null;
            var previous = RenderTexture.active;
            float environment = Shader.GetGlobalFloat("_SwgEnvironmentLighting");
            try
            {
                Shader.SetGlobalFloat("_SwgEnvironmentLighting",1);
                root.GetComponent<Renderer>().sharedMaterial = material;
                if (material.HasProperty("_AlphaToMask")) material.SetFloat("_AlphaToMask",0);
                if (baked)
                {
                    bakedMesh = UnityEngine.Object.Instantiate(root.GetComponent<MeshFilter>().sharedMesh);
                    bakedMesh.colors = Enumerable.Repeat(new Color(.4f,.3f,.2f),bakedMesh.vertexCount).ToArray();
                    root.GetComponent<MeshFilter>().sharedMesh = bakedMesh;
                    material.SetFloat("_UseVertexColor",1);
                    material.SetFloat("_PrecalculatedVertexLighting",1);
                    material.SetFloat("_VertexColorAmbientOnly",1);
                }
                var point = new PortalLayout.RoomLight { Type = PortalLayout.LightType.Point, Diffuse = new Color(.8f,.4f,.2f), Transform = Matrix4x4.Translate(new Vector3(0,0,-4)), Attenuation = Vector3.up };
                new InteriorLighting(new PortalLayout.Cell(), Matrix4x4.identity,new[]{point}).Apply(root);
                var properties = new MaterialPropertyBlock(); root.GetComponent<Renderer>().GetPropertyBlock(properties);
                using var commands = new UnityEngine.Rendering.CommandBuffer();
                commands.SetRenderTarget(target); commands.ClearRenderTarget(true,true,Color.black);
                commands.SetViewProjectionMatrices(Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.Translate(new Vector3(0,0,5)),GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(60,1,.1f,20),true));
                commands.SetGlobalVector("_WorldSpaceCameraPos",new Vector4(0,0,-5,1));
                commands.DrawMesh(root.GetComponent<MeshFilter>().sharedMesh,Matrix4x4.identity,material,0,0,properties);
                Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,64,64),0,0); pixels.Apply();
                var color = pixels.GetPixel(32,32);
                Assert.That(color.r, Is.EqualTo(Mathf.GammaToLinearSpace(baked ? .4f : .2f)).Within(.003f));
                Assert.That(color.g, Is.EqualTo(Mathf.GammaToLinearSpace(baked ? .3f : .1f)).Within(.002f));
                Assert.That(color.b, Is.EqualTo(Mathf.GammaToLinearSpace(baked ? .2f : .05f)).Within(.002f));
            }
            finally
            {
                Shader.SetGlobalFloat("_SwgEnvironmentLighting",environment);
                RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(pixels);
                if (bakedMesh != null) UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
        }

        [Test]
        public void ExteriorRendersBeyondTheDoorButNotInFrontOfIt()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("SWG/World"));
            var target = RenderTexture.GetTemporary(64, 64, 24);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            root.GetComponent<Renderer>().sharedMaterial = material;
            material.SetFloat("_Fullbright", 1);
            material.SetFloat("_AlphaToMask", 0);
            var portal = new[] { new Vector3(-1,-1,-2), new Vector3(1,-1,-2), new Vector3(1,1,-2), new Vector3(-1,1,-2) };
            using var visibility = new ExteriorPortalVisibility(root, new[] { portal }, Matrix4x4.identity);
            try
            {
                visibility.SetInside(true);
                var properties = new MaterialPropertyBlock();
                root.GetComponent<Renderer>().GetPropertyBlock(properties);
                using var commands = new UnityEngine.Rendering.CommandBuffer();
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, Color.black);
                var view = Matrix4x4.Scale(new Vector3(1,1,-1)) * Matrix4x4.Translate(new Vector3(0,0,5));
                commands.SetViewProjectionMatrices(view, GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(60,1,.1f,20), true));
                commands.SetGlobalVector("_WorldSpaceCameraPos", new Vector4(0,0,-5,1));
                // The apron beyond the door should draw; overlapping shell geometry in front should not.
                var mesh = root.GetComponent<MeshFilter>().sharedMesh;
                var portalMaterial = root.GetComponent<Renderer>().sharedMaterial;
                commands.DrawMesh(mesh, Matrix4x4.Scale(Vector3.one * 8), portalMaterial, 0, 0, properties);
                var frontProperties = new MaterialPropertyBlock();
                root.GetComponent<Renderer>().GetPropertyBlock(frontProperties);
                frontProperties.SetColor("_Tint", Color.green);
                commands.DrawMesh(mesh, Matrix4x4.TRS(new Vector3(0,0,-4), Quaternion.identity, Vector3.one * 8), portalMaterial, 0, 0, frontProperties);
                Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,64,64),0,0); pixels.Apply();
                var center = pixels.GetPixel(32,32);
                Assert.That(center.r, Is.GreaterThan(.5f), "The apron should remain visible through the doorway.");
                Assert.That(center.g, Is.GreaterThan(.5f));
                Assert.That(pixels.GetPixel(2,32).maxColorComponent, Is.LessThan(.1f), "Exterior outside the doorway must not overlap the interior.");
                // Rotate the camera through the doorway while the player remains indoors.
                visibility.UpdateCamera(new Vector3(0,0,1), new Vector3(0,0,-5));
                Assert.That(root.GetComponent<Collider>().enabled, Is.False, "Camera movement must not alter player collision.");
                Assert.That(root.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
                Assert.That(material.IsKeywordEnabled("SWG_EXTERIOR_PORTALS"), Is.False);
                // Ordinary outdoor materials have no portal buffer and must still render.
                commands.Clear();
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, Color.black);
                commands.SetViewProjectionMatrices(view, GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(60,1,.1f,20), true));
                commands.DrawMesh(mesh, Matrix4x4.Scale(Vector3.one * 8), material, 0, 0);
                Graphics.ExecuteCommandBuffer(commands);
                pixels.ReadPixels(new Rect(0,0,64,64),0,0); pixels.Apply();
                Assert.That(pixels.GetPixel(2,32).r, Is.GreaterThan(.5f), "Unclipped outdoor geometry needs no portal buffer.");
                visibility.UpdateCamera(new Vector3(.2f,0,-4), new Vector3(0,0,-5));
                Assert.That(root.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("SWG_EXTERIOR_PORTALS"), Is.True, "Rotating back inside restores portal clipping.");
                Assert.That(root.GetComponent<Collider>().enabled, Is.False);
                visibility.SetInside(false);
                Assert.That(root.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
                Assert.That(root.GetComponent<Collider>().enabled, Is.True);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PointLightFallsOffAcrossMeshesAndCellsWithoutASeam()
        {
            var left = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var right = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("SWG/World"));
            var target = RenderTexture.GetTemporary(128, 128, 24);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(128,128,TextureFormat.RGBA32,false);
            try
            {
                material.SetFloat("_AlphaToMask", 0);
                left.transform.position = new Vector3(-1,0,0); right.transform.position = new Vector3(1,0,0);
                left.transform.localScale = right.transform.localScale = Vector3.one * 2;
                left.GetComponent<Renderer>().sharedMaterial = right.GetComponent<Renderer>().sharedMaterial = material;
                var point = new PortalLayout.RoomLight { Type = PortalLayout.LightType.Point, Diffuse = Color.white, Transform = Matrix4x4.Translate(new Vector3(-1.2f,0,-1)), Attenuation = Vector3.up };
                var room = new PortalLayout.Cell(); room.Lights.Add(point);
                var hall = new PortalLayout.Cell();
                // The point is authored in one room, but both rooms receive its positional light.
                new InteriorLighting(room, Matrix4x4.identity, new[]{point}).Apply(left);
                new InteriorLighting(hall, Matrix4x4.identity, new[]{point}).Apply(right);
                var properties = new MaterialPropertyBlock();
                foreach (var root in new[]{left,right})
                {
                    root.GetComponent<Renderer>().GetPropertyBlock(properties);
                    Assert.That(properties.GetInt("_SwgInteriorLightCount"), Is.EqualTo(1));
                    Assert.That(properties.GetVectorArray("_SwgInteriorLightPositions")[0].w, Is.EqualTo(1), "Point lights retain their world positions.");
                }
                using var commands = new UnityEngine.Rendering.CommandBuffer();
                commands.SetRenderTarget(target); commands.ClearRenderTarget(true,true,Color.black);
                var view = Matrix4x4.Scale(new Vector3(1,1,-1)) * Matrix4x4.Translate(new Vector3(0,0,5));
                commands.SetViewProjectionMatrices(view,GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(60,1,.1f,20),true));
                commands.SetGlobalVector("_WorldSpaceCameraPos",new Vector4(0,0,-5,1));
                foreach (var root in new[]{left,right})
                {
                    root.GetComponent<Renderer>().GetPropertyBlock(properties);
                    commands.DrawMesh(root.GetComponent<MeshFilter>().sharedMesh,root.transform.localToWorldMatrix,material,0,0,properties);
                }
                Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,128,128),0,0); pixels.Apply();
                Assert.That(pixels.GetPixel(40,64).r, Is.GreaterThan(pixels.GetPixel(88,64).r + .15f), "The light must vary spatially across a large surface.");
                Assert.That(Mathf.Abs(pixels.GetPixel(63,64).r - pixels.GetPixel(64,64).r), Is.LessThan(.04f), "No jump at the room/mesh boundary.");
            }
            finally
            {
                RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(left); UnityEngine.Object.DestroyImmediate(right); UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void NativeDirectionalLightDoesNotIlluminateItsBackSideAndEmissionStaysVisible()
        {
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("SWG/World"));
            var backMesh = UnityEngine.Object.Instantiate(back.GetComponent<MeshFilter>().sharedMesh);
            backMesh.normals = backMesh.normals.Select(_ => Vector3.forward).ToArray();
            back.GetComponent<MeshFilter>().sharedMesh = backMesh;
            var target = RenderTexture.GetTemporary(128,128,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(128,128,TextureFormat.RGBAFloat,false,true);
            try
            {
                front.transform.position = Vector3.left; back.transform.position = Vector3.right;
                front.transform.localScale = back.transform.localScale = Vector3.one * 2;
                front.GetComponent<Renderer>().sharedMaterial = back.GetComponent<Renderer>().sharedMaterial = material;
                material.SetFloat("_AlphaToMask",0);
                var cell = new PortalLayout.Cell();
                cell.Lights.Add(new PortalLayout.RoomLight { Type = PortalLayout.LightType.Ambient, Diffuse = new Color(.1f,.1f,.1f) });
                cell.Lights.Add(new PortalLayout.RoomLight { Type = PortalLayout.LightType.Directional, Diffuse = new Color(.5f,.5f,.5f), Transform = Matrix4x4.identity });
                var lighting = new InteriorLighting(cell,Matrix4x4.identity,Array.Empty<PortalLayout.RoomLight>());
                lighting.Apply(front); lighting.Apply(back);
                using var commands = new UnityEngine.Rendering.CommandBuffer();
                commands.SetRenderTarget(target); commands.ClearRenderTarget(true,true,Color.black);
                commands.SetViewProjectionMatrices(Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.Translate(new Vector3(0,0,5)),GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(60,1,.1f,20),true));
                commands.SetGlobalVector("_WorldSpaceCameraPos",new Vector4(0,0,-5,1));
                var properties = new MaterialPropertyBlock();
                foreach(var root in new[]{front,back})
                {
                    root.GetComponent<Renderer>().GetPropertyBlock(properties);
                    commands.DrawMesh(root.GetComponent<MeshFilter>().sharedMesh,root.transform.localToWorldMatrix,material,0,0,properties);
                }
                Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,128,128),0,0); pixels.Apply();
                float ambient = Mathf.GammaToLinearSpace(.1f);
                float direct = Mathf.GammaToLinearSpace(.5f);
                Assert.That(pixels.GetPixel(40,64).r, Is.EqualTo(Mathf.GammaToLinearSpace(.6f)).Within(.01f), "Native -K direction and unit Lambert response; no doubled probe intensity.");
                Assert.That(pixels.GetPixel(88,64).r, Is.EqualTo(ambient).Within(.005f), "Ceilings/back-facing walls must not receive the direct light.");
                material.SetColor("_EmissionColor",new Color(.5f,.5f,.5f));
                Graphics.ExecuteCommandBuffer(commands);
                pixels.ReadPixels(new Rect(0,0,128,128),0,0); pixels.Apply();
                Assert.That(pixels.GetPixel(88,64).r, Is.EqualTo(ambient+direct).Within(.01f), "Screen emission survives a dark room.");
            }
            finally
            {
                RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(front); UnityEngine.Object.DestroyImmediate(back); UnityEngine.Object.DestroyImmediate(backMesh); UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void DownwardFixtureLightsTheFloorWithoutLightingTheCeiling()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("SWG/World"));
            var ceilingMesh = UnityEngine.Object.Instantiate(ceiling.GetComponent<MeshFilter>().sharedMesh);
            ceilingMesh.normals = ceilingMesh.normals.Select(_ => Vector3.forward).ToArray();
            ceiling.GetComponent<MeshFilter>().sharedMesh = ceilingMesh;
            var target = RenderTexture.GetTemporary(64,64,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);
            try
            {
                material.SetFloat("_AlphaToMask",0);
                floor.GetComponent<Renderer>().sharedMaterial = ceiling.GetComponent<Renderer>().sharedMaterial = material;
                ceiling.transform.position = new Vector3(0,0,-3);
                var building = Matrix4x4.Rotate(Quaternion.Euler(-90,0,0));
                var fixture = new PortalLayout.RoomLight { Type=PortalLayout.LightType.Point, Diffuse=Color.white, Attenuation=Vector3.up,
                    Transform=Matrix4x4.TRS(Vector3.up,Quaternion.FromToRotation(Vector3.forward,Vector3.up),Vector3.one) };
                var lighting = new InteriorLighting(new PortalLayout.Cell(),building,new[]{fixture});
                lighting.Apply(floor); lighting.Apply(ceiling);
                float Render(GameObject root)
                {
                    using var commands = new UnityEngine.Rendering.CommandBuffer();
                    commands.SetRenderTarget(target); commands.ClearRenderTarget(true,true,Color.black);
                    commands.SetViewProjectionMatrices(Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.Translate(new Vector3(0,0,5)),GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(60,1,.1f,20),true));
                    commands.SetGlobalVector("_WorldSpaceCameraPos",new Vector4(0,0,-5,1));
                    var properties = new MaterialPropertyBlock(); root.GetComponent<Renderer>().GetPropertyBlock(properties);
                    commands.DrawMesh(root.GetComponent<MeshFilter>().sharedMesh,root.transform.localToWorldMatrix,material,0,0,properties);
                    Graphics.ExecuteCommandBuffer(commands);
                    RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,64,64),0,0); pixels.Apply();
                    return pixels.GetPixel(32,32).r;
                }
                Assert.That(Render(floor),Is.GreaterThan(.8f));
                Assert.That(Render(ceiling),Is.LessThan(.001f),"The fixture should project down, not wash the ceiling green.");
            }
            finally
            {
                RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(floor); UnityEngine.Object.DestroyImmediate(ceiling); UnityEngine.Object.DestroyImmediate(ceilingMesh); UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void MaterialEmissionDoesNotReadTheFollowingSpecularColor()
        {
            var material = Bytes(writer =>
            {
                foreach(float value in new[]{.1f,.2f,.3f,.8f})
                {
                    writer.Write(1f); writer.Write(value); writer.Write(value); writer.Write(value);
                }
                writer.Write(20f);
            });
            var bytes = Form("SSHT",Form("0000",Form("MATS",Form("0000",Chunk("TAG ",BitConverter.GetBytes(0x4d41494e)),Chunk("MATL",material)))));
            Assert.That(ShtFile.TryGetEmission(IffNode.Parse(bytes),out var emission),Is.True);
            Assert.That(emission.r,Is.EqualTo(.3f)); Assert.That(emission.g,Is.EqualTo(.3f)); Assert.That(emission.b,Is.EqualTo(.3f));
        }

        [Test]
        public void NativeDisplayMaterialKeepsItsEmissionSeparateFromRoomLighting()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath,"../client.ini")));
            Assert.That(ShtFile.TryGetEmission(IffNode.Parse(assets.Loader.Read("shader/diagnostic_screen_screen.sht")),out var screen),Is.True);
            Assert.That(screen.maxColorComponent,Is.Zero,"Its gray specular material must not become emission.");
            Assert.That(ShtFile.TryGetEmission(IffNode.Parse(assets.Loader.Read("shader/intr_tech_floor_c_cs8.sht")),out var floor),Is.True);
            Assert.That(floor.maxColorComponent,Is.Zero);
        }

        [Test]
        public void RoomLightingDoesNotMutateSharedMaterialsAndResetPreservesOtherOverrides()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("SWG/World"));
            try
            {
                var renderer = root.GetComponent<Renderer>(); renderer.sharedMaterial = material;
                var properties = new MaterialPropertyBlock(); properties.SetColor("_Tint", Color.cyan); renderer.SetPropertyBlock(properties);
                var cell = new PortalLayout.Cell();
                cell.Lights.Add(new PortalLayout.RoomLight { Type = PortalLayout.LightType.Ambient, Diffuse = Color.white * .5f, Transform = Matrix4x4.identity });
                new InteriorLighting(cell, Matrix4x4.identity, Array.Empty<PortalLayout.RoomLight>()).Apply(root);
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_SwgInteriorLighting"), Is.EqualTo(1));
                Assert.That(properties.GetInt("_SwgInteriorLightCount"), Is.Zero);
                Assert.That(properties.GetColor("_SwgInteriorAmbient").r, Is.EqualTo(.5f).Within(.0001f));
                Assert.That(material.GetFloat("_SwgInteriorLighting"), Is.Zero);
                InteriorLighting.Reset(root); renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_SwgInteriorLighting"), Is.Zero);
                Assert.That(properties.GetColor("_Tint"), Is.EqualTo(Color.cyan));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
