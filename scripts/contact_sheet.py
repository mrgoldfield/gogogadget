#!/usr/bin/env python3
"""Contact sheet for scripts/shots.sh: one row per scenario folder.

Each row shows the gadget on a dark and on a light backdrop, then the
saved dialogs and windows, all scaled to a common row height so one image gives an
overview. Usage: contact_sheet.py <shots-dir> <out.png> [row-height]
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROW_HEIGHT = 320
LABEL_WIDTH = 150
GAP = 10
MAX_WIDTH = 2000


def row_images(folder: Path):
    names = ["gadget-on-dark.png", "gadget-on-light.png"]
    names += sorted(p.name for p in folder.glob("dialog-*.png"))
    names += sorted(p.name for p in folder.glob("window-*.png"))
    return [folder / n for n in names if (folder / n).exists()]


def main() -> int:
    shots, out = Path(sys.argv[1]), Path(sys.argv[2])
    row_height = int(sys.argv[3]) if len(sys.argv) > 3 else ROW_HEIGHT
    rows = []
    for folder in sorted(p for p in shots.iterdir() if p.is_dir()):
        images = []
        for path in row_images(folder):
            im = Image.open(path).convert("RGB")
            scale = min(1.0, row_height / im.height)
            images.append(im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale)))))
        error = folder / "error.txt"
        rows.append((folder.name, images, error.read_text(errors="replace")[:200] if error.exists() else ""))
    if not rows:
        print("contact sheet: no scenario folders")
        return 1

    width = min(MAX_WIDTH, max(LABEL_WIDTH + sum(i.width + GAP for i in imgs) for _, imgs, _ in rows))
    height = sum(max([i.height for i in imgs] + [40]) + GAP for _, imgs, _ in rows)
    sheet = Image.new("RGB", (width, height), (128, 128, 128))
    draw = ImageDraw.Draw(sheet)
    y = 0
    for name, images, error in rows:
        draw.text((6, y + 6), name, fill=(255, 255, 255))
        if error:
            draw.text((6, y + 22), "ERROR: " + error, fill=(255, 80, 80))
        x = LABEL_WIDTH
        for im in images:
            if x + im.width > width:
                break
            sheet.paste(im, (x, y))
            x += im.width + GAP
        y += max([i.height for i in images] + [40]) + GAP
    sheet.save(out)
    print(f"contact sheet: {out} ({width}x{height}, {len(rows)} rows)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
