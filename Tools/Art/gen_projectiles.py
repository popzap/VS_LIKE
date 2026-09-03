"""탄환 2종 — 플레이어 탄 · 적 탄 (C33).

왜 만드나
  Proj_Bullet 과 Proj_EnemyBolt 가 **같은 스프라이트**(ICON/Bullet.png, 50px)를 썼다.
  색만 달랐는데(적탄은 붉은 틴트) 이 게임은 이미 색 신호를 셋 쓴다 —
  엘리트/보스 외곽선(보라·빨강) · 피격 플래시 · 적탄 틴트.
  넷째를 색으로 얹으면 안 읽힌다. **모양으로 갈라야 한다.**

가른 축 = 방향성
  플레이어 탄 : 길쭉한 예광탄. **어디로 가는지가 보인다** (내가 쏜 것)
  적      탄 : 방사 대칭 가시 구슬. **방향이 없다** (피할 것)
  ProjectileBase.cs:43 이 Atan2 로 진행 방향을 향해 회전시키므로
  길쭉한 쪽은 텍스처에서 **+X(오른쪽)** 를 향해야 한다.

🔴 픽셀아트로 그린다 — 기존 투사체를 재서 맞춘 값이다
  Arrow.png · Shuriken.png 를 실측했더니 **256px 안에 4px 블록 = 논리 64x64**,
  색은 7~13개, 검은 외곽선이 있다. 처음에 매끈한 그라디언트로 뽑았다가
  Arrow 옆에 놓으니 혼자 다른 게임 물건처럼 보여서 다시 그렸다.
  ⇒ 64x64 로 그리고 **NEAREST 로 4배 확대**한다. 안티에일리어싱을 넣지 않는다.

크기 — 월드 크기를 지금과 똑같이 맞춘다 (게임플레이 변화 0)
  기존: Bullet.png 50px / PPU 100 = 0.5 유닛
  신규: 256px / PPU 512 = 0.5 유닛  → 프리팹 scale 을 안 건드려도 된다
        (적탄은 프리팹 scale 0.55 라 그대로 0.275 유닛)

색
  플레이어 탄 : 텍스처가 색을 갖는다 (프리팹 m_Color 가 흰색이라 그대로 나온다).
  적      탄 : 텍스처는 **무채색**이다. 프리팹이 (1, 0.3, 0.2)로 틴트하므로
                색은 코드가 정하고 텍스처는 명암만 준다.
"""
from PIL import Image
import os

L     = 64      # 논리 해상도
SCALE = 4       # 4배 확대 -> 256px (Arrow.png 실측값과 같다)

OUTLINE = (26, 22, 38, 255)     # 공통 외곽선

def new_grid():
    return [[None] * L for _ in range(L)]

def add_outline(g, color=OUTLINE):
    """칠해진 칸의 바깥 테두리를 한 칸 두른다. 4방향 인접만 본다."""
    out = [row[:] for row in g]
    for y in range(L):
        for x in range(L):
            if g[y][x] is not None:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < L and 0 <= ny < L and g[ny][nx] is not None:
                    out[y][x] = color
                    break
    return out

def save(g, path):
    img = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    px = img.load()
    for y in range(L):
        for x in range(L):
            if g[y][x] is not None:
                px[x, y] = g[y][x]
    img = img.resize((L * SCALE, L * SCALE), Image.NEAREST)
    img.save(path)
    a = img.getchannel("A")
    cols = len(img.convert("RGB").getcolors(maxcolors=100000))
    print(f"{os.path.basename(path):18s} {img.size[0]}x{img.size[1]}  "
          f"alpha {a.getextrema()}  colors {cols}")

# ── 플레이어 탄 — 예광탄 ─────────────────────────────────────────
#
# 코를 오른쪽에 두고 꼬리를 왼쪽으로 길게 뺀다.
# 가장 두꺼운 곳을 코 쪽(x=42)에 몰아야 "어디로 가는지"가 읽힌다 —
# 가운데에 두면 좌우 대칭 렌즈가 되어 방향이 사라진다 (첫 시도에서 밟았다).

P_CORE = (255, 255, 255, 255)
P_MID  = (140, 238, 255, 255)
P_EDGE = (54, 150, 208, 255)

def player_bullet():
    g = new_grid()
    cy = L // 2
    NOSE, TAIL, FAT = 55, 9, 42
    for x in range(TAIL, NOSE + 1):
        if x >= FAT:                                  # 코 — 짧고 급하게 모인다
            k = 1.0 - (x - FAT) / float(NOSE - FAT)
            h = 1 + int(round(5.4 * (k ** 0.55)))
        else:                                         # 꼬리 — 길고 완만하게
            k = (x - TAIL) / float(FAT - TAIL)
            h = int(round(6.4 * (k ** 1.35)))
        if h <= 0:
            continue
        for dy in range(-h, h + 1):
            ay = abs(dy)
            if ay >= h - 0:      c = P_EDGE
            elif ay >= h - 2:    c = P_MID
            else:                c = P_CORE
            # 꼬리 절반은 코어를 쓰지 않는다 — 앞이 더 밝아야 눈이 앞으로 간다
            if x < FAT - 12 and c is P_CORE:
                c = P_MID
            g[cy + dy][x] = c
    return add_outline(g)

# ── 적 탄 — 가시 구슬 ────────────────────────────────────────────
#
# 방사 대칭이라 회전해도 같아 보인다 = "방향이 없다".
# 가시 6개가 실루엣을 원과 다르게 만들어 멀리서도 갈린다.
# 무채색이다 — 프리팹이 붉게 틴트한다.

E_SHELL = (238, 238, 238, 255)
E_SPIKE = (176, 176, 176, 255)
E_CORE  = (86,  86,  92,  255)

def enemy_bolt():
    import math
    g = new_grid()
    cx = cy = L // 2
    BODY, SPIKE, CORE = 12.0, 21.0, 6.0
    for y in range(L):
        for x in range(L):
            dx, dy = x - cx + 0.5, y - cy + 0.5
            d = math.hypot(dx, dy)
            if d > SPIKE:
                continue
            th = math.atan2(dy, dx)
            s  = abs(math.cos(3.0 * th)) ** 5          # 6갈래
            edge = BODY + (SPIKE - BODY) * s
            if d > edge:
                continue
            g[y][x] = E_CORE if d <= CORE else (E_SHELL if d <= BODY else E_SPIKE)
    return add_outline(g)

if __name__ == "__main__":
    out = "Assets/Game/Sprites/Projectiles"
    os.makedirs(out, exist_ok=True)
    save(player_bullet(), f"{out}/PlayerBullet.png")
    save(enemy_bolt(),    f"{out}/EnemyBolt.png")
