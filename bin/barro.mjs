#!/usr/bin/env node
// Barro's Pizza CLI — wraps the Barros sidecar + LMStudio + Slice 1 verifier.
// Usage:
//   barro compose "Make a margherita pizza" [--name Margherita] [--heat Medium]
//   barro lab --tags "spicy,budget,under-15" [--count 3]
//   barro verify <pizza.final.json>
//   barro previews --all
//   barro health
//   barro doctor (run all health checks)

import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'fs';
import { execSync } from 'child_process';
import { readdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const projectRoot = join(__dirname, '..');
const barrosBase = process.env.BARROS_URL || 'http://127.0.0.1:48173';
const lmstudioBase = process.env.LMSTUDIO_URL || 'http://127.0.0.1:1234';
const outDir = process.env.OUT_DIR || join(projectRoot, 'output');
const verifierDir = process.env.VERIFIER_DIR || 'S:/Unity_Games/PC3 - Pizza Creator/_pizza-agent';
const gameDir = process.env.GAME_DIR || 'S:/Unity_Games/PC3 - Pizza Creator/_decompiled/Assembly-CSharp';
mkdirSync(outDir, { recursive: true });

const catalogPath = join(projectRoot, 'Assets/StreamingAssets/catalog.json');
const catalog = JSON.parse(readFileSync(catalogPath, 'utf-8'));
const barrosCatalog = catalog.ingredients.map(ing => ({
  id: ing.id,
  name: ing.name || ing.id,
  type_id: ing.type || 'Unknown',
  sizes: ['Large', 'Medium', 'Small'].map(sz => ({
    size: sz,
    grams: sz === 'Large' ? ing.max_g : sz === 'Small' ? ing.min_g / 2 : (ing.min_g + ing.max_g) / 2,
    cost: ((sz === 'Large' ? ing.max_g : sz === 'Small' ? ing.min_g / 2 : (ing.min_g + ing.max_g) / 2) / 100) * (ing.base_price || 0)
  }))
}));

const args = process.argv.slice(2);
const cmd = args[0];
const flags = parseFlags(args.slice(1));

function parseFlags(arr) {
  const out = {};
  for (let i = 0; i < arr.length; i++) {
    const a = arr[i];
    if (a.startsWith('--')) {
      const key = a.slice(2);
      const next = arr[i + 1];
      if (!next || next.startsWith('--')) out[key] = true;
      else { out[key] = next; i++; }
    } else if (!out._positional) out._positional = [a];
    else out._positional.push(a);
  }
  return out;
}

async function fetchJSON(url, opts = {}) {
  const r = await fetch(url, opts);
  if (!r.ok) throw new Error(`HTTP ${r.status} on ${url}`);
  return r.json();
}

async function health() {
  console.log(`Barro's Pizza CLI`);
  console.log(`================`);
  console.log(`creator-ui:    ${projectRoot}`);
  console.log(`Barros:        ${await fetch(`${barrosBase}/health`).then(r => r.ok ? 'OK' : 'FAIL').catch(() => 'UNREACHABLE')}`);
  console.log(`LMStudio:      ${(await fetch(`${lmstudioBase}/v1/models`).then(r => r.ok ? 'OK' : 'FAIL').catch(() => 'UNREACHABLE'))}`);
  console.log(`Catalog:       ${catalog.ingredients.length} ingredients`);
  console.log(`Output dir:    ${outDir}`);
}

async function doctor() {
  let allOk = true;
  const checks = [];

  // Barros
  try {
    const h = await fetchJSON(`${barrosBase}/health`);
    checks.push({ name: 'Barros sidecar', ok: h.ok, detail: `provider=${h.provider} v${h.version}` });
  } catch (e) {
    checks.push({ name: 'Barros sidecar', ok: false, detail: e.message });
    allOk = false;
  }

  // LMStudio
  try {
    const m = await fetchJSON(`${lmstudioBase}/v1/models`);
    const qwen3 = m.data?.find(x => x.id.includes('qwen3'));
    checks.push({ name: 'LMStudio', ok: true, detail: `${m.data?.length || 0} models${qwen3 ? `, primary=${qwen3.id}` : ''}` });
  } catch (e) {
    checks.push({ name: 'LMStudio', ok: false, detail: e.message });
    allOk = false;
  }

  // Unity
  const unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Unity.exe';
  checks.push({ name: 'Unity 6', ok: existsSync(unityExe), detail: unityExe });

  // Slice 1 verifier
  checks.push({ name: 'Slice 1 verifier', ok: existsSync(verifierDir), detail: verifierDir });

  // Catalog
  checks.push({ name: 'creator-ui catalog', ok: catalog.ingredients.length >= 6, detail: `${catalog.ingredients.length} ingredients` });

  for (const c of checks) {
    const tag = c.ok ? '✅' : '❌';
    console.log(`${tag} ${c.name.padEnd(20)} ${c.detail}`);
  }
  process.exit(allOk ? 0 : 1);
}

async function compose() {
  const prompt = flags._positional?.[0];
  if (!prompt) { console.error('Usage: barro compose "prompt" [--name X] [--heat Medium]'); process.exit(1); }
  const name = flags.name || prompt.split(' ').slice(0, 2).join('');
  const heat = flags.heat || 'Medium';
  const resp = await fetchJSON(`${barrosBase}/compose`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, catalog: barrosCatalog, count: 1, heat })
  });
  if (!resp.ok) { console.error('FAIL:', resp.message); process.exit(1); }
  const r = resp.recipes[0];
  console.log(`✅ ${r.name} (${r.shape}) — ${r.ingredients.length} ingredients`);
  console.log(`   Scores: taste=${r.scores.taste} cost=$${r.scores.cost} profit=${r.scores.profit}%`);
  for (const ing of r.ingredients) console.log(`   - ${ing.id} ${ing.size} ${ing.target_grams}g (${ing.distribution})`);

  // Write PC3 DataContract
  const pc3 = {
    ID: name + '-' + Date.now(),
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
  const finalPath = join(outDir, `${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${Date.now()}.final.json`);
  writeFileSync(finalPath, JSON.stringify(pc3, null, 2));
  console.log(`📁 Wrote ${finalPath}`);
  console.log(`   Run \`barro verify "${finalPath}"\` to validate`);
  console.log(`   Run \`barro previews\` to render texture`);
}

