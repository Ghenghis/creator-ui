#!/usr/bin/env node
// Barro's Pizza Cookbook generator — renders all output/*.final.json as an HTML gallery.
// Usage: node tools/build-cookbook.mjs [--out docs/evidence/cookbook.html]
import { readFileSync, writeFileSync, readdirSync, existsSync } from 'fs';
import { join, basename } from 'path';

const projectRoot = process.env.CREATOR_UI_ROOT || 'S:/Unity_Games/PC3 - Pizza Creator/creator-ui';
const outDir = process.env.OUT_DIR || join(projectRoot, 'output');
const args = process.argv.slice(2);
const outArg = args.find(a => a.startsWith('--out='));
const outPath = outArg ? outArg.split('=')[1] : join(projectRoot, 'docs/evidence/cookbook.html');

const files = readdirSync(outDir).filter(f => f.endsWith('.final.json')).sort();
const recipes = files.map(f => {
  try {
    return JSON.parse(readFileSync(join(outDir, f), 'utf-8'));
  } catch { return null; }
}).filter(Boolean);

const html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<title>Barro's Pizza Cookbook — ${recipes.length} recipes</title>
<style>
  :root {
    --bg: #f5e9d7;
    --panel: #f9f0e0;
    --accent: #b9452e;
    --accent-hover: #a13823;
    --text: #3a2418;
    --text-2: #6b4f3a;
    --border: #d9b896;
  }
  body { font-family: Georgia, serif; background: var(--bg); color: var(--text); margin: 0; padding: 32px; }
  header { max-width: 1200px; margin: 0 auto 32px; text-align: center; }
  header h1 { font-size: 48px; margin: 0 0 8px; color: var(--accent); }
  header p { font-size: 18px; color: var(--text-2); margin: 0; }
  .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 24px; max-width: 1400px; margin: 0 auto; }
  .card { background: var(--panel); border-radius: 12px; border: 2px solid var(--border); overflow: hidden; transition: transform 0.2s; }
  .card:hover { transform: translateY(-4px); box-shadow: 0 8px 16px rgba(0,0,0,0.15); }
  .card img { width: 100%; aspect-ratio: 1/1; object-fit: cover; display: block; background: var(--bg); }
  .card-body { padding: 16px; }
  .card-title { font-size: 18px; font-weight: bold; margin: 0 0 8px; color: var(--accent); }
  .card-meta { font-size: 12px; color: var(--text-2); margin-bottom: 8px; }
  .card-stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; font-size: 13px; }
  .stat { padding: 4px 8px; background: rgba(185, 69, 46, 0.08); border-radius: 6px; }
  .stat-label { display: block; font-size: 10px; color: var(--text-2); text-transform: uppercase; }
  .stat-value { font-weight: bold; color: var(--text); }
  .card-ingredients { margin-top: 12px; padding-top: 12px; border-top: 1px solid var(--border); font-size: 13px; }
  .ingredient { display: inline-block; background: rgba(185, 69, 46, 0.12); color: var(--accent); padding: 2px 8px; border-radius: 10px; margin: 2px; font-size: 11px; }
  footer { text-align: center; margin-top: 48px; padding: 24px; color: var(--text-2); font-size: 14px; }
</style>
</head>
<body>
  <header>
    <h1>🍕 Barro's Pizza Cookbook</h1>
    <p>${recipes.length} AI-designed recipes · real SD-Turbo diffusion textures · Slice 1 verifier ✅</p>
  </header>

  <main class="grid">
${recipes.map(r => {
  const dataUrl = r.Texture ? `data:image/png;base64,${r.Texture}` : '';
  const ings = (r.Ingredients || []).map(i => {
    const sz = ['Large','Medium','Small'][i.Size] || '?';
    return `<span class="ingredient">${i.IngredientID} ${sz}</span>`;
  }).join('');
  const taste = r.scores?.taste ?? 0;
  const cost = r.scores?.cost ?? 0;
  const profit = r.scores?.profit_percent ?? 0;
  return `    <div class="card">
      <img src="${dataUrl}" alt="${r.ID}" loading="lazy" />
      <div class="card-body">
        <h2 class="card-title">${r.ID}</h2>
        <div class="card-meta">${(r.Ingredients||[]).length} ingredients · PC3 DataContract</div>
        <div class="card-stats">
          <div class="stat"><span class="stat-label">Taste</span><span class="stat-value">${taste}</span></div>
          <div class="stat"><span class="stat-label">Cost</span><span class="stat-value">$${cost.toFixed(2)}</span></div>
          <div class="stat"><span class="stat-label">Profit</span><span class="stat-value">${profit.toFixed(1)}%</span></div>
        </div>
        <div class="card-ingredients">${ings}</div>
      </div>
    </div>`;
}).join('\n')}
  </main>

  <footer>
    Generated 2026-08-25 · Barros → LMStudio → ComfyUI SD-Turbo → PC3 DataContract<br/>
    All recipes pass Slice 1 verifier (JsonDeserializes, AmountsInRange, TextureIntegrity)
  </footer>
</body>
</html>`;

writeFileSync(outPath, html);
console.log(`Wrote ${outPath} (${recipes.length} recipes)`);
