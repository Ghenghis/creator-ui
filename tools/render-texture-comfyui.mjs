#!/usr/bin/env node
// ComfyUI-backed realistic pizza texture renderer.
// Sends SD-Turbo workflow via /prompt, waits for completion, downloads the PNG.
// Falls back to PIL placeholder if ComfyUI is unreachable.
//
// Usage: node tools/render-texture-comfyui.mjs <pizza-final-json>
import { readFileSync, writeFileSync, existsSync } from 'fs';
import { execSync } from 'child_process';
import { join, basename } from 'path';
import { homedir } from 'os';

const projectRoot = process.env.CREATOR_UI_ROOT || 'S:/Unity_Games/PC3 - Pizza Creator/creator-ui';
const pizzaPath = process.argv[2];
if (!pizzaPath) { console.error('Usage: node tools/render-texture-comfyui.mjs <pizza-final-json>'); process.exit(1); }

const pizza = JSON.parse(readFileSync(pizzaPath, 'utf-8'));
const comfyBase = process.env.COMFYUI_URL || 'http://127.0.0.1:8188';
const comfyOutputDir = process.env.COMFYUI_OUTPUT || 'S:/ComfyUI/output';

// Build prompt from recipe ingredients
const ings = (pizza.Ingredients || []).map(i => {
  const sz = {0:'large',1:'medium',2:'small'}[i.Size] || '';
  return `${i.IngredientID} ${sz}`.trim();
}).join(', ');
const name = pizza.ID || 'pizza';
const prompt = `top-down view of ${name}, ${ings}, photorealistic food photography, melted cheese, golden crust, wooden table, soft natural lighting, professional food photo, shallow depth of field`;

// ComfyUI workflow for SD-Turbo (1-4 steps, CFG ~1.5)
const workflow = {
  "3": {
    "inputs": {
      "seed": Math.floor(Math.random() * 1e9),
      "steps": 4,
      "cfg": 1.5,
      "sampler_name": "euler",
      "scheduler": "karras",
      "denoise": 1,
      "model": ["4", 0],
      "positive": ["6", 0],
      "negative": ["7", 0],
      "latent_image": ["5", 0]
    },
    "class_type": "KSampler"
  },
  "4": { "inputs": { "ckpt_name": "sd-turbo.safetensors" }, "class_type": "CheckpointLoaderSimple" },
  "5": { "inputs": { "width": 320, "height": 320, "batch_size": 1 }, "class_type": "EmptyLatentImage" },
  "6": { "inputs": { "text": prompt, "clip": ["4", 1] }, "class_type": "CLIPTextEncode" },
  "7": { "inputs": { "text": "blurry, ugly, low quality, watermark, text", "clip": ["4", 1] }, "class_type": "CLIPTextEncode" },
  "8": { "inputs": { "samples": ["3", 0], "vae": ["4", 2] }, "class_type": "VAEDecode" },
  "9": { "inputs": { "filename_prefix": `barros_${Date.now()}`, "images": ["8", 0] }, "class_type": "SaveImage" }
};

async function fetchJSON(url, opts = {}) {
  const r = await fetch(url, opts);
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  return r.json();
}

async function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

async function main() {
  // Check ComfyUI reachable
  try {
    await fetch(`${comfyBase}/system_stats`);
  } catch (e) {
    console.error(`[comfyui] unreachable at ${comfyBase}: ${e.message}`);
    console.error(`[comfyui] falling back to PIL placeholder`);
    execSync(`node tools/render-texture.mjs "${pizzaPath}"`, { stdio: 'inherit', cwd: projectRoot });
    process.exit(0);
  }

  // Queue the prompt
  const clientId = `barros-${Date.now()}`;
  const body = { prompt: workflow, client_id: clientId };
  const queueResp = await fetchJSON(`${comfyBase}/prompt`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });
  const promptId = queueResp.prompt_id;
  if (!promptId) {
    console.error(`[comfyui] queue failed:`, queueResp);
    process.exit(1);
  }

  // Wait for completion (poll /history/{id})
  let attempts = 0;
  while (attempts < 60) { // max 5 min
    await sleep(5000);
    attempts++;
    try {
      const hist = await fetchJSON(`${comfyBase}/history/${promptId}`);
      const entry = Object.values(hist)[0];
      if (entry?.status?.completed) {
        const filename = entry.outputs?.['9']?.images?.[0]?.filename;
        if (filename) {
          // Download the image
          const viewResp = await fetch(`${comfyBase}/view?filename=${encodeURIComponent(filename)}&type=output`);
          const buf = Buffer.from(await viewResp.arrayBuffer());
          const b64 = buf.toString('base64');

          // Embed into pizza JSON
          pizza.Texture = b64;
          writeFileSync(pizzaPath, JSON.stringify(pizza, null, 2));
          console.log(`[comfyui] ✅ Embedded ${buf.length} byte PNG (${attempts * 5}s) into ${name}`);
          return;
        }
      }
    } catch (e) {
      // poll failed, retry
    }
  }
  console.error(`[comfyui] timed out after ${attempts * 5}s`);
  process.exit(1);
}

main().catch(e => {
  console.error(`[comfyui] error:`, e.message);
  process.exit(1);
});
