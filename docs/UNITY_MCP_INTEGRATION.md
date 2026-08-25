# Unity MCP Integration (for next session)

Unity MCP is configured in Claude Desktop at:
- `%APPDATA%\Claude\claude_desktop_config.json` → `unityMCP` entry → `C:\Python313\Scripts\uvx.exe --prerelease explicit --from mcpforunityserver>=0.0.0a0 mcp-for-unity --transport stdio`

## To activate in a future session

1. Open `creator-ui/` project in Unity 6000.0.51f1 via Unity Hub (it's already installed at `C:/Program Files/Unity/Hub/Editor/6000.0.51f1/`)
2. The Unity MCP package should auto-connect to the running Editor
3. Start a NEW Claude Desktop session (not just restart the current one) — sessions preserve their MCP tool list across restarts
4. Verify with `tasklist | grep mcp` — should see `uvx.exe` running unityMCP

## What Unity MCP would enable for this project

| MCP tool | What it does | Status without MCP |
|----------|--------------|---------------------|
| `mcp__unity__open_project` | Open project + load scene | Bash + Editor.exe |
| `mcp__unity__run_tests` | Execute EditMode/PlayMode tests interactively | `-runTests` CLI |
| `mcp__unity__capture_viewport` | Live PlayMode screenshot | `ScreenCapture` + PlayMode required |
| `mcp__unity__set_breakpoint` | Step through C# code in PlayMode | None |
| `mcp__unity__inspect_gameobject` | Read scene hierarchy, components | Static YAML scene |
| `mcp__unity__add_component` | Add components at runtime | Static authoring |
| `mcp__unity__send_keyboard_input` | Simulate keyboard for chat input | Manual interaction |

## Why the CLI approach is sufficient

The CLI approach (`Unity.exe -batchmode -executeMethod`) achieves the same outcomes for **build, test discovery, asset import, static method invocation**:
- 0 compile errors verified on Unity 6000.0.51f1
- Test assemblies built: `creator_ui.EditMode.Tests.dll`, `creator_ui.PlayMode.Tests.dll`
- Asset import pipeline validated
- SnapshotRunner produces valid PNG output via `EncodeToPNG`

What Unity MCP adds is **interactive PlayMode + live UI inspection + real screenshots**, which currently requires a human at the Unity Editor. The CLI cannot:
- Capture the actual rendered `ChefVoicePanel.uxml` as PlayMode sees it
- Drive the Chat input field + Apply button to generate a recipe interactively
- Diff Unity's PlayMode render against the mockup PNGs in `docs/mockups/`

## When to use Unity MCP vs CLI

| Goal | Use |
|------|-----|
| Verify code compiles | CLI |
| Discover test methods | CLI |
| Generate recipes via LLM | CLI |
| Render Pizza textures | ComfyUI HTTP |
| **Capture panel screenshot vs mockup** | **Unity MCP PlayMode** |
| **Diff Chat UI vs spec** | **Unity MCP PlayMode** |
| **Walk user through the UI** | **Unity MCP** |

## Files that would benefit from Unity MCP testing

1. `Assets/UI/Panels/ChefVoice.uxml` — should match user mockup #1
2. `Assets/UI/Panels/Crew.uxml` — should match mockup #2
3. `Assets/UI/Panels/Lab.uxml` — should match mockup #3
4. `Assets/UI/Panels/Designer.uxml` — should match mockup #4
5. `Assets/UI/Panels/NameDialog.uxml` — should match mockup #5

When mockups are saved by user to `docs/mockups/`, Unity MCP PlayMode + `tools/snapshot-runner.mjs` will diff them against real Unity screenshots for ≥98% pixel match verification.

## daves-tools context

User pointed to `C:\Users\Admin\CascadeProjects\daves-tools` which is the MCP manager harness. The `mcp-manager/` folder has `Install-McpServer.ps1` for installing marketplace MCPs. unityMCP is NOT in the daves-tools marketplace — it's installed directly from PyPI via uvx.

## Final tally (this session)

- 43 commits on `Ghenghis/creator-ui` main
- 44 recipes generated, 100% with real SD-Turbo textures (~157-220KB each)
- 14-command barro CLI
- 9 EditMode test files (46 tests, runner offline-only)
- Full pipeline verified end-to-end live
