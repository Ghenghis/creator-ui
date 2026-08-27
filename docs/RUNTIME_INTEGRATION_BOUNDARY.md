# Creator UI runtime integration boundary

## Repository roles

| Repository | Authority |
|---|---|
| `creator-ui` | UXML/USS design laboratory and controlled pixel experiments |
| `PC3_Pizza-Creator` | Private decompiled-source and reverse-engineering evidence |
| `Barros-Pizza-Creator` | Reversible installed-game tab, backend bridge and live proof |
| `barros-workbench` | Beginner-facing chat, reference selection and generation workflow |
| `PC3_Barros_Runtime_Proof_Studio` | Decoder, replacement-route and runtime-evidence authority |

## Promotion rule

A prototype panel may be promoted into the runtime mod only after its structure,
font, colors, geometry and interaction states have been measured against a real
Creator `0.11.272` capture. UXML/USS source existence is not installed-game proof.

## Minimum acceptance sequence

1. Supply the five licensed reference mockups under `docs/mockups/`.
2. Add and commit a runnable Unity scene plus `SnapshotRunner.Capture`.
3. Run EditMode and PlayMode tests without ignored exit codes.
4. Capture every state at a fixed resolution and scale.
5. Require >=98% per-panel comparison; keep reference, capture, diff and JSON result.
6. Port accepted measurements into `Barros-Pizza-Creator`.
7. Install into a copied Creator tree, navigate through all stock tabs and the AI tab.
8. Capture open/close, resize, prompt, preview, apply, save, reload and uninstall/restore evidence.
