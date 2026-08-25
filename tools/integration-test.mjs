#!/usr/bin/env node
// End-to-end integration test: creator-ui catalog -> Barros sidecar -> LMStudio -> PC3 recipe -> texture -> Slice 1 verifier
// Usage: node tools/integration-test.mjs [--skip-verifier] [--skip-texture]
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'fs';
import { join } from 'path';
import { execSync } from 'child_process';

const projectRoot = process.env.CREATOR_UI_ROOT || 'S:/Unity_Games/PC3 - Pizza Creator/creator-ui';
const catalogPath = join(projectRoot, 'Assets/StreamingAssets/catalog.json');
const barrosBase = process.env.BARROS_URL || 'http://127.0.0.1:48173';
const lmstudioBase = 'http://127.0.0.1:1234';
const outDir = process.env.OUT_DIR || join(projectRoot, 'output');
const verifierDir = process.env.VERIFIER_DIR || 'S:/Unity_Games/PC3 - Pizza Creator/_pizza-agent';
const gameDir = process.env.GAME_DIR || 'S:/Unity_Games/PC3 - Pizza Creator/_decompiled/Assembly-CSharp';
const skipVerifier = process.argv.includes('--skip-verifier');
const skipTexture = process.argv.includes('--skip-texture');

mkdirSync(outDir, { recursive: true });

const catalog = JSON.parse(readFileSync(catalogPath, 'utf-8'));
const barrosCatalog = catalog.ingredients.map(ing => ({
  id: ing.id,
  name: ing.name || ing.id,
  type_id: ing.type || 'Unknown',
  sizes: ['Large', 'Medium', 'Small'].map((sz, i) => ({
    size: sz,
    grams: sz === 'Large' ? ing.max_g : sz === 'Small' ? ing.min_g / 2 : (ing.min_g + ing.max_g) / 2,
    cost: ((sz === 'Large' ? ing.max_g : sz === 'Small' ? ing.min_g / 2 : (ing.min_g + ing.max_g) / 2) / 100) * (ing.base_price || 0)
  }))
}));

async function checkBackend(url, name) {
  try {
    const r = await fetch(url);
    return r.ok ? `${name} OK (${r.status})` : `${name} HTTP ${r.status}`;
  } catch (e) {
    return `${name} UNREACHABLE: ${e.message}`;
  }
}

async function composeRecipe(prompt, heat = 'Medium') {
  const payload = { prompt, catalog: barrosCatalog, count: 1, heat };
  const r = await fetch(`${barrosBase}/compose`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!r.ok) throw new Error(`Barros HTTP ${r.status}: ${await r.text()}`);
  return await r.json();
}

function renderTexture(pizzaPath) {
  try {
    execSync(`node "${join(projectRoot, 'tools/render-texture.mjs')}" "${pizzaPath}"`, { stdio: 'pipe', timeout: 30000 });
    return true;
  } catch (e) {
    return false;
  }
}

function runVerifier(pizzaPath) {
  const reportPath = `C:/Users/Admin/AppData/Local/Temp/vr-${Date.now()}.json`;
  try {
    const out = execSync(
      `cd "${verifierDir}" && dotnet run --project verifier/PizzaAgent.Verify.csproj -- --pizza "${pizzaPath}" --game-dir "${gameDir}" --out "${reportPath}"`,
      { stdio: 'pipe', timeout: 60000 }
    ).toString();
    const report = JSON.parse(readFileSync(reportPath, 'utf-8'));
    return { ok: report.passed, report };
  } catch (e) {
    return { ok: false, report: null, error: e.message };
  }
}

const tests = [];
const log = (msg) => { console.log(msg); tests.push(msg); };

log(`=== Integration test @ ${new Date().toISOString()} ===`);
log(`creator-ui: ${projectRoot}`);
log(`Barros: ${await checkBackend(barrosBase + '/health', 'Barros sidecar')}`);
log(`LMStudio: ${await checkBackend(lmstudioBase + '/v1/models', 'LMStudio')}`);

let allPassed = true;
try {
  const customPrompt = process.env.INTEGRATION_PROMPT;
  const customName = process.env.INTEGRATION_NAME;
  const themes = customPrompt
    ? [{ prompt: customPrompt, name: customName || customPrompt.split(' ').slice(0, 2).join('') }]
    : [
        { prompt: 'Make a margherita pizza. Tomato sauce, mozzarella, fresh basil.', name: 'Margherita' },
        { prompt: 'Spicy diavola with hot salami and chili oil.', name: 'Diavola' },
        { prompt: 'Hawaiian with pineapple and ham.', name: 'Hawaiian' }
      ];

  for (const t of themes) {
    log(`\n--- ${t.name} ---`);
    const t0 = Date.now();
    const resp = await composeRecipe(t.prompt);
    const dt = ((Date.now() - t0) / 1000).toFixed(1);
    if (!resp.ok) {
      log(`FAIL: ${t.name} - ${resp.message}`);
      allPassed = false;
      continue;
    }
    const r = resp.recipes[0];
    log(`Barros [${dt}s]: ${r.name} (${r.shape}) - ${r.ingredients.length} ingredients`);
    for (const ing of r.ingredients) {
      log(`   - ${ing.id} ${ing.size} ${ing.target_grams}g (${ing.distribution})`);
    }
    log(`   Scores: taste=${r.scores.taste} cost=$${r.scores.cost} profit=${r.scores.profit}%`);

    // Write PC3 DataContract JSON
    const pc3 = {
      ID: 'integration-' + Date.now(),
      Ingredients: r.ingredients.map(ing => ({
        IngredientID: ing.id,
        Rotation: { x: 0, y: 0, z: 0 },
        Position: { x: 0, y: 0, z: 0.95 },
        Size: ing.size === 'Large' ? 0 : ing.size === 'Small' ? 2 : 1
      })),
      DoughPositions: [{ x: 0, y: 0, z: 0 }],
      ProfitFactor: r.profit_factor || 1.5,
      Owner: null,
      Texture: ''
    };
    const safeName = t.name.toLowerCase().replace(/[^a-z0-9]+/g, '-');
    const finalPath = join(outDir, `${safeName}-${Date.now()}.final.json`);
    writeFileSync(finalPath, JSON.stringify(pc3, null, 2));
    log(`   Wrote: ${finalPath}`);

    // Render texture (PIL placeholder)
    if (!skipTexture) {
      const texOk = renderTexture(finalPath);
      log(`   Texture: ${texOk ? 'embedded (256x256 PNG)' : 'FAILED'}`);
    }

    // Run Slice 1 verifier
    if (!skipVerifier && existsSync(finalPath)) {
      const v = runVerifier(finalPath);
      if (v.ok) {
        log(`   Verifier: ✅ PASSED`);
        const checks = v.report?.info?.map(i => i.Check) || [];
        log(`     Checks: ${checks.join(', ')}`);
      } else {
        log(`   Verifier: ❌ FAILED`);
        if (v.report) {
          for (const e of v.report.errors) log(`     ERROR: ${e.Check} - ${e.Message}`);
        } else {
          log(`     ERROR: ${v.error}`);
        }
        allPassed = false;
      }
    }
  }

  log(`\n=== ${allPassed ? 'PASS' : 'PARTIAL'}: ${themes.length} recipes generated ===`);
  if (!allPassed) process.exit(1);
} catch (e) {
  log(`\n=== FAIL: ${e.message} ===`);
  process.exit(1);
}
