# Truth Proof Evidence

Every snapshot test run writes 3 artifacts per panel to `evidence/snapshots/`:

- `{timestamp}-{panel}.png` - Unity Editor screenshot of the rendered panel
- `{timestamp}-{panel}.diff.png` - pixel diff vs mockup (red = mismatch)
- (logs in `evidence/snapshots.log`)

## Current retained state

No real Unity capture or installed-game screenshot is retained in this repository yet. The portable CI job audits source/readiness only and cannot satisfy pixel or runtime acceptance.

## Acceptance threshold

- **Target:** >=98.0% pixel match per panel
- **Stretch:** >=99.0%
- **Per-panel release gate:** >=98.0%; a missing reference, capture, scene, capture runner, or diff is a failure
- **Installed-game gate:** separately capture the injected fifth tab from Creator `0.11.272`; this prototype cannot satisfy that gate

## PC3 scope guard

Every commit is checked for PC2 contamination:
```bash
grep -RIlE "FastFood|Fast Food Tycoon|amount_oz" Assets --include='*.cs' --include='*.json'
```
Expected: no output. If `amount_oz` appears, STOP - PC2 contamination.
