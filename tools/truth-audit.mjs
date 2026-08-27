#!/usr/bin/env node
import { existsSync, readFileSync } from 'fs';
import { join, resolve } from 'path';

const root = resolve(process.env.CREATOR_UI_ROOT || '.');
const requiredSource = [
  'Assets/Scripts/Sidebar/TabNavigator.cs',
  'Assets/Scripts/Chat/DesignerPanel.cs',
  'Assets/UI/Panels/Designer.uxml',
  'Assets/UI/Panels/Designer.uss',
  'ProjectSettings/ProjectVersion.txt'
];
const mockups = [
  'docs/mockups/01-chef-voice.png',
  'docs/mockups/02-crew.png',
  'docs/mockups/03-lab.png',
  'docs/mockups/04-designer.png',
  'docs/mockups/05-name-dialog.png'
];
const acceptanceInputs = [
  'Assets/Scenes/CreatorUI.unity',
  'Assets/Scripts/Editor/SnapshotRunner.cs',
  ...mockups
];
const missingSource = requiredSource.filter(path => !existsSync(join(root, path)));
const missingAcceptance = acceptanceInputs.filter(path => !existsSync(join(root, path)));
const versionPath = join(root, 'ProjectSettings/ProjectVersion.txt');
const unityVersion = existsSync(versionPath)
  ? (readFileSync(versionPath, 'utf8').match(/m_EditorVersion:\s*(.+)/)?.[1]?.trim() || 'unknown')
  : 'unknown';
const report = {
  schema_version: '1.0.0',
  component: 'creator-ui design prototype',
  unity_version: unityVersion,
  source_contract_ok: missingSource.length === 0,
  pixel_acceptance_ready: missingAcceptance.length === 0,
  missing_source: missingSource,
  missing_acceptance_inputs: missingAcceptance,
  truth_rule: 'Portable audit success is not a Unity render, installed-game injection, or pixel-match pass.'
};
console.log(JSON.stringify(report, null, 2));
if (missingSource.length) process.exit(1);
