# Mockups — Truth Spec

These are the truth-spec images for the Barro's Pizza Creator chat UI. Snapshot verification (`tools/snapshot-runner.mjs`) compares rendered Unity panels against these mockups via pixelmatch. Target: ≥98% match per panel.

## Required files

The user shared 8 mockups in chat. They must be saved here as PNGs:

| File | Panel | Status |
|------|-------|--------|
| `01_chef_voice.png` | ChefVoice panel (Bakehouse / Chef Voice mode) | needs save |
| `02_crew.png` | Crew panel (Barro's Design Crew, 4 agents) | needs save |
| `03_lab.png` | Lab panel (AI Pizza Lab, batch mode) | needs save |
| `04_designer.png` | Designer panel (Barro's AI Pizza Designer, hybrid) | needs save |
| `05_name_dialog.png` | Name this pizza dialog (Pizza Nonamo) | needs save |
| `06_load_recipe_book.png` | Bakehouse / Load from recipe book | needs save |
| `07_recipe_save.png` | Bakehouse / Recipe save (Pizza Nonamo) | needs save |
| `08_ingredient_size.png` | Bakehouse / Ingredient size selector | needs save |

The chat panels (01–05) are part of Slice 2 build (this repo). The Bakehouse tabs (06–08) remain in the original PC3 game and are out of scope for this build.

## How to add

1. Save each mockup image (PNG) from chat to this folder with the exact filename above.
2. Verify dimensions:
   - Chat panels: 2048x1147 (or matching the actual share size)
   - Name dialog: roughly square, ~800x400
3. Run `node tools/snapshot-runner.mjs` to verify ≥98% match.

## Saving via PowerShell

```powershell
# After receiving mockup images, save via right-click "Save As" or via clipboard
cd 'S:\Unity_Games\PC3 - Pizza Creator\creator-ui\docs\mockups'
# Example (replace with actual save):
# Copy-Item $env:TEMP\01_chef_voice.png .
```

## Status

- [ ] 01_chef_voice.png
- [ ] 02_crew.png
- [ ] 03_lab.png
- [ ] 04_designer.png
- [ ] 05_name_dialog.png
- [ ] 06_load_recipe_book.png
- [ ] 07_recipe_save.png
- [ ] 08_ingredient_size.png
