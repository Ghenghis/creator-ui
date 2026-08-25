# Barro's Pizza Creator — Integration Guide

This document describes the verified end-to-end integration between creator-ui (Unity front-end), the Barros backend (Python sidecar), LMStudio (local LLM), and ComfyUI (real diffusion textures).

## Pipeline (verified live 2026-08-25)

```
[1] user types prompt in chat panel
    ↓
[2] creator-ui Chat panel (ChefVoicePanel.cs / CrewPanel.cs / LabPanel.cs / DesignerPanel.cs)
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
[8] tools/render-texture-comfyui.mjs (default) OR tools/render-texture.mjs (--texture=pil)
    - ComfyUI path: SD-Turbo diffusion via /prompt (~25s per image on CPU)
      - Builds prompt from recipe ingredients + name
      - KSampler 4 steps, CFG 1.5, euler/karras, 320x320 PNG
      - Downloads via /view, embeds base64 in pizza.Texture
    - PIL path: realistic placeholder (crust ring, sauce base, ingredient blobs)
    ↓
[9] Slice 1 .NET verifier (in integration-test.mjs)
    Confirms JSON deserializes into PC3's actual PizzaModel class
    Validates TextureIntegrity (PNG signature + 256x256)
```

## Components

### creator-ui (Unity 6)
- `Assets/Scripts/Recipe/RecipeComposer.cs` — entry point, prefers Barros
- `Assets/Scripts/LLM/BarrosBackend.cs` — POSTs to Barros /compose with full catalog
- `Assets/Scripts/LLM/LMStudioBackend.cs` — direct LMStudio fallback (model: `qwen3.8-9b-uncensored-cyber-exploit-xrpl-v3`)
- `Assets/Scripts/LLM/OpenAIBackend.cs` — OpenAI last-resort fallback
- `Assets/Scripts/Recipe/BarrosRecipeModels.cs` — schema for Barros responses + adapter
- `Assets/Scripts/Recipe/HistoryStore.cs` — per-mode chat history persistence
- `Assets/Scripts/Chat/CreatorUIBootstrap.cs` — runtime wiring (loads UXML/USS, builds UI tree, attaches controllers)
- `Assets/Scripts/Chat/CreatorUIResourcesLoader.cs` — Resources.Load wrapper
- `Assets/Scripts/Chat/GalleryView.cs` — in-Unity recipe gallery with PNG thumbnails
- `Assets/Scripts/Chat/ChatHistory.cs` — history + Barros orchestration wrapper
- `bin/barro.mjs` — Node CLI wrapper

### Barros sidecar (Python, separate repo: Ghenghis/Barros-Pizza-Creator)
- `backend/main.py` — entry point (port 48173)
- `backend/barros_ai/server.py` — HTTP server
- `backend/barros_ai/orchestrator.py` — PizzaOrchestrator.compose()
- `backend/barros_ai/solver.py` — recipe repair + scoring
- `backend/barros_ai/providers.py` — LMStudio + OpenAI clients
- Endpoints: `GET /health`, `POST /compose`, `POST /chat`, `POST /lab`

### LMStudio (local)
- HTTP server at `http://127.0.0.1:1234`
- Model: `qwen3.8-9b-uncensored-cyber-exploit-xrpl-v3` (verified loaded)
- 132 models available; cold-load can take 30-60s. Use 180s timeout.

### ComfyUI (local, real diffusion)
- HTTP server at `http://127.0.0.1:8188`
- Checkpoint: `sd-turbo.safetensors` (5.2GB) at `S:/ComfyUI/models/checkpoints/`
- ~25s per 320x320 PNG on CPU
- Falls back to PIL placeholder if unreachable

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

### 3. Verify ComfyUI (optional, for real textures)
```bash
curl -s http://127.0.0.1:8188/system_stats | python -m json.tool | head -5
```
Expected: `comfyui_version` and at least 1 device.

### 4. Run integration test (E2E pipeline)
```bash
cd /s/Unity_Games/PC3\ -\ Pizza\ Creator/creator-ui
node tools/integration-test.mjs                  # ComfyUI default (~25s/recipe)
node tools/integration-test.mjs --texture=pil    # PIL placeholder (fast)
node tools/integration-test.mjs --skip-texture   # no texture
```
Expected: 3/3 recipes generated, all deserialize-OK in Slice 1 verifier.

### 5. Use barro CLI
```bash
node bin/barro.mjs doctor                # health check
node bin/barro.mjs compose "Make a margherita" --name Test  # single recipe
node bin/barro.mjs lab --tags spicy,budget --count 3          # batch
node bin/barro.mjs verify output/margherita-*.json            # validate
node bin/barro.mjs previews --all                            # render textures
```

### 6. Open in Unity Editor (manual testing)
- Open `Assets/Scenes/CreatorUI.unity` in Unity 6000.0.51f1
- Press Play
- Bootstrap loads UXML/USS, builds sidebar + 4 chat panels, wires LLM stack
- Click a chat mode icon → switch panel
- Type a prompt → "Apply recipe" → enter name → save to `output/{name}.final.json`

## Verified live recipes

| Theme | Recipe | Ingredients | Taste | Profit | Texture |
|-------|--------|-------------|-------|--------|---------|
| Margherita | Margherita Pizza (Round) | Mozzarella, Tomato | 77 | 45.9% | SD-Turbo 204KB PNG |
| Diavola | Fiery Diavola Star (Star) | Bacon, Tomato | 77 | 38.3% | SD-Turbo PNG |
| Hawaiian | Hawaiian Bacon (Round) | Mozzarella, Bacon, Tomato | 82.5 | 42.9% | SD-Turbo PNG |
| Vegan | Vegan Mushroom (Round) | Mushroom, Pepper | 77 | 37.5% | SD-Turbo PNG |
| Buffalo | Buffalo Ranch Chicken (Round) | Chicken, Tomato | 77 | 42.9% | SD-Turbo 205KB PNG |
| Pesto | Pesto Chicken (Round) | Mozzarella, Chicken, Tomato | 82.5 | 41.9% | SD-Turbo 205KB PNG |

All recipes deserialize into PC3's `PizzaModel` schema and pass all 4 verifier checks.

## Slice 1 verifier notes

- `JsonDeserializes` — passes (recipe loads as PC3 PizzaModel)
- `AmountsInRange` — passes (ingredient grams within catalog bounds)
- `TextureIntegrity` — passes (PNG signature OK, 320x320 SD-Turbo output or 256x256 PIL fallback)
- `texture_check` — structural-only; full Unity decode requires running the game

## Scope lock (PC3 only)

```bash
grep -ril "amount_oz\|FastFood\|tycoon" Assets/ tests/
```
Expected: no output. CI fails PR if contamination detected.
