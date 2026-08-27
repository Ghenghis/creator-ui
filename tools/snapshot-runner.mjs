#!/usr/bin/env node
// Orchestrates: Unity Editor screenshot -> pixelmatch vs mockup -> emit evidence
import { execSync } from 'child_process';
import { mkdirSync, existsSync } from 'fs';
import { join } from 'path';

const projectRoot = process.env.CREATOR_UI_ROOT || '/s/Unity_Games/PC3 - Pizza Creator/creator-ui';
const mockupsDir = join(projectRoot, 'docs/mockups');
const snapshotsDir = join(projectRoot, 'evidence/snapshots');
const unityCmd = process.env.UNITY_PATH || 'unity';

if (!existsSync(snapshotsDir)) mkdirSync(snapshotsDir, { recursive: true });

const panels = [
  { id: 'chef-voice', mockup: '01-chef-voice.png' },
  { id: 'crew', mockup: '02-crew.png' },
  { id: 'lab', mockup: '03-lab.png' },
  { id: 'designer', mockup: '04-designer.png' },
  { id: 'name-dialog', mockup: '05-name-dialog.png' }
];

const minRatio = 0.98;
const results = [];
for (const p of panels) {
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const screenshot = join(snapshotsDir, `${ts}-${p.id}.png`);
  const mockup = join(mockupsDir, p.mockup);
  if (!existsSync(mockup)) {
    console.error(`Required reference mockup missing for ${p.id}: ${mockup}`);
    results.push({ panel: p.id, pass: false, error: 'reference mockup missing' });
    continue;
  }
  try {
    execSync(
      `${unityCmd} -batchmode -projectPath "${projectRoot}" -executeMethod SnapshotRunner.Capture -panel ${p.id} -out "${screenshot}" -quit`,
      { stdio: 'inherit' }
    );
  } catch (e) {
    console.error(`Unity capture failed for ${p.id}: ${e.message}`);
    results.push({ panel: p.id, pass: false, error: e.message });
    continue;
  }
  if (!existsSync(screenshot)) {
    console.error(`Required Unity screenshot missing for ${p.id}`);
    results.push({ panel: p.id, pass: false, error: 'runtime screenshot missing' });
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

const failed = results.filter(r => !r.pass);
if (failed.length > 0) {
  console.error(`${failed.length} panel(s) below ${(minRatio * 100).toFixed(0)}% threshold`);
  failed.forEach(f => console.error(`  ${f.panel}: ratio=${(f.ratio ?? 0).toFixed(2)}`));
  process.exit(1);
}
console.log(`All ${results.length} panels >=${(minRatio * 100).toFixed(0)}% threshold with real captures.`);
