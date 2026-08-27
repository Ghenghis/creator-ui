# Barro's Pizza Creator Chat UI

Unity UI Toolkit design prototype for the Barro's Pizza Creator chat experience. It contains four panel layouts, a name dialog, recipe JSON logic and a sidebar navigator. It is **not** the installed-game mod; live injection is owned by `Ghenghis/Barros-Pizza-Creator`.

Current source truth: the panel source exists, but the repository does not yet contain a runnable `CreatorUI.unity` scene, a `SnapshotRunner.Capture` implementation, the five reference mockup PNGs or retained runtime captures. Therefore no pixel-match or installed-game pass is claimed.

## Quick start

1. Use Unity `6000.0.51f1`, matching `ProjectSettings/ProjectVersion.txt`.
2. Run `node tools/truth-audit.mjs` to see the exact missing acceptance inputs.
3. Add the scene, capture runner and licensed reference PNGs before running snapshot acceptance.
4. Port only accepted style/layout measurements into the runtime `Barros-Pizza-Creator` mod.

## Tests

```bash
# Portable source/readiness audit
node tools/truth-audit.mjs

# EditMode/PlayMode require the matching Unity Editor
"$UNITY_PATH" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml -quit
"$UNITY_PATH" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults-PlayMode.xml -quit

# Fail-closed snapshots: all reference PNGs and real captures are mandatory
node tools/snapshot-runner.mjs
```

## Scope lock

PC3 / Barro's Pizza only. PC2 (Fast Food Tycoon 2) is PROHIBITED. Do not import PC2 paths, fields, or models.

## Spec + Plan

- Design: `docs/superpowers/specs/2026-08-25-barros-creator-chat-ui-design.md`
- Plan: `docs/superpowers/plans/2026-08-25-barros-creator-chat-ui.md`
- Evidence contract: `docs/evidence.md`
- Runtime implementation: `https://github.com/Ghenghis/Barros-Pizza-Creator`
