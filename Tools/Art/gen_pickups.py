# 픽업 4종 + 행운 아이콘 1종 (C23 · 요청-12 ①)
#
# 뽑는 것 — 규격이 두 가지다. 섞지 말 것:
#
#  1) _Incoming/Sprites/Pickups/{Bomb,Invincible,Haste,Gold}.png   1024x1024 · PPU 2048
#     → 바닥에 떨어지는 픽업. **이미 있는 Magnet.png 와 나란히 놓인다.**
#       크기 계약: 알파 bbox 높이 = **약 482px** (Magnet 482 · Chest 484 와 같게).
#       PPU 2048 이므로 화면에서 약 0.236 유닛 = 플레이어 키(1.0)의 24%.
#
#  2) _Incoming/Sprites/Passives/Luck.png                          1024x1024 · PPU 512
#     → 레벨업 3택 / 상점의 아이콘. 픽업이 아니라 **UI 그림**이다.
#       크기 계약: 알파 bbox 약 **700px** (GoldGain 715x676 · XpGain 418x668).
#
# 🔴 왜 논리 32 격자인가
#   Magnet 은 bbox 489x482 인데 눈으로 세면 약 16블록이다 => 블록 하나가 약 32px.
#   1024 / 32 = 32 이므로 **논리 캔버스 32x32 를 NEAREST 로 32배** 하면 결이 맞는다.
#   기존 그림은 AI 산출물이라 블록이 정확히 안 떨어진다(가장자리가 뭉개져 있다).
#   여기서 굽는 건 격자에 딱 맞으므로 더 또렷하다 — 그건 개선이지 불일치가 아니다.
#
# 🔴 투명 배경은 quantize() 가 마스크로 만든다 (I-41 의 가짜 알파가 아니다).
#   스크립트 끝에서 알파 min/max 를 반드시 찍어 확인한다.

import math
import numpy as np
from PIL import Image, ImageDraw

S = 16                     # 슈퍼샘플 배율
L = 32                     # 논리 격자
W = L * S
OUT = (18, 20, 32)         # 외곽선 — 기존 픽업의 검은 테두리와 같은 역할

# ── 팔레트 ────────────────────────────────────────────────────────────────
# 🔴 이미 쓰인 색을 피한다. 자석=빨강 · 상자=갈색 · 경험치=초록/파랑 젬 · 적=회색~보라.
BOMB_D  = (38, 42, 58);   BOMB_M = (60, 66, 88);   BOMB_H = (118, 126, 156)
FUSE    = (146, 104, 58);  SPARK1 = (255, 168, 48); SPARK2 = (255, 232, 128); SPARK3 = (255, 255, 240)
GOLD_D  = (170, 116, 22);  GOLD_M = (234, 180, 48); GOLD_H = (255, 228, 132)
BLUE_D  = (26,  86, 178);  BLUE_M = (72, 160, 246); BLUE_H = (176, 220, 255)
GRN_D   = (34, 108, 44);   GRN_M  = (76, 176, 76);  GRN_H  = (150, 216, 116)


def ell(d, cx, cy, rx, ry, fill):
    d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=fill)


def poly(d, pts, fill):
    d.polygon([(x * S, y * S) for x, y in pts], fill=fill)


