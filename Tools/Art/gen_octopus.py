# 문어 소환수 — 대기 시트 + 촉수 공격 이펙트 + 아이콘 (C14)
#
# 두 가지를 뽑는다. 규격이 서로 다르니 주의:
#
#  1) _Incoming/Summons/Octopus_Idle.png   1024x1024 · 4x4 · PPU 512 · Point
#     → 드래곤과 같은 규격. 논리 64 격자의 도트로 굽는다 (적 시트와 결을 맞춘다)
#
#  2) _Incoming/Effects/TentacleLash.png   1536x256 · 가로 6프레임 · PPU 100
#     → SwingArc / ToxinField 와 **완전히 같은 반지름 계약**이다.
#       바깥 반지름 = scale 1 에서 1.0 유닛. localScale = Range 로 두면 그림이 곧 판정이다.
#       이펙트는 도트로 굽지 않는다 — 기존 두 이펙트가 부드러운 그림이라 결을 맞춘다
#
#  3) _Incoming/ICON/Octopus.png           1024x1024
#     → 대기 시트와 같은 코드가 뽑는다 (드래곤과 같은 방식)

import math
from PIL import Image, ImageDraw

S = 16
L = 64
W = L * S
CELL = 256
MARGIN = 9

# ── 팔레트 ────────────────────────────────────────────────────────────────
# 드래곤은 하늘색(hue≈200)이다. 문어는 보라(hue≈285) — 소환수끼리도 갈라야 한다.
# 보라는 적·플레이어·건물 어디에도 안 쓰인 유일하게 빈 색상대다.
OUT     = (16, 18, 30)
SK_DARK = (72, 40, 116)
SK_MID  = (118, 72, 174)
SK_LIT  = (162, 116, 216)
SK_HI   = (206, 176, 242)
SUCK    = (250, 206, 224)
SUCK_D  = (206, 148, 178)
EYE_W   = (246, 244, 252)
EYE_K   = (24, 18, 40)
GLOW    = (226, 150, 255)


def P(*pts):
    return [(x * S, y * S) for x, y in pts]


def ell(d, cx, cy, rx, ry, fill, s=S):
    d.ellipse([(cx - rx) * s, (cy - ry) * s, (cx + rx) * s, (cy + ry) * s], fill=fill)


# ── 1. 대기 시트 ──────────────────────────────────────────────────────────
N_ARM = 6
ARM_X = [-8.4, -5.2, -1.8, 1.8, 5.2, 8.4]     # 촉수 뿌리 x (몸 중앙 기준)


def draw_idle(i, n=16):
    t = i / n
    bob = -math.sin(2 * math.pi * t) * 1.5

    rgb = Image.new('RGB', (W, W), OUT)
    msk = Image.new('L', (W, W), 0)
    c, m = ImageDraw.Draw(rgb), ImageDraw.Draw(msk)

    cy = 26 + bob

    # 촉수 — 뿌리부터 끝까지 원을 이어 그린다. 위상을 촉수마다 어긋나게 줘야 물결이 된다.
    # 흔들림을 뿌리에서 0, 끝에서 최대로 키운다. 뿌리까지 흔들면 몸에서 떨어져 보인다 (드래곤 꼬리와 같은 실수)
    for a, bx in enumerate(ARM_X):
        ph = 2 * math.pi * t + a * (2 * math.pi / N_ARM)
        outer = abs(bx) / 8.4                       # 바깥 촉수일수록 더 벌어진다
        for k in range(13):
            u = k / 12.0
            sway = math.sin(ph + u * 2.4) * 3.2 * u
            x = 32 + bx * (1 + 0.55 * u) + sway + outer * u * 3.0 * (1 if bx > 0 else -1)
            y = cy + 5.5 + u * 18.0                 # 팔이 몸보다 길어야 문어로 읽힌다
            r = 2.7 - u * 2.0
            for dd, col in ((c, SK_DARK if a in (0, 5) else SK_MID), (m, 255)):
                ell(dd, x, y, r, r, col)
            # 빨판 — 앞쪽 촉수에만. 촉수마다 높이를 어긋나게 준다.
            # 같은 k 로 맞추면 배를 가로지르는 점선 한 줄로 보인다 (실제로 그렇게 나왔다)
            if a in (1, 2, 3, 4) and k in (3 + a % 3, 8 + a % 3):
                ell(c, x, y, 0.85, 0.85, SUCK)

    # 몸(맨틀) — 위가 둥근 돔, 아래가 넓다
    for dd, col in ((c, SK_MID), (m, 255)):
        ell(dd, 32, cy, 10.4, 9.6, col)
        ell(dd, 32, cy + 4.5, 9.4, 6.0, col)
    ell(c, 30.2, cy - 3.2, 7.2, 5.4, SK_LIT)        # 위쪽 하이라이트
    ell(c, 29.0, cy - 5.0, 3.0, 2.0, SK_HI)
    for k, (sx, sy, sr) in enumerate([(24.5, cy + 2.5, 1.5), (39.5, cy + 3.2, 1.3),
                                      (32.0, cy + 7.0, 1.4)]):
        ell(c, sx, sy, sr, sr * 0.85, SK_DARK)      # 표면 반점 — 밋밋함을 깬다

    # 눈 — 문어의 정체성이다. 크고 낮게 붙는다
    for ex, ey in ((27.4, cy + 1.4), (36.6, cy + 1.4)):
        ell(c, ex, ey, 3.5, 3.7, OUT)
        ell(c, ex, ey, 2.6, 2.8, EYE_W)
        ell(c, ex, ey + 0.3, 1.25, 1.5, EYE_K)      # 가로로 긴 문어 동공 대신 세로 — 이 크기에선 이게 더 읽힌다
        ell(c, ex - 0.7, ey - 1.1, 0.7, 0.7, EYE_W)

    return rgb, msk


