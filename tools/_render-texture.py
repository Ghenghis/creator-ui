
import base64, json, math, sys, os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

with open(r"output/whitepizza-1787674178295.final.json") as f:
    pizza = json.load(f)

W, H = 320, 320
img = Image.new('RGB', (W, H), (245, 233, 215))
d = ImageDraw.Draw(img, 'RGBA')

ingredients = pizza.get('Ingredients', [])
title = pizza.get('ID', 'recipe')[:24]

# === Crust (golden brown irregular ring) ===
crust_outer = 152
crust_inner = 138
# Outer crust shadow
for r in range(crust_outer + 4, crust_outer + 1):
    d.ellipse([W/2 - r, H/2 - r, W/2 + r, H/2 + r], outline=(160, 90, 50, 80), width=2)
# Crust with irregular edge (jittered)
import random
random.seed(42)
for i in range(36):
    angle = i * 10 * math.pi / 180
    r1 = crust_outer + random.randint(-3, 4)
    r2 = crust_outer + random.randint(-2, 3)
    x1 = W/2 + r1 * math.cos(angle)
    y1 = H/2 + r1 * math.sin(angle)
    x2 = W/2 + r2 * math.cos(angle + 0.05)
    y2 = H/2 + r2 * math.sin(angle + 0.05)
    d.line([(x1, y1), (x2, y2)], fill=(180, 120, 60, 200), width=2)
# Main crust ring
d.ellipse([W/2 - crust_outer, H/2 - crust_outer, W/2 + crust_outer, H/2 + crust_outer],
          fill=(210, 150, 80), outline=(150, 80, 40), width=2)
# Inner crust ridge (darker)
d.ellipse([W/2 - crust_inner, H/2 - crust_inner, W/2 + crust_inner, H/2 + crust_inner],
          outline=(180, 110, 50), width=1)

# === Tomato sauce (irregular darker red circle) ===
sauce_r = 130
sauce_offset = 4
d.ellipse([W/2 - sauce_r + sauce_offset, H/2 - sauce_r + sauce_offset,
           W/2 + sauce_r + sauce_offset, H/2 + sauce_r + sauce_offset],
          fill=(190, 50, 40))
# Sauce variation spots
random.seed(123)
for _ in range(40):
    a = random.uniform(0, 2 * math.pi)
    r = random.uniform(0, sauce_r - 10)
    x = W/2 + r * math.cos(a)
    y = H/2 + r * math.sin(a)
    sz = random.randint(2, 6)
    shade = random.randint(150, 220)
    d.ellipse([x - sz, y - sz, x + sz, y + sz], fill=(shade, 30 + random.randint(0, 40), 30, 120))

# === Melted cheese layer (translucent white-yellow over sauce) ===
cheese_layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
cd = ImageDraw.Draw(cheese_layer)
# Soft cheese blob
for _ in range(25):
    a = random.uniform(0, 2 * math.pi)
    r = random.uniform(20, sauce_r - 15)
    x = W/2 + r * math.cos(a)
    y = H/2 + r * math.sin(a)
    sz = random.randint(8, 18)
    cd.ellipse([x - sz, y - sz, x + sz, y + sz], fill=(255, 245, 220, 60))
# Brighter cheese highlights
for _ in range(15):
    a = random.uniform(0, 2 * math.pi)
    r = random.uniform(30, sauce_r - 25)
    x = W/2 + r * math.cos(a)
    y = H/2 + r * math.sin(a)
    sz = random.randint(4, 10)
    cd.ellipse([x - sz, y - sz, x + sz, y + sz], fill=(255, 250, 235, 100))
cheese_layer = cheese_layer.filter(ImageFilter.GaussianBlur(2))
img.paste(cheese_layer, (0, 0), cheese_layer)
d = ImageDraw.Draw(img, 'RGBA')

