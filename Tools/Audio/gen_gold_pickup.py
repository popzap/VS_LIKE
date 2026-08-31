# -*- coding: utf-8 -*-
"""
돈 먹는 소리 — _Incoming/Audio/SFX_GoldPickup.wav  (SfxId 27 예정)

무엇인가
    골드 픽업(15G, C23)을 먹었을 때. 드랍 2.5% = **런당 13번쯤** — 6종 중 가장 자주 난다.
    ⚠️ 자주 나는 소리는 **짧아야 한다.** 0.20s 로 잡은 이유가 그것이다.

🔴 요청-12 의 조건: "`XpPickup` 과 구별될 것"
    경험치는 계속 먹는다. 돈과 경험치가 비슷하면 **둘 다 의미 없는 소음**이 된다.

    | | 이 소리 | `XpPickup` (20) | 어떻게 갈랐나 |
    |---|---|---|---|
    | 길이 | **0.20s** | 0.486s(실측) | 2.4배 |
    | 소리의 정체 | 🔑 **금속 3번 부딪힘** | 한 방의 블립 | 개수부터 다르다 |
    | 800~3k | **7%** | **78%**(실측) | 경험치는 이 대역이 전부다 |
    | 3k~9k | **62%** | 22%(실측) | 돈은 **위**에 있다 |

    🔑 가장 크게 가르는 건 대역이 아니라 **부딪힘이 세 번**이라는 사실이다.
    한 방짜리 소리와 또르륵 하는 소리는 스펙트럼이 같아도 절대 안 헷갈린다.

"동전"으로 들리게 하는 것 — 배음이 정수배면 안 된다
    사인의 정수배(1:2:3)는 **악기 소리**다. 금속판은 그렇게 안 운다.
    두들긴 금속의 모드비(1.00 / 2.30 / 3.64 / 5.41 / 7.20)를 쓴다 — 이게 "쨍" 이다.
    ⚠️ 여기를 정수배로 바꾸면 즉시 실로폰이 되고 `BuffPickup` 과 헷갈린다.

    1. **높은 모드일수록 빨리 죽는다** — 실제 금속이 그렇다. 안 그러면 신디사이저 종소리다
    2. **때리는 잡음 1ms** — 부딪히는 순간의 마찰. 없으면 소리가 그냥 "생겨난다"
    3. **음이 조금씩 올라간다** (2100 -> 2500 -> 2900Hz) — 돈은 받는 것이므로 상승이 맞다

`UiSelect` (0.133s / 3k 위가 99%) 와도 겹치지 않게
    ⚠️ 기음을 3k 위로 올린 대가로 **이쪽과 가까워졌다** (거리 1.09 — 새 3종 중 가장 가깝다).
    버티는 축은 **길이 1.5배**와 **어택**(0.5ms vs 14ms)이다.
    그리고 그쪽은 잡음 클릭이라 음정이 없고, 이쪽은 **부딪힘이 세 번**이다.

실행:  python Tools/Audio/gen_gold_pickup.py
"""

import math
import os

import numpy as np

from dsp import (FS, band_share, biquad_bandpass, biquad_highpass, report,
                 run_biquad, write_wav)

DUR = 0.20

# ① 부딪힘 — 3번. 🔑 "개수"가 XpPickup 과 가르는 첫 번째 축이다
CLINK_AT = (0.000, 0.035, 0.075)
# 🔴 2300/2750/3200 에서는 `DragonSpit`(7) 과 거리 **0.86** 이었다.
#    드래곤은 0.220s · 800~3k 52% 라 길이도 대역도 붙는다. 드래곤도 쿨마다 계속 뱉는 무기다.
#    ⇒ 기음을 통째로 올려 3k~9k 로 옮겼다. 작은 동전은 원래 이 높이에서 운다.
#    ⚠️ 더 올리면 이번엔 `UiSelect`(40, 3k 위가 99%) 로 붙는다. 위아래로 낀 자리다.
CLINK_F0 = (3000.0, 3600.0, 4200.0)     # 조금씩 올라간다 = 받는 소리
# ⚠️ 0.82 로는 2번째가 1번째보다 **크게** 나왔다 — 1번째의 꼬리가 겹쳐 더해지기 때문이다.
#    겹침까지 감안해 더 낮춘다. 뒤가 커지면 굴러 멎는 게 아니라 다가오는 소리가 된다.
CLINK_AMP = (1.00, 0.70, 0.52)

