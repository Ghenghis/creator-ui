# Barro's Pizza Creator Chat UI

In-game chat UI for Barro's Pizza (formerly PC3 Pizza Creator). 4 chat modes + Name dialog + sidebar tab nav. Built in Unity UI Toolkit. Truth spec: `docs/mockups/`. Design: `docs/superpowers/specs/2026-08-25-barros-creator-chat-ui-design.md`. Plan: `docs/superpowers/plans/2026-08-25-barros-creator-chat-ui.md`.

## Architecture

3 layers, file-based handoff:

- **Presentation** — Unity UI Toolkit (UXML/USS) for ChefVoice, Crew, Lab, Designer, NameDialog + SidebarTabs. C# MonoBehaviour controllers in `Assets/Scripts/Chat/`.
- **Orchestration** — C# in `Assets/Scripts/`:
  - `LLM/` — `BarrosBackend` (default :48173), `LMStudioBackend` (:1234 fallback), `OpenAIBackend` (further fallback), `LLMClient` (orchestrates retry + fallback)
  - `Recipe/` — `RecipeComposer` (LLM + validate + score), `ScoringEngine` (taste/cost/profit/novelty, PC3 formula), `JsonExporter` (writes PC3 PizzaModel DataContract)
  - `Sidebar/TabNavigator.cs`
  - `Editor/SnapshotRunner.cs` — invoked by `-executeMethod` for batch screenshot
- **Persistence** — `output/{name}.recipe.json` + `output/{name}.final.json` (PC3 DataContract shape, ready for M0-M1 conversion harness)

## Backend integration

`LLMClient` uses a 3-tier fallback:

1. **Barros sidecar** at `http://127.0.0.1:48173` (Ghenghis/Barros-Pizza-Creator `backend/`) — handles orchestration, provider routing, retries, history. REST endpoints: `/compose`, `/chat`, `/lab`, `/health`.
2. **LMStudio** at `http://127.0.0.1:1234/v1/chat/completions` — direct local model fallback.
3. **OpenAI** (`OPENAI_API_KEY`) — cloud fallback.

Configurable via `Assets/StreamingAssets/settings.json`.

## Quick start

1. Install Unity 6 (6000.0.51f1, Personal license compatible).
2. Open `Assets/Scenes/CreatorUI.unity`.
3. Start the Barros sidecar: `cd ../Barros-Pizza-Creator/backend && python main.py` (port 48173).
4. Press Play in Unity Editor.

## Tests

```bash
# EditMode (LLMClient, ScoringEngine, JsonExporter, RecipeComposer — 19 tests)
"C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Unity.exe" \
  -batchmode -nographics -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults TestResults-EditMode.xml -logFile unity-edit.log -quit

# PlayMode
... -runTests -testPlatform PlayMode ...

# Snapshots (pixelmatch >=98% per panel)
node tools/snapshot-runner.mjs
```

## Scope lock

PC3 / Barro's Pizza only. PC2 (Fast Food Tycoon 2) is PROHIBITED. Do not import PC2 paths, fields, or models.

```bash
grep -ril "amount_oz\|FastFood\|tycoon" Assets/ tests/
```
Expected: no output. CI fails PR if contamination detected.

## Spec + Plan

- Design: `docs/superpowers/specs/2026-08-25-barros-creator-chat-ui-design.md`
- Plan: `docs/superpowers/plans/2026-08-25-barros-creator-chat-ui.md`
- Truth proof: `docs/evidence.md`
- Mockup truth spec: `docs/mockups/README.md`

## GitHub

`https://github.com/Ghenghis/creator-ui` — main branch, 19+ commits.