def quantize(rgb, msk, box, nw, nh, ox, oy):
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


frames = [draw_idle(i) for i in range(16)]
boxes = [f[1].getbbox() for f in frames]
x0 = min(b[0] for b in boxes); y0 = min(b[1] for b in boxes)
x1 = max(b[2] for b in boxes); y1 = max(b[3] for b in boxes)
UNION = (x0, y0, x1, y1)
uw, uh = x1 - x0, y1 - y0

# 드래곤과 같은 34논리px. 소환수 둘이 같은 체급으로 보여야 한다 (색과 형태로만 갈린다)
TARGET_H = 34
k = min(TARGET_H * S / uh, (L - 2 * MARGIN) * S / uw)
nw, nh = max(1, int(uw * k)), max(1, int(uh * k))
ox, oy = (W - nw) // 2, (W - nh) // 2

sheet = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0))
for i, (rgb, msk) in enumerate(frames):
    r, cc = divmod(i, 4)
    sheet.paste(quantize(rgb, msk, UNION, nw, nh, ox, oy).resize((CELL, CELL), Image.NEAREST),
                (cc * CELL, r * CELL))
sheet.save('_Incoming/Summons/Octopus_Idle.png')
print('idle sheet saved. scale %.3f' % k)

IM = 3
ik = min((L - 2 * IM) * S / uh, (L - 2 * IM) * S / uw)
inw, inh = int(uw * ik), int(uh * ik)
icon = quantize(*frames[4], UNION, inw, inh, (W - inw) // 2, (W - inh) // 2)
icon.resize((1024, 1024), Image.NEAREST).save('_Incoming/ICON/Octopus.png')
print('icon saved. scale %.3f' % ik)


# ── 2. 촉수 공격 이펙트 ───────────────────────────────────────────────────
# 🔴 반지름 계약: 알파의 바깥 끝이 중심에서 정확히 100px 이어야 한다 (PPU 100 → 1.0 유닛).
#    SwingArc 는 이 계약을 107.4px 로 어겼고, DEV 가 나눗셈으로 보정해야 했다.
#    여기서는 아예 100 에 맞춰 놓고 아래에서 실측으로 확인한다.
FXC = 256
FXS = 4                      # 이펙트는 도트로 굽지 않는다 — 부드럽게 (SwingArc/ToxinField 와 같은 결)
R_MAX = 99.0
N_TENT = 6

REACH = [44, 72, 93, 100, 96, 80]        # 프레임별 바깥 도달 반지름
BASE_W = [4.2, 3.8, 3.5, 3.3, 2.7, 1.9]  # 뿌리 굵기 (C19 에서 절반으로 줄였다 — 아래 참조)
ALPHA = [170, 225, 255, 255, 205, 120]   # 마지막 두 장은 잔상이라 옅다
R_IN = 20.0                              # 🔴 촉수가 시작하는 안쪽 반지름 = 가운데 구멍

# ── C19: 왜 다시 그렸나 — "소환수가 자기 이펙트에 파묻힌다" (D13 판정 ⑥-3) ──────
# 처음엔 반경 2.5 가 너무 큰 줄 알았다. 재 보니 **크기가 아니라 가운데가 문제**였다.
#
#   · 잉크 밀도는 검 호와 거의 같다 — 촉수 13.7% vs 검 호 13.6% (원반 대비).
#     즉 "너무 빽빽해서" 가 아니다.
#   · 진짜 범인은 **가운데 빛 덩어리**였다. 반경 2.5 배율(2.512)에서 이 원반은
#     **0.384 유닛**으로 렌더된다. 문어 몸통 전체가 **0.281 유닛**이다 —
#     🔴 이펙트의 중심 장식 하나가 문어보다 **2.7배 크다.** 몸통이 그 밑에 깔린다.
#   · 촉수 굵기도 뿌리에서 0.14 유닛이었다 = **문어 폭의 절반**. 팔 하나가 몸통 반쪽이다.
#
# 그래서 반경은 **손대지 않았다.** D12 가 반대 방향을 증명했기 때문이다 —
# 최근접 적이 4.67 유닛에 있고 검(사거리 2.5)은 6초간 0회 휘둘렀다.
# **이 게임에서 2.5 유닛은 크지 않다.** D13 도 반경 2.5 에서 후리기 14회에 44피격을
# 실측했다. 줄이면 문어의 정체성("적까지 일정범위 공격")이 사라진다.
#
# 고친 것은 셋뿐이고 전부 **그림 안에서** 끝난다:
#   ① 가운데 빛 원반 제거 → 문어가 보일 구멍을 판다
#   ② 촉수를 R_IN=20px 부터 시작 → 몸통(배율 2.5 기준 약 5.6px 상당) 주위가 비워진다
#   ③ 뿌리 굵기 절반 → 젖은 채찍처럼 얇게. 밀도 13.7% → 아래 실측 참조
#
# 🔴 바깥 반지름 100px 계약은 그대로다. 따라서 DEV 는 아무것도 안 고쳐도 된다 —
#    `spriteRadiusAtScaleOne 0.9951` · `lashHitDelay 0.1` · `frameRate 30` 유지.


def draw_lash(i):
    big = FXC * FXS
    im = Image.new('RGBA', (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = cy = big / 2
    reach, bw, al = REACH[i], BASE_W[i], ALPHA[i]
    spin = i * 9.0                        # 프레임마다 조금씩 돌아 휘도는 느낌을 만든다

    for a in range(N_TENT):
        base = math.radians(spin + a * (360.0 / N_TENT))
        curl = math.radians(34)           # 끝으로 갈수록 휘어지는 각
        # 🔴 촉수는 원을 촘촘히 이어 그린다. 19개로는 끝이 구슬 목걸이처럼 끊긴다 —
        #    바깥으로 갈수록 원이 얇아지는데 간격은 그대로라 벌어진다. 64개면 이어진다
        for k in range(65):
            u = k / 64.0
            ang = base + curl * u * u
            rr = R_IN + (reach - R_IN) * u
            w = bw * (1.0 - 0.82 * u)
            rr = min(rr, R_MAX - w)       # 🔴 어떤 픽셀도 100 을 넘지 않게 잘라 둔다
            x = cx + math.cos(ang) * rr * FXS
            y = cy - math.sin(ang) * rr * FXS
            shade = SK_LIT if u > 0.55 else SK_MID
            d.ellipse([x - w * FXS, y - w * FXS, x + w * FXS, y + w * FXS], fill=shade + (al,))
            if k % 11 == 3 and u < 0.8:   # 빨판
                d.ellipse([x - w * 0.42 * FXS, y - w * 0.42 * FXS,
                           x + w * 0.42 * FXS, y + w * 0.42 * FXS], fill=SUCK + (al,))

    # ❌ 중심의 빛 원반은 지웠다. "촉수가 어디서 나오는지" 를 알려 주려던 장식인데,
    #    정작 그 자리에 **문어 본체가 서 있다.** 가리키려던 대상을 자기가 덮고 있었다.
    #    대신 아주 얇은 테두리 링만 남긴다 — 구멍의 가장자리를 보여 주되 속은 비운다.
    ring_r = R_IN * (0.72 + 0.05 * i)
    d.ellipse([cx - ring_r * FXS, cy - ring_r * FXS, cx + ring_r * FXS, cy + ring_r * FXS],
              outline=GLOW + (int(al * 0.45),), width=int(1.1 * FXS))
    return im.resize((FXC, FXC), Image.LANCZOS)


fx = Image.new('RGBA', (FXC * 6, FXC), (0, 0, 0, 0))
for i in range(6):
    fx.paste(draw_lash(i), (i * FXC, 0))
fx.save('_Incoming/Effects/TentacleLash.png')

# 실측 — 계약이 지켜졌는지 여기서 바로 확인한다
import numpy as np
arr = np.array(fx.split()[3])
worst = 0.0
for i in range(6):
    cell = arr[:, i * FXC:(i + 1) * FXC]
    ys, xs = np.nonzero(cell > 8)
    r = np.hypot(xs + 0.5 - 128, ys + 0.5 - 128)
    rr = r.max()
    worst = max(worst, rr)
    ink = cell.astype(float).sum() / 255.0
    # 🔴 C19 의 핵심 지표 — 가운데가 비었는가. 문어는 배율 2.5 에서 약 5.6px 상당이다
    print('  lash f%d 바깥 %6.2fpx  안쪽구멍 %5.2fpx  잉크 %.1f%%  알파최대 %d'
          % (i, rr, r.min(), 100.0 * ink / (math.pi * rr * rr), cell.max()))
print('=> 최대 반지름 %.2f px  =  %.4f 유닛 (PPU 100)' % (worst, worst / 100.0))
# cp949 콘솔에서 em dash 가 죽는다. 출력문에는 ASCII 기호만 쓴다 (gen_toxin_splash 와 같은 함정)
print('   문어 몸통은 배율 2.512 기준 약 5.6px 상당. 안쪽구멍이 이보다 커야 안 파묻힌다')
