# Barro's Pizza Creator — Integration Guide

This document describes the verified end-to-end integration between creator-ui (Unity front-end) and the Barros backend (Python sidecar) plus LMStudio (local LLM).

## Pipeline (verified live 2026-08-25)

```
[1] user types prompt in chat panel
    ↓
[2] creator-ui Chat panel (e.g. ChefVoicePanel.cs)
    ↓
[3] RecipeComposer.ComposeAsync(system, user, heat)
    ↓
[4] BarrosBackend.ComposeWithCatalogAsync(user, catalog, heat)
    POST http://127.0.0.1:48173/compose
    body: { prompt, heat, count:1, catalog:[87 ingredients] }
    ↓
[5] Barros sidecar (Python)
    - Reads catalog
    - Calls LLM with prompt + catalog summary
    - Runs solver: ingredient IDs validated, amounts clamped, positions assigned
    - Computes scores: taste, cost, profit, popularity, novelty, originality
    - Returns: BarrosComposeResponse { recipes: [BarrosRecipeData] }
    ↓
[6] BarrosRecipeAdapter.ToRecipeData(barros_recipe)
    Maps Barros fields to creator-ui RecipeData
    ↓
[7] JsonExporter.WriteFinal(recipe, output/{name}.final.json)
    Writes PC3 PizzaModel DataContract JSON
    ↓
[8] Slice 1 .NET verifier (optional, in tools/integration-test.mjs)
    Confirms JSON deserializes into PC3's actual PizzaModel class
    Texture check expected to fail (Slice 3 renders textures)
```

## Components

### creator-ui (Unity 6)
- `Assets/Scripts/Recipe/RecipeComposer.cs` — entry point, prefers Barros
- `Assets/Scripts/LLM/BarrosBackend.cs` — POSTs to Barros /compose with full catalog
- `Assets/Scripts/LLM/LMStudioBackend.cs` — direct LMStudio fallback (model: `qwen3.8-9b-uncensored-cyber-exploit-xrpl-v3`)
- `Assets/Scripts/LLM/OpenAIBackend.cs` — OpenAI last-resort fallback
- `Assets/Scripts/Recipe/BarrosRecipeModels.cs` — schema for Barros responses + adapter
- `Assets/Scripts/Chat/CreatorUIBootstrap.cs` — runtime wiring (loads UXML/USS, builds UI tree, attaches controllers)
- `Assets/Scripts/Chat/CreatorUIResourcesLoader.cs` — Resources.Load wrapper

### Barros sidecar (Python, separate repo)
- `Ghenghis/Barros-Pizza-Creator/backend/main.py` — entry point
- `backend/barros_ai/server.py` — HTTP server (port 48173)
- `backend/barros_ai/orchestrator.py` — PizzaOrchestrator.compose()
- `backend/barros_ai/solver.py` — recipe repair + scoring
- `backend/barros_ai/providers.py` — LMStudio + OpenAI clients
- Endpoints: `GET /health`, `POST /compose`, `POST /chat`, `POST /lab`

### LMStudio (local)
- HTTP server at `http://127.0.0.1:1234`
- Model: `qwen3.8-9b-uncensored-cyber-exploit-xrpl-v3` (verified loaded)
- Cold-load can take 30–60s; warm cache ~5s. Use 180s timeout.

## Running

### 1. Start Barros sidecar
```bash
cd /s/PC3_Barros_Backend_Runtime/backend
python main.py --host 127.0.0.1 --port 48173
```
Expected output: server listening on :48173.

### 2. Verify LMStudio
```bash
curl -s http://127.0.0.1:1234/v1/models | python -m json.tool | head -10
```
Expected: `qwen3.8-9b-uncensored-cyber-exploit-xrpl-v3` in the model list.

### 3. Run integration test (E2E pipeline)
```bash
cd /s/Unity_Games/PC3\ -\ Pizza\ Creator/creator-ui
node tools/integration-test.mjs
```
Expected: 3/3 recipes generated, all deserialize-OK in Slice 1 verifier.

### 4. Open in Unity Editor (manual testing)
- Open `Assets/Scenes/CreatorUI.unity` in Unity 6000.0.51f1
- Press Play
- Bootstrap loads UXML/USS, builds sidebar + 4 chat panels, wires LLM stack
- Click a chat mode icon → switch panel
- Type a prompt → "Apply recipe" → enter name → save to `output/{name}.final.json`

## Verified live (3 sample recipes)

| Theme | Recipe | Ingredients | Taste | Profit |
|-------|--------|-------------|-------|--------|
| Margherita | Margherita Pizza (Round) | Mozzarella, Tomato | 77 | 42.9% |
| Diavola | Spicy Diavola Hot Salami (Round) | Mozzarella, Tomato | 77 | 37.5% |
| Hawaiian | Hawaiian Bacon (Round) | Mozzarella, Bacon, Tomato | 82.5 | 42.9% |

All deserialized successfully into PC3's `PizzaModel` schema (verifier `JsonDeserializes` check passed).

## Slice 1 verifier notes

- `JsonDeserializes` — passes (recipe loads as PC3 PizzaModel)
- `TextureIntegrity` — expected to fail (textures are empty; Slice 3's job)
- `AmountsInRange` — skipped unless `--recipe` flag passed

## Scope lock (PC3 only)

```bash
grep -ril "amount_oz\|FastFood\|tycoon" Assets/ tests/
```
Expected: no output. CI fails PR if contamination detected.
