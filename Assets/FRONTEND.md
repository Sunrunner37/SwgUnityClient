Press Play (or **SWG > Play Client**) for normal startup, regardless of the scene currently open in the editor. The entry is `Assets/Scenes/Intro.unity`; `[Startup] SkipIntro=true` routes directly to Login without preparing the intro. With `false`, the intro plays first. There is no separate Frontend scene.

Use **SWG > Preview Current Screen** to run the scene you are editing directly. Previewing Intro explicitly plays it even when SkipIntro is enabled. Previewing CharacterCreation opens its labeled offline preview, without authenticating or contacting the game server.

Unity scenes live directly under `Assets/Scenes`. Screen controllers and scene-specific helpers live under `Assets/SwgClient/Runtime/UI/Screens/<name>`, with shared screen code in the parent folder. Each screen uses its own complete screen prefab:

* `Intro.unity` uses `IntroScreen.prefab` and `IntroScreen`: intro timing, artwork, crawl, planet, intro camera, and Escape/skip.
* `Login.unity` uses `LoginScreen.prefab` and `LoginScreen`: fields, form validation, login handshake, progress/errors, cancellation, and navigation after admission.
* `CharacterList.unity` uses `CharacterListScreen.prefab` and `CharacterListScreen`: the authenticated account's returned characters, selection, logout, and the explicit Create New action. `CharacterListRow.prefab` is the editable row layout.
* `CharacterCreation.unity` uses `CharacterCreationScreen.prefab` and `CreationScreen`, with `CharacterPreview` owning mesh rendering and camera framing: species, gender, descriptions, and Back navigation.

Open any of these scenes and use **Preview Current Screen**. CharacterCreation without an admitted session is explicitly labeled **Offline character preview**; it does not create or fabricate a session. Next remains disabled because character submission is not implemented. Every successful login opens CharacterList, even when the list is empty. Wrong passwords remain on Login and restore the controls. Create New is an explicit action and is disabled when the server denies creation permission. Returning from creation preserves the session and returns to the list; Back from the list logs out. Login permission and creation permission are separate.

Scenes load one screen at a time using real scene transitions. Each screen prefab owns its Canvas, UI camera, event system, controls, and screen-specific cameras/objects. No loading overlay is displayed during startup or navigation: the real screen appears as its archive-backed artwork becomes available. Login input does not wait for title music decoding, which runs in the background. The inactive Load error panel is shown only if initialization fails. The nested Intro, Login, and CharacterCreation visual prefabs remain editable for layout changes. They have no dependency on one another or a master controller. `ClientFrontend.prefab` and `SwgClientApp` have been removed.

Only shared service state survives transitions: configuration, archive/texture cache (`TreRuntimeAssets`), audio (`ClientMusic`), and the admitted connection (`ClientSession`). `ClientRuntime` provides their lifetime and has no UI/screen references. The login screen owns a pending connection, transfers it to the session upon admission, and Back from the character list disconnects it. Passwords never enter the shared session. Title music continues across Login/CharacterList/Creation transitions.

The Scene view frames the current screen or prefab stage on its first draw. Manual panning and zooming are retained after that. Use **SWG > Frame Frontend in Scene View** to frame it again. To run a screen directly, open its scene and choose Preview Current Screen; Prefab Mode is for editing its prefab.

SWG visual asset definitions
---------------------------

Use **Assets > Create > SWG** to create one of these ScriptableObject assets:

* **Texture** (`TreTexture`): enter a virtual DDS path such as `texture/intro_logo.dds`. Optional crop uses top-left pixel coordinates. Tint multiplies the original channels.
* **Panel** (`TrePanel`): enter atlas and UI-style markup paths, output size, and an ordered list of style layers. Each layer has a SWG style name, state, destination rectangle, and tint. Styles are composed using their original nine-slice source rectangles. Layer tint alpha controls opacity.
* **Button** (`TreButton`): enter atlas/style paths, frame style/state, output size, and optional atlas patches. Source Start/End are pixel endpoints; reversing them mirrors the source. Patch destinations use top-left pixel rectangles.

Select an asset to edit its fields and see an in-memory preview. **Reload from SWG archives** rebuilds its preview and refreshes scene bindings. Paths resolve using `[Assets]` in `client.ini`, with the same TRE precedence as the runtime loader.

Add a **Tre Texture Binding** component to a RawImage or Renderer and drag any SWG visual asset into its Asset field. The component only assigns the resulting texture; the definition owns all loading/composition settings. There is no artwork enum, reflection lookup, or duplicated role list. Renderer targets can choose the material texture property (default `_BaseMap`). Keep layout and additional UI tint on the GameObject. Disable the binding to use a normal Unity texture.

The converted frontend definitions live in `Assets/SwgAssets`: Background, LoginFrame, CreationPanel, Back, Next, Long, Logo, Crawl, and Planet. Their paths, crops, sizes, style names, and composition layers are editable data rather than hardcoded loader presets. The saved visual asset definitions are the only source for these settings; no code-generated frontend or legacy artwork preset list remains.

**SWG > TRE Asset Browser** searches the archive catalog and previews DDS files. **Assign DDS to selected RawImage** creates a Texture definition in `Assets/SwgAssets` and assigns it to the selected object's binding. Other file types have text/header previews and copyable virtual paths. **SWG > Refresh TRE Previews** refreshes scene/prefab previews. Original pixels remain in the external archives; only paths and composition metadata are saved in Unity.

`Assets/Generated` contains Unity render targets, materials, a procedural star field, and the UI font. Character meshes and music still load at runtime. **SWG > Build Windows Client** builds the enabled entry scene to `Build/SwgEmuClient.exe`.

**SWG > Validate Original Assets** loads every saved SWG visual asset definition from its configured archive paths, alongside character models and music. The standalone core checks validate binary formats and networking independently of Unity asset serialization.

Validation
----------

`Tests/Editor/VerifyNavigation.cs` is an editor-only integration check executed with Unity CLI `script run --file Tests/Editor/VerifyNavigation.cs --entry VerifyNavigation.Run` while Login is running and ready. It uses a loopback-only admission fixture to verify form validation, populated and empty character lists, scene isolation, creation permissions, session ownership, species/gender controls, music continuity, and logout cleanup. The separate core checks exercise the full mock-server wire handshake. No fixture API is shipped in the runtime controllers.


## Editing the intro

Use **SWG > Edit Intro Timeline** to open the Intro scene with its `IntroScreen` director selected in Unity's Timeline window. The sequence is `Assets/Animations/Intro/Intro.playable`; drag clip boundaries to change timing, and edit the `.anim` clips there to adjust movement and fades. Timeline preview supports scrubbing without entering Play mode. Music continues to load from the original TRE path and starts with the sequence in Play mode.

The `Crawl motion` object owns scrolling and opacity. Its child `Original opening crawl` owns the static plane, tilt, scale, and original TRE artwork. Move/scale/rotate the child in the Inspector to frame the text without rewriting mesh vertices. `RendererOpacity` makes the shader fade animatable while preserving the texture property block populated by the TRE loader. Opening text, logo, star field, crawl, and planet visibility are Timeline tracks. `IntroScreen.cs` only handles startup/music, Escape, and navigation when the sequence finishes.

`Tests/Editor/VerifyIntro.cs` checks entry below the viewport, texture preservation while scrubbing, immutable mesh geometry, fading, and the final Login transition.
