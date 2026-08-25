
import base64, json, sys
from PIL import Image, ImageDraw, ImageFont

with open(r"S:\\Unity_Games\\PC3 - Pizza Creator\\creator-ui\\output\\diavola-1787671335647.final.json") as f:
    pizza = json.load(f)

W, H = 256, 256
img = Image.new('RGB', (W, H), (245, 233, 215))
d = ImageDraw.Draw(img)

# Crust ring (circle outline)
d.ellipse([8, 8, W-8, H-8], outline=(185, 69, 46), width=6)

# Sauce (inner circle)
d.ellipse([28, 28, W-28, H-28], fill=(220, 50, 47))

# Ingredients as colored blobs (one per ingredient)
ingredients = pizza.get('Ingredients', [])
cx, cy = W/2, H/2
import math
n = max(1, len(ingredients))
for i, ing in enumerate(ingredients):
    color_hex = 'b9452e,daa520,6b8e23,8f5e2e,2e7d8f,a13823,6b4f3a,d9b896'.split(',')
    h = sum(ord(c) for c in ing['IngredientID'])
    color = color_hex[h % len(color_hex)]
    rgb = tuple(int(color[j:j+2], 16) for j in (0, 2, 4))
    angle = (i / n) * 2 * math.pi
    r = 60
    x = cx + r * math.cos(angle)
    y = cy + r * math.sin(angle)
    sz = 35 + (abs(hash(ing['IngredientID'])) % 25)
    d.ellipse([x-sz, y-sz, x+sz, y+sz], fill=rgb)

# Title text
try:
    font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 16)
    small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 11)
except Exception:
    font = ImageFont.load_default()
    small = font

title = pizza.get('ID', 'recipe')[:20]
tw = d.textlength(title, font=font)
d.text(((W - tw) / 2, H - 36), title, fill=(58, 36, 24), font=font)
caption = f"PC3 recipe - {len(ingredients)} ingredients"
d.text((10, 10), caption, fill=(58, 36, 24), font=small)

import io
buf = io.BytesIO()
img.save(buf, format='PNG')
b64 = base64.b64encode(buf.getvalue()).decode('ascii')

pizza['Texture'] = b64
with open(r"S:\\Unity_Games\\PC3 - Pizza Creator\\creator-ui\\output\\diavola-1787671335647.final.json", 'w') as f:
    json.dump(pizza, f, indent=2)
print(f"Embedded texture ({len(b64)} chars) into {pizza['ID']}")
