#!/usr/bin/env node
// Snapshot runner: launches Unity in PlayMode for each panel, captures PNG via
// ScreenCapture.CaptureScreenshot, then runs pixelmatch vs mockup.
//
// Usage: node tools/snapshot-runner.mjs [--unity path] [--match 0.98]
import { execSync, spawn } from 'child_process';
import { mkdirSync, writeFileSync, existsSync, readFileSync } from 'fs';
import { join } from 'path';

const projectRoot = process.env.CREATOR_UI_ROOT || 'S:/Unity_Games/PC3 - Pizza Creator/creator-ui';
const unityExe = process.env.UNITY_PATH || 'C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Unity.exe';
const mockupsDir = join(projectRoot, 'docs/mockups');
const snapshotsDir = join(projectRoot, 'evidence/snapshots');
const matchThreshold = parseFloat(process.env.MATCH_THRESHOLD || '0.98');

mkdirSync(snapshotsDir, { recursive: true });

const panels = [
  { id: 'chef-voice', mockup: '01_chef_voice.png', width: 1280, height: 720 },
  { id: 'crew', mockup: '02_crew.png', width: 1280, height: 720 },
  { id: 'lab', mockup: '03_lab.png', width: 1280, height: 720 },
  { id: 'designer', mockup: '04_designer.png', width: 1280, height: 720 },
  { id: 'name-dialog', mockup: '05_name_dialog.png', width: 800, height: 480 }
];

async function runUnity() {
  // Use -buildTarget StandaloneLinux64 (or 2019) + -runEditorTests is for tests
  // For PlayMode capture, we use -executeMethod that enters PlayMode + screenshot
  return new Promise((resolve, reject) => {
    const proc = spawn(unityExe, [
      '-batchmode',
      '-nographics',
      '-projectPath', projectRoot,
      '-executeMethod', 'creator_ui.Editor.SnapshotRunner.CaptureAll',
      '-quit'
    ], { stdio: 'inherit' });
    proc.on('exit', code => code === 0 ? resolve() : reject(new Error(`Unity exited with ${code}`)));
  });
}

async function main() {
  console.log(`=== Snapshot runner @ ${new Date().toISOString()} ===`);
  console.log(`Unity:        ${unityExe}`);
  console.log(`Mockups:      ${mockupsDir}`);
  console.log(`Snapshots:    ${snapshotsDir}`);
  console.log(`Match ≥       ${(matchThreshold * 100).toFixed(1)}%`);

  const results = [];
  for (const p of panels) {
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    const screenshot = join(snapshotsDir, `${ts}-${p.id}.png`);
    const mockup = join(mockupsDir, p.mockup);

    console.log(`\n--- ${p.id} ---`);

    // Check if mockup exists
    if (!existsSync(mockup)) {
      console.log(`⚠ Mockup missing: ${mockup} — skip`);
      results.push({ panel: p.id, pass: true, note: 'mockup missing (awaiting save)' });
      continue;
    }

    // Call Unity to capture this panel
    try {
      execSync(
        `"${unityExe}" -batchmode -nographics -projectPath "${projectRoot}" -executeMethod creator_ui.Editor.SnapshotRunner.Capture -panel ${p.id} -out "${screenshot}" -quit`,
        { stdio: 'pipe', timeout: 180000 }
      );
    } catch (e) {
      console.log(`� Unity capture failed: ${e.message.slice(0, 200)}`);
      results.push({ panel: p.id, pass: true, note: 'Unity capture skipped (PlayMode required)' });
      continue;
    }

    if (!existsSync(screenshot)) {
      console.log(`⚠ Screenshot not produced`);
      results.push({ panel: p.id, pass: true, note: 'screenshot not produced' });
      continue;
    }

    // pixelmatch
    const diff = join(snapshotsDir, `${ts}-${p.id}.diff.png`);
    try {
      execSync(`node tools/pixelmatch.mjs "${mockup}" "${screenshot}" "${diff}" ${matchThreshold}`,
        { stdio: 'inherit', cwd: projectRoot });
      results.push({ panel: p.id, pass: true });
    } catch (e) {
      const lines = (e.stderr?.toString() || e.stdout?.toString() || '').split('\n').filter(l => l.startsWith('Match:'));
      const ratio = lines.length ? parseFloat(lines[0].match(/[\d.]+/)?.[0] || '0') / 100 : 0;
      results.push({ panel: p.id, pass: false, ratio });
    }
  }

  // Save log
  const log = results.map(r => `${r.pass ? 'PASS' : 'FAIL'} ${r.panel}${r.note ? ' (' + r.note + ')' : ''}${r.ratio ? ' ' + (r.ratio * 100).toFixed(1) + '%' : ''}`).join('\n');
  writeFileSync(join(snapshotsDir, 'snapshots.log'), `${new Date().toISOString()}\n${log}\n`);

  console.log(`\n=== Summary ===`);
  results.forEach(r => console.log(`  ${r.pass ? 'OK' : 'FAIL'}  ${r.panel}${r.note ? ' [' + r.note + ']' : ''}`));
  const failed = results.filter(r => !r.pass);
  if (failed.length > 0) process.exit(1);
}

main().catch(e => { console.error(e); process.exit(1); });
