"""메타 강화 아이콘 — Precision (`CritMultiplier`) 1장 (C34).

왜 1장뿐인가
  영구 강화 7종 중 **6종은 스탯이 패시브와 겹쳐서 기존 아이콘을 그대로 쓴다**
  (Vitality->MaxHp · Power->Damage · Magnetism->PickupRadius ·
   Insight->XpGain · Toughness->Armor · Swiftness->MoveSpeed).
  실측으로 재사용 가능한 것도 확인했다 — 패시브 11장 전부 모서리 알파 0 이고
  반투명은 글로우 halo 뿐이다(0~10%). I-41 가짜 투명이 아니다.
  남는 건 `CritMultiplier` 하나다.

왜 CritChance.png 를 안 쓰나
  그건 **다른 스탯**이고 이미 `CritChance` 패시브가 쓴다. 같은 그림을 쓰면
  "치명 확률"과 "치명 배수"가 화면에서 구분되지 않는다.

의미를 어떻게 갈랐나
  CritChance = **맞히는 것** (기존 아이콘은 과녁 + 폭탄)
  Precision  = **크게 터지는 것** ⇒ 큰 폭발 별 + 그 위에 얹힌 얇은 조준 링.
  색도 갈랐다 — 기존 치명/공격 아이콘이 빨강 계열이라 이쪽은 **금색**이다.

규격 — 패시브 11장을 재서 맞췄다
  1024x1024 · PPU 512 · Point · 알파 0-255 · 검은 외곽선 · 밝은 부분에 글로우 halo
  논리 64x64 를 16배 확대한다 (1024/64 = 16).
"""
from PIL import Image, ImageFilter
import math, os

L, SCALE = 64, 16          # 논리 64 -> 1024
OUTLINE = (18, 14, 26, 255)

C_HOT  = (255, 252, 226, 255)   # 별 중심
C_GOLD = (255, 206, 74,  255)   # 별 몸통
C_DEEP = (214, 138, 24,  255)   # 별 가장자리
C_RING = (198, 240, 255, 255)   # 조준 링
C_RING_D = (96, 168, 208, 255)

def new_grid():
    return [[None] * L for _ in range(L)]

def add_outline(g, color=OUTLINE):
    out = [r[:] for r in g]
    for y in range(L):
        for x in range(L):
            if g[y][x] is not None:
                continue
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)):
                nx, ny = x+dx, y+dy
                if 0 <= nx < L and 0 <= ny < L and g[ny][nx] is not None:
                    out[y][x] = color
                    break
    return out

def build():
    g = new_grid()
    cx = cy = L / 2.0 - 0.5

    # ── 폭발 별 (8갈래) ──
    for y in range(L):
        for x in range(L):
            dx, dy = x - cx, y - cy
            d = math.hypot(dx, dy)
            if d < 0.5:
                g[y][x] = C_HOT; continue
            # 🔴 별을 22.5도 틀어 둔다. 이웃 패시브 아이콘 11장이 전부
            #    "비스듬히 놓인 물건"이라, 정면 대칭 엠블럼이면 혼자 겉돈다.
            #    긴 갈래가 링 눈금(상하좌우) 사이로 들어가 구성도 덜 심심해진다.
            th = math.atan2(dy, dx) - math.pi / 8
            # 4갈래는 길고 4갈래는 짧다 — 단조로운 별을 피한다
            s_long  = abs(math.cos(2 * th)) ** 3
            s_short = abs(math.cos(2 * (th - math.pi / 4))) ** 3
            edge = 7.0 + 19.0 * s_long + 10.0 * s_short
            if d > edge:
                continue
            k = d / edge
            g[y][x] = C_HOT if k < 0.30 else (C_GOLD if k < 0.66 else C_DEEP)

    # ── 조준 링 — 별 위에 얹는다 ──
    R, W = 20.5, 1.6
    for y in range(L):
        for x in range(L):
            d = math.hypot(x - cx, y - cy)
            if abs(d - R) <= W:
                g[y][x] = C_RING if abs(d - R) <= W * 0.55 else C_RING_D
    # 링의 눈금 4개 (상하좌우) — 링 밖으로 살짝 뻗는다
    for t in range(int(R - 4), int(R + 6)):
        for c in (C_RING,):
            g[int(cy)][int(cx) + t] = c
            g[int(cy)][int(cx) - t] = c
            g[int(cy) + t][int(cx)] = c
            g[int(cy) - t][int(cx)] = c
    return add_outline(g)

def render(g, path):
    img = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    px = img.load()
    for y in range(L):
        for x in range(L):
            if g[y][x] is not None:
                px[x, y] = g[y][x]
    img = img.resize((L * SCALE, L * SCALE), Image.NEAREST)

    # 글로우 halo — 패시브 11장이 전부 갖고 있는 층이다 (반투명 0~10%).
    # 밝은 픽셀만 뽑아 블러해서 **밑에** 깐다.
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gp, ip = glow.load(), img.load()
    W, H = img.size
    for y in range(0, H, 2):
        for x in range(0, W, 2):
            r, gg, b, a = ip[x, y]
            if a > 0 and (r + gg + b) > 430:
                for oy in (0, 1):
                    for ox in (0, 1):
                        gp[x + ox, y + oy] = (r, gg, b, 150)
    glow = glow.filter(ImageFilter.GaussianBlur(26))
    out = Image.alpha_composite(glow, img)

    out.save(path)
    a = out.getchannel("A")
    print(f"{os.path.basename(path):18s} {out.size[0]}x{out.size[1]}  alpha {a.getextrema()}")

if __name__ == "__main__":
    d = "Assets/Game/Sprites/Passives"
    os.makedirs(d, exist_ok=True)
    render(build(), f"{d}/CritMultiplier.png")
