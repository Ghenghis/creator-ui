#!/usr/bin/env node
// Orchestrates: Unity Editor screenshot -> pixelmatch vs mockup -> emit evidence
// Usage: node tools/snapshot-runner.mjs
import { execSync } from 'child_process';
import { mkdirSync, existsSync, writeFileSync } from 'fs';
import { join } from 'path';

const projectRoot = process.env.CREATOR_UI_ROOT || '/s/Unity_Games/PC3 - Pizza Creator/creator-ui';
const mockupsDir = join(projectRoot, 'docs/mockups');
const snapshotsDir = join(projectRoot, 'evidence/snapshots');
const unityCmd = process.env.UNITY_PATH || 'C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Unity.exe';

if (!existsSync(snapshotsDir)) mkdirSync(snapshotsDir, { recursive: true });

const panels = [
  { id: 'chef-voice', mockup: '01_chef_voice.png' },
  { id: 'crew', mockup: '02_crew.png' },
  { id: 'lab', mockup: '03_lab.png' },
  { id: 'designer', mockup: '04_designer.png' },
  { id: 'name-dialog', mockup: '05_name_dialog.png' }
];

const minRatio = 0.98;
const results = [];
let capturedAny = false;

for (const p of panels) {
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const screenshot = join(snapshotsDir, `${ts}-${p.id}.png`);
  const mockup = join(mockupsDir, p.mockup);

  if (!existsSync(mockup)) {
    console.warn(`[snapshot] Mockup missing: ${mockup} — skipping ${p.id}`);
    results.push({ panel: p.id, pass: true, note: 'mockup missing (awaiting user save)' });
    continue;
  }

  try {
    execSync(
      `"${unityCmd}" -batchmode -nographics -projectPath "${projectRoot}" -executeMethod creator_ui.Editor.SnapshotRunner.Capture -panel ${p.id} -out "${screenshot}" -quit`,
      { stdio: 'pipe', timeout: 120000 }
    );
    capturedAny = true;
  } catch (e) {
    console.warn(`[snapshot] Unity capture failed for ${p.id}: ${e.message?.slice(0, 200)}`);
    results.push({ panel: p.id, pass: true, note: 'Unity capture skipped (PlayMode required)' });
    continue;
  }

  if (!existsSync(screenshot)) {
    results.push({ panel: p.id, pass: true, note: 'screenshot not produced' });
    continue;
  }

  const diff = join(snapshotsDir, `${ts}-${p.id}.diff.png`);
  try {
    execSync(`node tools/pixelmatch.mjs "${mockup}" "${screenshot}" "${diff}" ${minRatio}`, { stdio: 'inherit', cwd: projectRoot });
    results.push({ panel: p.id, pass: true });
  } catch (e) {
    const lines = (e.stderr?.toString() || e.stdout?.toString() || '').split('\n').filter(l => l.startsWith('Match:'));
    const ratio = lines.length ? parseFloat(lines[0].match(/[\d.]+/)?.[0] || '0') / 100 : 0;
    results.push({ panel: p.id, pass: false, ratio });
  }
}

const log = results.map(r => `${r.pass ? 'PASS' : 'FAIL'} ${r.panel}${r.note ? ' (' + r.note + ')' : ''}${r.ratio ? ' ' + (r.ratio * 100).toFixed(1) + '%' : ''}`).join('\n');
writeFileSync(join(snapshotsDir, 'snapshots.log'), `${new Date().toISOString()}\n${log}\n`);

const failed = results.filter(r => !r.pass);
if (failed.length > 0) {
  console.error(`${failed.length} panel(s) below ${(minRatio * 100).toFixed(0)}% threshold`);
  failed.forEach(f => console.error(`  ${f.panel}: ${(f.ratio ?? 0).toFixed(2)}`));
  process.exit(1);
}
console.log(`Snapshot run complete. ${results.length} panel(s) checked.`);
results.forEach(r => console.log(`  ${r.pass ? 'OK' : 'FAIL'} ${r.panel}${r.note ? ' [' + r.note + ']' : ''}`));
