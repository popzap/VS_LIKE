# 드래곤 소환수 비행 시트 생성기 (C13)
#
# 규격 — 기존 적 걷기 시트와 동일하게 맞춘다:
#   1024x1024 · 4x4 · 셀 256 · PPU 512 · Point 필터 · 압축 없음
#
# 그리는 방식:
#   논리 64x64 격자에 그린다 (기존 적 시트가 눈으로 읽히는 해상도가 이 정도다).
#   실제로는 1024x1024 로 크게 그린 뒤 64 로 내리고, 알파를 128 에서 자르고,
#   4배 NEAREST 로 256 까지 올린다 → 가장자리가 뭉개지지 않는 진짜 도트가 된다.
#
# 16프레임은 sin(2*pi*i/16) 으로만 움직인다 → f15 다음이 f0 과 정확히 이어진다.
# (C1 에서 4프레임을 반복한 이유가 "이어짐 보장"이었는데, 수식으로 그리면 16장이 그냥 이어진다)

import math
from PIL import Image, ImageDraw

S = 16            # 논리 1px = 16px 로 그린다 (64 * 16 = 1024)
L = 64            # 논리 셀 크기
W = L * S         # 작업 캔버스
CELL = 256        # 최종 셀
MARGIN = 9        # 논리 여백 (= 256 기준 36px). B1 때문에 30px 이상이어야 한다

# ── 팔레트 ────────────────────────────────────────────────────────────────
# 적은 초록(고블린·슬라임)·주황(오거)·빨강(데몬)을 이미 쓴다.
# 소환수는 채도 높은 하늘색이다 — 적과 절대 헷갈리면 안 된다.
OUT      = (16, 18, 30)
SC_DARK  = (20, 68, 100)
SC_MID   = (40, 124, 164)
SC_LIT   = (86, 182, 214)
SC_HI    = (170, 226, 242)
BELLY    = (238, 206, 132)
BELLY_D  = (196, 152, 66)
HORN     = (236, 226, 196)
HORN_D   = (176, 162, 128)
MEM_D    = (26, 86, 116)
MEM_M    = (52, 146, 178)
EYE      = (255, 200, 74)
FIRE     = (255, 138, 46)


def P(*pts):
    return [(x * S, y * S) for x, y in pts]