def quantize(rgb, msk, target_h):
    """gen_octopus.py 와 같은 방식 — 마스크로 알파를 만들고 바깥 한 겹에 외곽선을 두른다.

    target_h 는 **외곽선까지 포함한** 최종 논리 높이다. 그리기 좌표가 조금 틀어져도
    여기서 다시 맞추므로, 아래 그림 함수들은 대략만 맞으면 된다."""
    bb = msk.getbbox()
    bw, bh = bb[2] - bb[0], bb[3] - bb[1]
    # 외곽선이 한 겹 붙으므로 알맹이는 (target_h - 2) 논리px 로 굽는다
    k = (target_h - 2) * S / bh
    if bw * k > (L - 4) * S:
        k = (L - 4) * S / bw
    nw, nh = max(1, int(bw * k)), max(1, int(bh * k))
    cr = Image.new('RGB', (W, W), OUT)
    cm = Image.new('L', (W, W), 0)
    cr.paste(rgb.crop(bb).resize((nw, nh), Image.LANCZOS), ((W - nw) // 2, (W - nh) // 2))
    cm.paste(msk.crop(bb).resize((nw, nh), Image.LANCZOS), ((W - nw) // 2, (W - nh) // 2))

    px = cr.resize((L, L), Image.BOX).load()
    ap = cm.resize((L, L), Image.BOX).point(lambda v: 255 if v >= 128 else 0).load()
    out = Image.new('RGBA', (L, L), (0, 0, 0, 0))
    op = out.load()
    for y in range(L):
        for x in range(L):
            if ap[x, y]:
                op[x, y] = px[x, y] + (255,)
    for y in range(L):
        for x in range(L):
            if not ap[x, y] and any(0 <= x + dx < L and 0 <= y + dy < L and ap[x + dx, y + dy]
                                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1),
                                                   (1, 1), (1, -1), (-1, 1), (-1, -1))):
                op[x, y] = OUT + (255,)
    return out


def canvas():
    rgb = Image.new('RGB', (W, W), OUT)
    msk = Image.new('L', (W, W), 0)
    return rgb, msk, ImageDraw.Draw(rgb), ImageDraw.Draw(msk)


# ── 논리 픽셀을 직접 찍는 길 ──────────────────────────────────────────────
# 🔴 왜 두 갈래인가 — 첫 판에서 폭탄/번개/동전이 **못 알아볼 그림으로 나왔다.**
#    슈퍼샘플 후 임계값으로 깎는 방식은 곡선(방패·클로버)에는 잘 듣지만,
#    15논리px 안에서 **가늘고 꺾이는 것**(번개)이나 **여러 개가 겹치는 것**(동전 더미)은
#    다운샘플이 통째로 뭉갠다 — 번개는 파란 얼룩이, 동전 더미는 금색 식빵이 나왔다.
#    그래서 이 셋만 픽셀을 손으로 찍는다. 이 크기에선 그리는 게 아니라 **세는** 것이다.
def stamp(rows, palette):
    """문자 격자 -> 32x32 RGBA. '.' 은 투명. 외곽선은 여기서 안 넣는다(outline() 이 넣는다)."""
    img = Image.new('RGBA', (L, L), (0, 0, 0, 0))
    p = img.load()
    h, w = len(rows), max(len(r) for r in rows)
    oy, ox = (L - h) // 2, (L - w) // 2
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != '.':
                p[ox + x, oy + y] = palette[ch] + (255,)
    return img


def outline(img):
    """gen_octopus.py 의 마지막 단계와 같다 — 알파 바깥 한 겹에 OUT 을 두른다."""
    p = img.load()
    op = [(x, y) for y in range(L) for x in range(L) if p[x, y][3]]
    ink = set(op)
    for x, y in list(ink):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < L and 0 <= ny < L and (nx, ny) not in ink:
                p[nx, ny] = OUT + (255,)
    return img


def shade(rows, hi, mid, dark):
    """'#' 만 찍은 실루엣에 볼륨을 준다 — 각 줄의 왼쪽 끝은 밝게, 오른쪽 끝은 어둡게.
    번개처럼 획이 얇은 그림은 이 규칙 하나로 금속처럼 읽힌다."""
    out = []
    for row in rows:
        cs = list(row)
        idx = [i for i, ch in enumerate(cs) if ch == '#']
        for i in idx:
            cs[i] = 'M'
        if idx:
            cs[idx[0]] = 'H'
            cs[idx[-1]] = 'D'
        out.append(''.join(cs))
    return out, {'H': hi, 'M': mid, 'D': dark}


# ── 1. Bomb — 검정 구체 + 타는 심지 ───────────────────────────────────────
# 실루엣만으로 읽혀야 한다. **동그란 구체 + 대각선 심지 + 불꽃** = 폭탄 말고 없다.
# 🔴 구체가 동그래야 한다. 첫 판은 둥근 사각형으로 나와 "검은 자루"로 읽혔다 —
#    그래서 9x9 원을 줄별 폭(5/7/9/9/9/9/9/7/5)으로 못박아 찍는다.
BOMB_MAP = [
    '.........s..',
    '........SWS.',
    '.......f.s..',
    '.....ff.....',
    '..HHMMM.....',
    '.HHHMMMM....',
    'HHHMMMMMM...',
    'HHMMMMMMM...',
    'HMMMMMMMD...',
    'MMMMMMMDD...',
    'MMMMMMDDD...',
    '.MMMMMDD....',
    '..MMDDD.....',
]
BOMB_PAL = {'H': BOMB_H, 'M': BOMB_M, 'D': BOMB_D,
            'f': FUSE, 's': SPARK1, 'S': SPARK2, 'W': SPARK3}

# ── 3. Haste — 파랑 번개 ──────────────────────────────────────────────────
# 🔴 번개는 **꺾이는 자리**로 읽힌다. 위 획이 왼쪽 아래로 내려오다 가로로 삐치고,
#    아래 획이 그 오른쪽 끝에서 다시 왼쪽 아래로 간다. 이 계단이 없으면 그냥 사선이다.
HASTE_MAP = [
    '......####',
    '.....####.',
    '....####..',
    '...####...',
    '..####....',
    '.#########',
    '.########.',
    '.....####.',
    '....####..',
    '...####...',
    '..####....',
    '.####.....',
    '.##.......',
]

# ── 4. Gold — 동전 6닢 피라미드 ───────────────────────────────────────────
# ⚠️ Passives/GoldGain.png 도 동전 더미다. **일부러 비슷하게 둔다** — 하나는 UI 아이콘이고
#    하나는 바닥 픽업이라 같은 화면에 안 나온다. 오히려 "이 둘이 같은 것"이 읽혀야 한다.
#    대신 GoldGain 의 초록 화살표는 여기 없다(그게 패시브라는 표식이다).
# 🔴 옆에서 본 동전탑은 쓰지 않는다. 첫 판에서 **금색 식빵**이 나왔다 —
#    가로줄만으로는 낱개가 안 갈린다. 앞에서 본 원반을 겹쳐 쌓으면 테두리가 서로를 가른다.
COIN = [
    '.DDD.',
    'DHMMD',
    'DMMMD',
    'DMMMD',
    '.DDD.',
]
GOLD_PAL = {'D': GOLD_D, 'M': GOLD_M, 'H': GOLD_H}


def gold_map():
    g = [['.'] * 15 for _ in range(13)]
    # 🔴 가로 간격은 5 다 — 낱개 폭과 같게. 4 로 겹치면 서로의 잘린 모서리를 메워
    #    금괴를 쌓은 것처럼 보인다(실제로 그렇게 나왔다). 5 라야 위아래에 홈이 남아 갈린다.
    # 아래 -> 위 순서로 찍어야 위 동전이 아래 동전을 덮는다
    for oy, xs in ((8, (0, 5, 10)), (4, (2, 7)), (0, (5,))):
        for ox in xs:
            for y, row in enumerate(COIN):
                for x, ch in enumerate(row):
                    if ch != '.':
                        g[oy + y][ox + x] = ch
    return [''.join(r) for r in g]


# ── 2. Invincible — 금빛 방패 ─────────────────────────────────────────────
# 별은 안 쓴다. 이 게임엔 이미 노란 반짝임(경험치·골드)이 많아 별이면 그것들과 섞인다.
# 방패는 **아래가 뾰족한 실루엣**이라 다른 어떤 픽업과도 안 겹친다.
def draw_invincible():
    rgb, msk, c, m = canvas()
    body = [(10.2, 10.0), (21.8, 10.0), (21.8, 15.0), (20.6, 19.4),
            (16.0, 23.6), (11.4, 19.4), (10.2, 15.0)]
    inner = [(11.6, 11.4), (20.4, 11.4), (20.4, 15.0), (19.4, 18.6),
             (16.0, 21.8), (12.6, 18.6), (11.6, 15.0)]
    poly(m, body, 255)
    poly(c, body, GOLD_D)
    poly(c, inner, GOLD_M)
    # 왼쪽 위 사선 광택 — 금속으로 읽히게 하는 유일한 단서다
    poly(c, [(12.4, 12.2), (15.6, 12.2), (13.4, 19.0), (12.4, 16.4)], GOLD_H)
    ell(c, 18.4, 13.6, 1.0, 1.2, GOLD_H)
    return rgb, msk


# ── 5. Luck — 네잎클로버 (패시브 아이콘) ──────────────────────────────────
# 🔴 주사위는 안 쓴다. 이 게임에 도박 UI 가 없어서 "다시 굴린다"로 오해된다.
#    클로버는 초록인데, 기존 패시브 아이콘 10종 중 초록이 주인공인 건 없다 (XpGain 은 파랑 젬).
def draw_luck():
    rgb, msk, c, m = canvas()
    cx, cy, dist, r = 16.0, 13.4, 5.6, 3.4
    for d, cols in ((m, (255, 255, 255)), (c, (GRN_M, GRN_D, GRN_H))):
        # 줄기 — 잎보다 먼저 그려 잎이 덮게 한다
        for k in range(11):
            u = k / 10.0
            ell(d, cx + 0.4 + u * 1.7, cy + 4.4 + u * 8.2, 0.8 - u * 0.28, 0.8 - u * 0.28,
                cols[0] if d is m else GRN_D)
    for a in range(4):
        th = math.radians(45 + a * 90)
        lx, ly = cx + math.cos(th) * dist, cy - math.sin(th) * dist
        px_, py_ = -math.sin(th), -math.cos(th)          # 잎을 가르는 수직 방향
        # 하트형 잎 = 원 두 개 + 클로버 중심을 향한 삼각형. 원만 쓰면 사이가 안 파여 꽃이 된다
        for d, col in ((m, 255), (c, GRN_M)):
            ell(d, lx + px_ * 1.9, ly + py_ * 1.9, r * 0.72, r * 0.72, col)
            ell(d, lx - px_ * 1.9, ly - py_ * 1.9, r * 0.72, r * 0.72, col)
            poly(d, [(lx + px_ * 2.5, ly + py_ * 2.5), (lx - px_ * 2.5, ly - py_ * 2.5),
                     (cx, cy)], col)
        ell(c, lx + px_ * 1.4 - math.cos(th) * 0.6, ly + py_ * 1.4 + math.sin(th) * 0.6,
            r * 0.40, r * 0.40, GRN_H)                   # 잎마다 같은 자리에 광택
    ell(c, cx, cy, 1.5, 1.5, GRN_D)                      # 잎이 만나는 가운데를 눌러 준다
    return rgb, msk


# ── 굽기 ──────────────────────────────────────────────────────────────────
# 🔴 목표 bbox: 픽업 15논리px(= 480px) · 패시브 아이콘 22논리px(= 672~704px)
#    'map' 은 손으로 찍은 격자(크기를 이미 맞춰 뒀다) · 'draw' 는 곡선(quantize 가 맞춘다)
HASTE_ROWS, HASTE_PAL = shade(HASTE_MAP, BLUE_H, BLUE_M, BLUE_D)
JOBS = [
    ('Pickups',  'Bomb',       'map',  (BOMB_MAP, BOMB_PAL),     15),
    ('Pickups',  'Invincible', 'draw', draw_invincible,          15),
    ('Pickups',  'Haste',      'map',  (HASTE_ROWS, HASTE_PAL),  15),
    ('Pickups',  'Gold',       'map',  (gold_map(), GOLD_PAL),   15),
    ('Passives', 'Luck',       'draw', draw_luck,                22),
]

import os
print('%-15s %-9s %-11s %-9s %s' % ('file', 'size', 'alpha bbox', 'alpha', 'ink'))
for folder, name, kind, fn, th in JOBS:
    if kind == 'map':
        small = outline(stamp(fn[0], fn[1]))
    else:
        small = quantize(*fn(), th)
    big = small.resize((1024, 1024), Image.NEAREST)
    d = '_Incoming/Sprites/' + folder
    os.makedirs(d, exist_ok=True)
    big.save('%s/%s.png' % (d, name))

    a = np.array(big)[:, :, 3]
    ys, xs = np.nonzero(a > 8)
    # 🔴 I-41 검사 — 알파 min 이 0 이 아니면 배경이 가짜다
    print('%-15s %-9s %-11s %-9s %.1f%%'
          % (name + '.png', '1024^2',
             '%dx%d' % (xs.max() - xs.min() + 1, ys.max() - ys.min() + 1),
             '%d/%d' % (a.min(), a.max()),
             100.0 * (a > 8).sum() / a.size))

print('')
print('reference: Magnet 489x482 / Chest 659x484 / GoldGain 715x676 (alpha 0/255)')
print('pickups must match ~482 height. Luck must match ~700.')