# === Ingredient blobs (per-ingredient colors) ===
cx, cy = W/2, H/2
n = max(1, len(ingredients))
ing_color_map = {"Mozzarella":[255,248,230],"Tomato":[220,50,47],"PizzaSauce":[200,40,40],"Basil":[50,140,60],"Bacon":[150,70,50],"Chicken":[200,170,110],"CookedChicken":[200,170,110],"Jalapeño":[80,160,70],"Jalapeno":[80,160,70],"Pepperoni":[180,50,40],"Salami":[170,60,50],"Mushroom":[220,200,180],"Olive":[60,70,50],"Pineapple":[240,220,80],"Ham":[220,150,150],"Onion":[240,230,220],"Pepper":[80,200,80],"Sausage":[180,120,90],"Ricotta":[255,252,240],"Parmesan":[240,230,180],"Gorgonzola":[200,210,230],"Anchovy":[120,100,70],"Artichoke":[140,170,90],"Arugula":[80,130,70],"Pesto":[60,120,50],"Truffle":[80,60,40],"Garlic":[250,245,230]}
for i, ing in enumerate(ingredients):
    iid = ing['IngredientID']
    if iid in ing_color_map:
        base_color = tuple(ing_color_map[iid])
    else:
        # hash-based fallback
        h = sum(ord(c) for c in iid)
        base_color = (180 + (h % 70), 100 + ((h >> 4) % 100), 80 + ((h >> 8) % 80))

    # Position on circle
    angle = (i / n) * 2 * math.pi - math.pi / 2
    r = 50 + ((sum(ord(c) for c in iid) % 30))
    x = cx + r * math.cos(angle)
    y = cy + r * math.sin(angle)

    # Blob size based on Size enum (0=Large, 1=Medium, 2=Small)
    sz_map = {0: 32, 1: 24, 2: 14}
    sz = sz_map.get(ing.get('Size', 1), 22)

    # Shadow
    d.ellipse([x - sz + 2, y - sz + 3, x + sz + 2, y + sz + 3], fill=(0, 0, 0, 50))
    # Main blob
    d.ellipse([x - sz, y - sz, x + sz, y + sz], fill=base_color + (230,))
    # Highlight
    hl = tuple(min(255, c + 40) for c in base_color)
    d.ellipse([x - sz // 2, y - sz // 2, x - sz // 4, y - sz // 4], fill=hl + (200,))

# === Herb specs (small green dots scattered) ===
for ing in ingredients:
    if ing['IngredientID'] in ["Basil","Arugula","Pesto","Oregano","Parsley"]:
        for _ in range(8):
            a = random.uniform(0, 2 * math.pi)
            r = random.uniform(0, 100)
            x = cx + r * math.cos(a)
            y = cy + r * math.sin(angle) if False else cy + r * math.sin(a)
            d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=(50, 130, 60, 200))

# === Title text with shadow ===
try:
    title_font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 18)
    sub_font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 12)
    cap_font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 10)
except Exception:
    title_font = ImageFont.load_default()
    sub_font = title_font
    cap_font = title_font

# Caption top-left
caption = f"PC3 recipe - {len(ingredients)} ingredients"
d.text((8, 6), caption, fill=(58, 36, 24, 220), font=cap_font)

# Title bottom-center with shadow
tw = d.textlength(title, font=title_font)
tx = (W - tw) / 2
ty = H - 30
# Shadow
d.text((tx + 1, ty + 1), title, fill=(40, 25, 18, 200), font=title_font)
# Title text
d.text((tx, ty), title, fill=(255, 248, 235, 240), font=title_font)

# === Save and embed ===
import io
buf = io.BytesIO()
img.save(buf, format='PNG', optimize=True)
b64 = base64.b64encode(buf.getvalue()).decode('ascii')

pizza['Texture'] = b64
with open(r"output/whitepizza-1787674178295.final.json", 'w') as f:
    json.dump(pizza, f, indent=2)
print(f"Embedded realistic texture ({len(b64)} chars) into {pizza['ID']}")
