#!/usr/bin/env python3
"""LunchFate 应用图标生成器（新粗野主义）。

用途：从一份参数化绘制逻辑生成 PWA 图标与 favicon，避免手工维护二进制资产。
产物（相对仓库根目录）：
  src/LunchFate.Blazor.Client/wwwroot/icon-192.png   PWA 图标（manifest.json 引用）
  src/LunchFate.Blazor.Client/wwwroot/icon-512.png   PWA 图标（manifest.json 引用）
  src/LunchFate.Blazor/wwwroot/favicon.ico           多尺寸 favicon（16/32/48）
  src/LunchFate.Blazor/wwwroot/favicon.svg           SVG favicon（矢量，首选）

设计：明黄底 + 四象限转盘（黑描边 + 零模糊硬阴影 + 米白轴心），对应
      src/LunchFate.Blazor.Client/wwwroot/styles/tokens.css 中的设计令牌。

依赖：Pillow（仅本机生成图标时需要，应用运行时不需要）。
用法：python etc/scripts/generate-icons.py
"""

from __future__ import annotations

import os
from PIL import Image, ImageDraw

# ---- 设计令牌（与 tokens.css 保持一致，改动请同步两处） ----
YELLOW = (255, 210, 63, 255)   # --nb-primary  #FFD23F
PAPER = (255, 253, 245, 255)   # --nb-paper    #FFFDF5
INK = (0, 0, 0, 255)           # --nb-ink      #000000
QUADRANTS = [
    (76, 201, 240, 255),    # --nb-accent-cyan   #4CC9F0（右上）
    (255, 107, 157, 255),   # --nb-accent-pink   #FF6B9D（右下）
    (61, 220, 151, 255),    # --nb-success       #3DDC97（左下）
    (255, 138, 61, 255),    # --nb-accent-orange #FF8A3D（左上）
]

# ---- 比例（相对画布边长，保证任意尺寸下观感一致） ----
WHEEL_RADIUS = 0.36
RING_WIDTH = 0.055
RING_WIDTH_MIN = 1.5
HUB_RADIUS = 0.10
HUB_RING_WIDTH = 0.035
HUB_RING_WIDTH_MIN = 1.0
SHADOW_OFFSET = 0.035
SUPERSAMPLE = 4


def draw_icon(size: int) -> Image.Image:
    """绘制指定边长的图标。

    Args:
        size: 目标边长（像素）。

    Returns:
        边长等于 size 的 RGBA 图像。
    """
    work = size * SUPERSAMPLE
    img = Image.new("RGBA", (work, work), YELLOW)
    draw = ImageDraw.Draw(img)

    cx = cy = work / 2
    radius = WHEEL_RADIUS * work
    ring = max(RING_WIDTH_MIN * SUPERSAMPLE, RING_WIDTH * work)
    hub_radius = HUB_RADIUS * work
    hub_ring = max(HUB_RING_WIDTH_MIN * SUPERSAMPLE, HUB_RING_WIDTH * work)
    offset = SHADOW_OFFSET * work

    # 1. 零模糊硬阴影：整体偏移的纯黑圆，先画，被转盘盖住大部分
    draw.ellipse(
        [cx - radius + offset, cy - radius + offset, cx + radius + offset, cy + radius + offset],
        fill=INK,
    )

    # 2. 四个象限：以圆心为顶点、按角度切出的扇形（Pillow 用 bounding box + 起止角表示）
    box = [cx - radius, cy - radius, cx + radius, cy + radius]
    for index, color in enumerate(QUADRANTS):
        start = -90 + index * 90
        draw.pieslice(box, start=start, end=start + 90, fill=color)

    # 3. 外圈黑描边
    draw.ellipse(box, outline=INK, width=round(ring))

    # 4. 轴心：米白底 + 黑描边
    draw.ellipse(
        [cx - hub_radius, cy - hub_radius, cx + hub_radius, cy + hub_radius],
        fill=PAPER,
        outline=INK,
        width=round(hub_ring),
    )

    return img.resize((size, size), Image.LANCZOS)


SVG_TEMPLATE = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64">
  <title>LunchFate</title>
  <rect width="64" height="64" fill="#ffd23f"/>
  <g transform="translate(2.24 2.24)">
    <circle cx="32" cy="32" r="23.04" fill="#000"/>
  </g>
  <path d="M32 8.96 A23.04 23.04 0 0 1 55.04 32 L32 32 Z" fill="#4cc9f0"/>
  <path d="M55.04 32 A23.04 23.04 0 0 1 32 55.04 L32 32 Z" fill="#ff6b9d"/>
  <path d="M32 55.04 A23.04 23.04 0 0 1 8.96 32 L32 32 Z" fill="#3ddc97"/>
  <path d="M8.96 32 A23.04 23.04 0 0 1 32 8.96 L32 32 Z" fill="#ff8a3d"/>
  <circle cx="32" cy="32" r="23.04" fill="none" stroke="#000" stroke-width="3.52"/>
  <circle cx="32" cy="32" r="6.4" fill="#fffdf5" stroke="#000" stroke-width="2.24"/>
</svg>
"""


def main() -> None:
    """生成全部图标资产。"""
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    client_wwwroot = os.path.join(root, "src", "LunchFate.Blazor.Client", "wwwroot")
    host_wwwroot = os.path.join(root, "src", "LunchFate.Blazor", "wwwroot")

    outputs: list[tuple[str, str]] = []

    for size in (192, 512):
        path = os.path.join(client_wwwroot, f"icon-{size}.png")
        draw_icon(size).save(path, format="PNG", optimize=True)
        outputs.append((path, f"{os.path.getsize(path)} B"))

    # favicon.ico：多尺寸打包，浏览器按需取用
    ico_path = os.path.join(host_wwwroot, "favicon.ico")
    draw_icon(256).save(
        ico_path,
        format="ICO",
        sizes=[(16, 16), (32, 32), (48, 48)],
    )
    outputs.append((ico_path, f"{os.path.getsize(ico_path)} B"))

    # SVG favicon：现代浏览器的首选，矢量、体积极小
    svg_path = os.path.join(host_wwwroot, "favicon.svg")
    with open(svg_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(SVG_TEMPLATE)
    outputs.append((svg_path, f"{os.path.getsize(svg_path)} B"))

    # 覆盖 ABP MVC 主题登录页引用的 /themes/basic/favicon.svg（能否生效取决于静态资源优先级）
    theme_dir = os.path.join(host_wwwroot, "themes", "basic")
    os.makedirs(theme_dir, exist_ok=True)
    theme_svg = os.path.join(theme_dir, "favicon.svg")
    with open(theme_svg, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(SVG_TEMPLATE)
    outputs.append((theme_svg, f"{os.path.getsize(theme_svg)} B"))

    for path, size_text in outputs:
        print(f"  {os.path.relpath(path, root)}  ({size_text})")


if __name__ == "__main__":
    main()
