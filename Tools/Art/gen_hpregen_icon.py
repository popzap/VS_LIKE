"""체력 재생 패시브 아이콘 — Regeneration (C47).

왜 만드나
  `D74` 가 `HpRegen` 패시브를 넣으면서 아이콘을 `MaxHp.png`(빨간 하트) 그대로 썼다.
  ⇒ **최대 체력과 그림이 같다.** 레벨업 카드에서 둘을 못 가른다.
  `C33`(탄환)·`C46`(Swift 픽업)에 이어 **세 번째 같은 문제**고, 답도 같다 —
  **색만이 아니라 실루엣을 갈라야 한다.**

🔴 어떻게 갈랐나
  MaxHp = **빨간 하트**(아래가 뾰족한 실루엣).
  이건 **초록 회전 화살표 + 십자**(둥근 고리 실루엣) 로 잡았다.
    · 실루엣 : 뾰족한 하트 ↔ **둥근 고리** — 작게 줄여도 갈린다
    · 색     : 빨강 ↔ **초록** — 색약이어도 실루엣이 남는다
    · 뜻     : 회전 화살표는 "되돌아온다", 십자는 "치유" — 재생을 곧장 가리킨다

규격 — 패시브 11장을 재서 맞췄다 (C34 와 같다)
  1024x1024 · PPU 512 · Point · 알파 0-255 · 검은 외곽선 · 밝은 부분에 글로우 halo
  논리 64x64 를 16배 확대한다.
"""
from PIL import Image, ImageFilter
import math, os

L, SCALE = 64, 16
OUTLINE = (18, 14, 26, 255)

C_HOT  = (232, 255, 226, 255)   # 십자 중심
C_MID  = (126, 226, 116, 255)   # 초록 본체
C_DEEP = (46, 150, 66, 255)     # 짙은 가장자리

def new_grid():
    return [[None] * L for _ in range(L)]

def add_outline(g, color=OUTLINE):
    out = [r[:] for r in g]
    for y in range(L):
        for x in range(L):
            if g[y][x] is not None:
                continue
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < L and 0 <= ny < L and g[ny][nx] is not None:
                        out[y][x] = color
                        break
                else:
                    continue
                break
    return out

def build():
    g = new_grid()
    cx = cy = L / 2.0 - 0.5
    R, W = 21.0, 4.2            # 고리 반지름·두께
    A0, A1 = -0.35, math.pi * 1.62   # 호의 시작·끝 (3/4 바퀴)

    # ── 회전 호 ──
    # 🔴 처음엔 고리를 두 군데 끊고 촉을 둘 달았는데, 64px 에서 촉이 부서져
    #    한쪽이 노이즈 조각처럼 보였다. **호 하나 + 촉 하나**로 줄였다 —
    #    "새로고침" 기호와 같은 모양이라 작아져도 뜻이 안 흐려진다.
    for y in range(L):
        for x in range(L):
            dx, dy = x - cx, y - cy
            d = math.hypot(dx, dy)
            if abs(d - R) > W / 2:
                continue
            th = math.atan2(dy, dx) % (2 * math.pi)
            a0 = A0 % (2 * math.pi)
            span = (th - a0) % (2 * math.pi)
            if span > (A1 - A0):
                continue
            g[y][x] = C_MID if abs(d - R) <= W / 2 - 1.3 else C_DEEP

    # ── 화살촉 하나 (호 끝에, 접선 방향으로) ──
    the = A0 + (A1 - A0)
    hx, hy = cx + R * math.cos(the), cy + R * math.sin(the)
    tdir = the + math.pi / 2                 # 진행 방향
    for i in range(10):
        half = 5.0 * (1 - i / 10.0)          # 앞으로 갈수록 좁아진다
        k = -3
        while k <= 3:
            if abs(k) <= half:
                px_ = hx + math.cos(tdir) * (i - 3) + math.cos(tdir + math.pi / 2) * k
                py_ = hy + math.sin(tdir) * (i - 3) + math.sin(tdir + math.pi / 2) * k
                ix, iy = int(round(px_)), int(round(py_))
                if 0 <= ix < L and 0 <= iy < L:
                    g[iy][ix] = C_MID if abs(k) <= half - 1.2 else C_DEEP
            k += 1

    # ── 가운데 십자 ──
    AR, TH = 11, 3
    for y in range(L):
        for x in range(L):
            dx, dy = abs(x - cx), abs(y - cy)
            if (dx <= TH and dy <= AR) or (dy <= TH and dx <= AR):
                g[y][x] = C_HOT if (dx <= TH - 1 and dy <= TH - 1) or (dx <= 1 or dy <= 1) else C_MID
    return add_outline(g)

def render(g, path):
    img = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    px = img.load()
    for y in range(L):
        for x in range(L):
            if g[y][x] is not None:
                px[x, y] = g[y][x]
    img = img.resize((L * SCALE, L * SCALE), Image.NEAREST)

    # 글로우 halo — 패시브 11장이 전부 갖고 있는 층이다 (C34 와 같은 방법)
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gp, ip = glow.load(), img.load()
    W_, H_ = img.size
    for y in range(0, H_, 2):
        for x in range(0, W_, 2):
            r, gg, b, a = ip[x, y]
            if a > 0 and (r + gg + b) > 380:
                for oy in (0, 1):
                    for ox in (0, 1):
                        gp[x + ox, y + oy] = (r, gg, b, 140)
    glow = glow.filter(ImageFilter.GaussianBlur(26))
    out = Image.alpha_composite(glow, img)
    out.save(path)
    print(f"{os.path.basename(path)}  {out.size[0]}x{out.size[1]}  "
          f"alpha {out.getchannel('A').getextrema()}  "
          f"colors {len(out.convert('RGB').getcolors(maxcolors=99999))}")

if __name__ == "__main__":
    d = "Assets/Game/Sprites/Passives"
    os.makedirs(d, exist_ok=True)
    render(build(), f"{d}/HpRegen.png")