async function lab() {
  const tagStr = (flags.tags || '').split(',').map(s => s.trim()).filter(Boolean);
  if (tagStr.length === 0) { console.error('Usage: barro lab --tags "spicy,budget" [--count 3]'); process.exit(1); }
  const count = parseInt(flags.count || '3');
  const prompt = `Pizza with these qualities: ${tagStr.join(', ')}.`;
  const resp = await fetchJSON(`${barrosBase}/lab`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, tags: tagStr, count, catalog: barrosCatalog, heat: 'Medium' })
  });
  if (!resp.ok) { console.error('FAIL:', resp.message); process.exit(1); }
  for (const r of resp.recipes || []) {
    console.log(`✅ ${r.name} (${r.shape}) — taste=${r.scores.taste} profit=${r.scores.profit}%`);
    for (const ing of r.ingredients) console.log(`   - ${ing.id} ${ing.size} ${ing.target_grams}g`);
  }
}

async function verify() {
  const pizzaPath = flags._positional?.[0];
  if (!pizzaPath) { console.error('Usage: barro verify <pizza.final.json>'); process.exit(1); }
  // Resolve to absolute path (dotnet verifier needs abs)
  const path = await import('path');
  const absPath = path.resolve(pizzaPath);
  if (!existsSync(absPath)) { console.error(`Not found: ${absPath}`); process.exit(1); }
  const reportPath = `C:/Users/Admin/AppData/Local/Temp/barro-vr-${Date.now()}.json`;
  try {
    const out = execSync(
      `cd "${verifierDir}" && dotnet run --project verifier/PizzaAgent.Verify.csproj -- --pizza "${absPath}" --game-dir "${gameDir}" --out "${reportPath}"`,
      { stdio: 'pipe', timeout: 60000 }
    ).toString();
    const report = JSON.parse(readFileSync(reportPath, 'utf-8'));
    if (report.passed) {
      console.log(`✅ PASSED`);
      console.log(`   Checks: ${(report.info || []).map(i => i.Check).join(', ')}`);
    } else {
      console.log(`❌ FAILED`);
      for (const e of report.errors || []) console.log(`   ERROR: ${e.Check} - ${e.Message}`);
      process.exit(1);
    }
  } catch (e) {
    console.error('Verifier failed:', e.message?.slice(0, 300));
    process.exit(1);
  }
}

