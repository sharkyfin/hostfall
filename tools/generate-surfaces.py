#!/usr/bin/env python3
"""Original deterministic, seamless industrial surface maps. Requires Pillow and numpy."""
from pathlib import Path
from PIL import Image, ImageFilter, ImageDraw
import numpy as np

out = Path(__file__).resolve().parent.parent / 'apps/unity/Assets/_HOSTFALL/Content/Textures'
out.mkdir(exist_ok=True)
rng = np.random.default_rng(4717)
n = 512
for kind in ['paint', 'concrete', 'floor']:
    noise = rng.normal(0, 1, (n, n))
    # Blur a repeated tile and take its center to keep opposite boundaries periodic.
    repeated = np.tile(np.uint8(np.clip(noise * 30 + 128, 0, 255)), (3, 3))
    smooth = np.array(Image.fromarray(repeated).filter(ImageFilter.GaussianBlur(12)), dtype=float)[n:2*n, n:2*n] - 128
    if kind == 'paint':
        height = 184 + noise * 1.4 + smooth * .6
    elif kind == 'concrete':
        height = 186 + noise * 4 + smooth * 2.4
    else:
        yy, xx = np.mgrid[:n, :n]
        height = 187 + noise * 2 + smooth * .4 + ((xx + yy) % 16 < 3) * 5
    im = Image.fromarray(np.uint8(np.clip(height, 0, 255))).convert('RGB')
    draw = ImageDraw.Draw(im)
    if kind == 'paint':
        for _ in range(100):
            x, y = rng.integers(0, n, 2)
            draw.line((int(x), int(y), int(x + rng.integers(3, 25)), int(y)), fill=(165, 166, 165), width=1)
    if kind == 'floor':
        draw.rectangle((2, 2, 509, 509), outline=(130, 135, 139), width=2)
        for x in [15, 496]:
            for y in [15, 496]:
                draw.ellipse((x-3, y-3, x+3, y+3), fill=(102, 109, 113))
                draw.line((x-2, y, x+2, y), fill=(56, 65, 70))
    im.save(out / (kind + '.png'))
    h = np.array(im.convert('L'), dtype=float) / 255
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * .5
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * .5
    normal = np.stack((-dx*2, dy*2, np.ones_like(h)), axis=2)
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    Image.fromarray(np.uint8(np.clip((normal*.5+.5)*255, 0, 255))).save(out / (kind + '-normal.png'))