# 🔑 두들긴 금속의 모드비. 정수배가 아니다 — 여기가 "쨍"의 전부다
MODES = (1.00, 2.30, 3.64, 5.41, 7.20)
# ⚠️ 처음엔 (1.00,0.62,0.40,0.24,0.14)/DEC_FALL 0.62 였는데 대역이 **75.7/20.5** 로 나왔다 —
#    `XpPickup` 78/22 와 거의 같다. 기음 하나가 에너지를 70% 먹어서 "블립"이 된 것이다.
#    윗모드를 살려 에너지를 위로 퍼뜨린다. 이게 블립과 금속을 가르는 실제 손잡이다.
MODE_AMP = (1.00, 0.90, 0.75, 0.58, 0.40)
MODE_DEC0 = 0.055                       # 기음의 감쇠
MODE_DEC_FALL = 0.80                    # 🔑 높은 모드일수록 짧다 (실제 금속). 1.0 이면 종소리다

# ② 때리는 잡음 — 부딪히는 순간의 마찰
STRIKE_LO = 3500.0
STRIKE_HI = 11000.0
STRIKE_DEC = 0.0016
STRIKE_GAIN = 0.90

# ③ 몸통 — 동전이 얇지 않게. 아주 조금만
BODY_LO = 420.0
BODY_HI = 900.0
BODY_DEC = 0.035
BODY_GAIN = 0.85

HPF = 300.0             # 폭탄 픽업의 저역과 안 부딪히게
TARGET_PEAK = 0.35
CORR = 0.50

OUT = os.path.join("_Incoming", "Audio", "SFX_GoldPickup.wav")


def clink(t, f0, amp):
    """금속 한 번 부딪힘. 모드마다 다른 속도로 죽는다."""
    out = np.zeros(len(t))
    m = t >= 0.0
    for i, (r, a) in enumerate(zip(MODES, MODE_AMP)):
        dec = MODE_DEC0 * (MODE_DEC_FALL ** i)
        e = np.zeros(len(t))
        e[m] = np.exp(-t[m] / dec)
        out += a * np.sin(2.0 * math.pi * f0 * r * np.clip(t, 0, None)) * e
    return out * amp


def band_noise(noise, lo, hi):
    c = math.sqrt(lo * hi)
    return run_biquad(noise, biquad_bandpass(c, c / (hi - lo)))


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260831)

    cn = rng.normal(0.0, 1.0, n)

    # 모드는 좌우 공통 — 벌리면 음정이 흔들려 "찌그러진 금속"이 된다
    tones = np.zeros(n)
    for at, f0, amp in zip(CLINK_AT, CLINK_F0, CLINK_AMP):
        tones += clink(t - at, f0, amp)

    chans = []
    for _ in range(2):
        nz = CORR * cn + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        strike = band_noise(nz, STRIKE_LO, STRIKE_HI)
        body = band_noise(nz, BODY_LO, BODY_HI)

        hits = np.zeros(n)
        soft = np.zeros(n)
        for at, amp in zip(CLINK_AT, CLINK_AMP):
            lt = t - at
            m = lt >= 0.0
            eh = np.zeros(n)
            eb = np.zeros(n)
            eh[m] = np.exp(-lt[m] / STRIKE_DEC)
            eb[m] = np.exp(-lt[m] / BODY_DEC)
            hits += eh * amp
            soft += eb * amp

        mix = tones + STRIKE_GAIN * strike * hits + BODY_GAIN * body * soft
        chans.append(run_biquad(mix, biquad_highpass(HPF)))

    st = np.stack(chans, axis=1)
    tail = np.clip(np.linspace(1.0, 0.0, n) / 0.05, 0, 1)
    st *= tail[:, None]
    st *= TARGET_PEAK / np.abs(st).max()

    write_wav(OUT, st)
    report(OUT, st)

    # cp949 콘솔이라 이 아래 출력은 ASCII 만 쓴다
    mono = st.mean(axis=1)
    # 부딪힘이 정말 셋으로 들리나 - 창마다 봉우리가 서야 한다
    peaks = [float(np.abs(mono[int(FS * a):int(FS * b)]).max())
             for a, b in ((0.000, 0.030), (0.035, 0.070), (0.075, 0.115))]
    print("  clink peaks %.3f / %.3f / %.3f   [three separate hits, decaying]"
          % tuple(peaks))
    valley = float(np.abs(mono[int(FS * 0.028):int(FS * 0.034)]).max())
    print("  gap before 2nd hit %.3f vs %.3f   [must dip, else it is one smeared blip]"
          % (valley, peaks[1]))
    print("  bands 800-3k %.1f%% / 3k-9k %.1f%%   [XpPickup is 78%% / 22%%]"
          % (band_share(mono, 800, 3000), band_share(mono, 3000, 9000)))


if __name__ == "__main__":
    main()