async function previews() {
  const all = flags.all;
  let files = [];
  if (flags._positional?.length) {
    files = flags._positional;
  } else if (all) {
    files = readdirSync(outDir).filter(f => f.endsWith('.final.json')).map(f => join(outDir, f));
  } else {
    console.error('Usage: barro previews <pizza.json>... OR barro previews --all');
    process.exit(1);
  }
  for (const f of files) {
    if (!existsSync(f)) { console.error(`Not found: ${f}`); continue; }
    try {
      execSync(`node tools/render-texture.mjs "${f}"`, { stdio: 'inherit', cwd: projectRoot });
      console.log(`✅ ${f}`);
    } catch (e) {
      console.error(`❌ ${f}: ${e.message?.slice(0, 200)}`);
    }
  }
}

async function special() {
  const themes = [
    'Spicy diavola with hot salami and chili oil',
    'Margherita with fresh basil from the garden',
    'Quattro formaggi with aged Italian cheeses',
    'Hawaiian with pineapple and smoked ham',
    'Vegan supreme with mushrooms and roasted peppers',
    'Mediterranean with olives, feta, and sun-dried tomatoes',
    'Pepperoni and jalapeño with hot honey drizzle',
    'Buffalo chicken with blue cheese and celery',
    'Truffle mushroom with arugula and parmesan',
    'Pesto chicken with sun-dried tomatoes',
    'White pizza with ricotta and garlic',
    'BBQ pulled pork with smoked gouda',
    'Spinach and artichoke with cream cheese',
    'Greek-style with olives, feta, and oregano',
    'Meat lovers with pepperoni, sausage, and bacon',
    'Dessert pizza with Nutella and strawberries',
    'Lemon and ricotta with fresh basil',
    'Sausage and caramelized onion',
    'Roasted vegetable medley with balsamic glaze',
    'Shrimp scampi with garlic and parsley'
  ];
  let theme;
  if (flags.seed) {
    const seed = parseInt(flags.seed);
    theme = themes[Math.abs(Math.floor(Math.sin(seed) * 10000)) % themes.length];
  } else {
    theme = themes[Math.floor(Math.random() * themes.length)];
  }
  const name = `Special-${Date.now()}`;
  console.log(`🎲 Today's special: "${theme}"`);
  flags._positional = [theme];
  flags.name = name;
  flags.heat = flags.heat || 'Medium';
  await compose();
}

// === Dispatch ===
console.log('');
switch (cmd) {
  case 'health': await health(); break;
  case 'doctor': await doctor(); break;
  case 'compose': await compose(); break;
  case 'lab': await lab(); break;
  case 'verify': await verify(); break;
  case 'previews': await previews(); break;
  case 'special': await special(); break;
  default:
    console.log(`Barro's Pizza CLI`);
    console.log(`Usage:`);
    console.log(`  barro compose "Make a margherita pizza" [--name Margherita] [--heat Medium]`);
    console.log(`  barro lab --tags "spicy,budget,under-15" [--count 3]`);
    console.log(`  barro special [--seed X]`);
    console.log(`  barro verify <pizza.final.json>`);
    console.log(`  barro previews --all`);
    console.log(`  barro previews <pizza.json>...`);
    console.log(`  barro health`);
    console.log(`  barro doctor`);
}