def ell(d, cx, cy, rx, ry, fill):
    d.ellipse([(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S], fill=fill)


def rot(px, py, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return px * c + py * s, -px * s + py * c


WING = [(0, 0), (8, -6.5), (16, -8), (15, 1), (11, -1),
        (10, 5), (6.5, 1.5), (4.5, 8), (1.5, 3), (0, 1.5)]
WING_BONES = [(16, -8), (15, 1), (10, 5), (4.5, 8)]


def draw_wing(d, sx, sy, ang, side, mem, bone):
    """side: +1 오른쪽 / -1 왼쪽. mem/bone 은 색(또는 마스크용 255)"""
    pts = []
    for x, y in WING:
        rx, ry = rot(x, y, ang)
        pts.append(((sx + side * rx) * S, (sy + ry) * S))
    d.polygon(pts, fill=mem)
    for bx, by in WING_BONES:                      # 날개뼈 — 막에 결이 생긴다
        rx, ry = rot(bx, by, ang)
        d.line([(sx * S, sy * S), ((sx + side * rx) * S, (sy + ry) * S)],
               fill=bone, width=int(S * 0.9))


def draw_frame(i, n=16):
    t = i / n
    flap = math.sin(2 * math.pi * t)              # -1(아래) .. +1(위)
    ang = 10 + flap * 40                          # -30° .. +50°
    bob = -math.sin(2 * math.pi * t + math.pi / 2) * 1.6
    sway = math.sin(2 * math.pi * t + math.pi / 3) * 7

    rgb = Image.new('RGB', (W, W), OUT)           # 배경도 외곽선색 — 축소할 때 가장자리가 어둡게 번진다
    msk = Image.new('L', (W, W), 0)
    c = ImageDraw.Draw(rgb)
    m = ImageDraw.Draw(msk)

    cy = 38 + bob
    hy = 21 + bob

    # 1. 날개 (몸 뒤) — 왼쪽을 어둡게 둬서 깊이가 생긴다
    draw_wing(c, 25.5, cy - 6, ang, -1, MEM_D, MEM_D)
    draw_wing(m, 25.5, cy - 6, ang, -1, 255, 255)
    draw_wing(c, 38.5, cy - 6, ang, +1, MEM_M, SC_DARK)
    draw_wing(m, 38.5, cy - 6, ang, +1, 255, 255)

    # 2. 꼬리 — 몸 안쪽에서 시작해 마디로 이어진다.
    #    흔들림(sway)을 마디마다 0 에서 점점 키운다. 뿌리까지 흔들면 몸에서 떨어져 보인다
    tail = [(32, cy + 7.5, 3.4), (33.6 + sway * 0.14, cy + 10.6, 2.9),
            (35.6 + sway * 0.34, cy + 13.2, 2.4), (37.5 + sway * 0.58, cy + 15.2, 1.9)]
    for tx, ty, r in tail:
        for dd, col in ((c, SC_DARK), (m, 255)):
            ell(dd, tx, ty, r, r, col)
    spx, spy = 39.4 + sway * 0.85, cy + 16.6      # 창끝 모양 꼬리 끝
    for dd, col in ((c, SC_MID), (m, 255)):
        dd.polygon(P((spx - 3, spy), (spx, spy - 3.2), (spx + 3, spy), (spx, spy + 3.2)), fill=col)

    # 3. 몸통 — 최종 34논리px 로 줄어드는 것을 감안해 요소를 크고 적게 잡는다.
    #    (첫 시도는 64px 기준으로 그렸다가 눈·발톱이 1px 미만이 되어 뭉갰다)
    for dd, col in ((c, SC_MID), (m, 255)):
        ell(dd, 32, cy, 7.6, 9.2, col)
    ell(c, 30.4, cy - 2.6, 5.4, 6.0, SC_LIT)      # 위쪽 하이라이트
    ell(c, 32, cy + 2.0, 5.4, 6.4, BELLY)         # 배 판때기
    for k in range(2):                            # 배 비늘 — 3줄이면 뭉친다. 2줄, 두껍게
        ell(c, 32, cy + 0.2 + k * 3.6, 3.8, 1.5, BELLY_D)

    # 4. 다리
    for lx in (27.8, 36.2):
        for dd, col in ((c, SC_DARK), (m, 255)):
            ell(dd, lx, cy + 9.0, 3.0, 2.6, col)
        for k in (-1.5, 1.5):                     # 발톱은 2개까지만. 3개는 한 덩어리가 된다
            ell(c, lx + k, cy + 10.2, 1.35, 1.35, HORN)

    # 5. 머리 — 몸보다 크게. 작은 스프라이트는 머리가 커야 읽힌다
    ell(c, 32, hy + 6.5, 6.2, 4.0, SC_DARK)       # 목 그늘 — 없으면 머리와 몸이 한 덩어리로 뭉친다
    for dd, col in ((c, SC_MID), (m, 255)):
        ell(dd, 32, hy, 8.2, 7.6, col)
    ell(c, 30.8, hy - 1.8, 6.0, 4.8, SC_LIT)
    for dd, col in ((c, SC_LIT), (m, 255)):       # 주둥이
        ell(dd, 32, hy + 5.4, 4.8, 3.4, col)
    ell(c, 32, hy + 6.0, 3.6, 2.2, BELLY)
    ell(c, 32, hy + 6.6, 2.2, 1.4, FIRE)          # 입 안쪽 불빛 — 불을 뿜는 놈이라는 표시

    # 6. 뿔 — 귀지느러미는 뺐다. 이 크기에서는 뿔과 붙어 한 덩어리로 보인다
    for side in (-1, 1):
        base = (32 + side * 4.6, hy - 5.8)
        tip = (32 + side * 8.4, hy - 12.0)
        for dd, col in ((c, HORN), (m, 255)):
            dd.polygon(P((base[0] - side * 2.4, base[1] + 1.6), (base[0] + side * 2.4, base[1]), tip), fill=col)
        c.line(P((base[0], base[1] + 0.8), tip), fill=HORN_D, width=int(S * 0.9))

    # 7. 눈 — 마지막. 여기가 가장 밝아야 시선이 얼굴로 간다
    for ex in (28.4, 35.6):
        ell(c, ex, hy - 0.8, 2.6, 2.8, OUT)
        ell(c, ex, hy - 1.0, 1.7, 1.9, EYE)
        ell(c, ex - 0.45, hy - 1.7, 0.8, 0.85, SC_HI)

    return rgb, msk


def quantize(rgb, msk, box, nw, nh, ox, oy):
    """작업 캔버스를 논리 L 로 내리고 알파를 128 에서 자른 뒤 1px 외곽선을 두른다."""
    cr = Image.new('RGB', (W, W), OUT); cm = Image.new('L', (W, W), 0)
    cr.paste(rgb.crop(box).resize((nw, nh), Image.LANCZOS), (ox, oy))
    cm.paste(msk.crop(box).resize((nw, nh), Image.LANCZOS), (ox, oy))

    px = cr.resize((L, L), Image.BOX).load()
    ap = cm.resize((L, L), Image.BOX).point(lambda v: 255 if v >= 128 else 0).load()

    out = Image.new('RGBA', (L, L), (0, 0, 0, 0)); op = out.load()
    for y in range(L):
        for x in range(L):
            if ap[x, y]:
                op[x, y] = px[x, y] + (255,)
    for y in range(L):
        for x in range(L):
            if not ap[x, y] and any(0 <= x + dx < L and 0 <= y + dy < L and ap[x + dx, y + dy]
                                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                op[x, y] = OUT + (255,)
    return out


# ── 16프레임 렌더 → 공통 bbox 로 한 번만 축소해 여백을 보장한다 ──────────────
frames = [draw_frame(i) for i in range(16)]
boxes = [f[1].getbbox() for f in frames]
x0 = min(b[0] for b in boxes); y0 = min(b[1] for b in boxes)
x1 = max(b[2] for b in boxes); y1 = max(b[3] for b in boxes)
UNION = (x0, y0, x1, y1)
uw, uh = x1 - x0, y1 - y0
# 🔴 드래곤은 셀 안에서 적보다 작다 (적 ≈ 50논리px, 드래곤 ≈ 34).
# 소환수가 오거만 하면 화면을 가린다. 코드에서 축소하지 말고 그림이 이미 작다.
# 덤으로 여백이 15논리px(=256 기준 60px) 나와서 B1 외곽선 번짐이 아예 닿지 않는다.
TARGET_H = 34
k = min(TARGET_H * S / uh, (L - 2 * MARGIN) * S / uw)
nw, nh = max(1, int(uw * k)), max(1, int(uh * k))
ox, oy = (W - nw) // 2, (W - nh) // 2    # 나는 놈이라 세로도 가운데. 흔들림은 프레임 안에 남는다

sheet = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0))
for i, (rgb, msk) in enumerate(frames):
    r, cc = divmod(i, 4)
    sheet.paste(quantize(rgb, msk, UNION, nw, nh, ox, oy).resize((CELL, CELL), Image.NEAREST),
                (cc * CELL, r * CELL))
sheet.save('_Incoming/Summons/Dragon_Fly.png')
print('sheet saved. union bbox', UNION, 'scale %.3f' % k)

# ── 아이콘 — 시트와 같은 그림을 그대로 키운다 ────────────────────────────────
# 🔑 아이콘을 따로 그리지 않는다. 같은 코드·같은 팔레트라 아이콘과 실물이 어긋날 수가 없다.
# 프레임 10 = 날개가 가장 넓게 펴진 자세. 아이콘은 실루엣이 넓을수록 알아보기 쉽다.
IM = 3
ik = min((L - 2 * IM) * S / uh, (L - 2 * IM) * S / uw)
inw, inh = int(uw * ik), int(uh * ik)
icon = quantize(*frames[10], UNION, inw, inh, (W - inw) // 2, (W - inh) // 2)
icon.resize((1024, 1024), Image.NEAREST).save('_Incoming/ICON/Dragon.png')
print('icon saved. scale %.3f' % ik)
