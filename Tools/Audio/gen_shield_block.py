# -*- coding: utf-8 -*-
"""
보호막이 막았다 — Assets/Game/Audio/SFX_ShieldBlock.wav  (SfxId 18)

무엇인가
    D113 의 보호막이 **원거리 한 방을 먹은** 순간. 지금은 `UiCancel`(41)을 빌려 쓴다 —
    그건 **UI 소리**다. 전투 한복판에서 메뉴 취소음이 나면 뭘 잘못 눌렀나 싶어진다.

🔴 이 소리는 `PlayerHit`(12)의 **반대말**이다
    같은 순간에 둘 중 하나만 난다 — "맞았다" 아니면 "안 맞았다".
    **둘이 닮으면 플레이어는 체력이 줄었는지 안 줄었는지 귀로 못 안다.**

    | | 길이 | 어택 | 대역 |
    |---|---|---|---|
    | `PlayerHit` (12) | **1.015s** | **194ms** | 800~3k **83%** |
    | 🔑 **이 소리** | **0.18s** | **~2ms** | 위아래로 퍼진다 |

    길이 5.6배 · 어택 100배. 이보다 더 벌릴 수는 없다.

가까이 있는 것들과 어떻게 갈리나
    · `GoldPickup`(27) 0.200s / atk 4ms / 3k~9k 62% — 🔴 **길이도 어택도 거의 같다.**
      금화도 금속이라 부분음 구조까지 닮을 수 있다.
      ⇒ 여기는 **0~800Hz 에 몸을 준다**(금화는 그 대역이 **0%** 다). 이게 유일한 손잡이다.
      막는 것은 **충격**이라 저역이 있는 게 옳기도 하다 — 동전 부딪히는 소리와 다르다.
    · `EnemyHit`(10) 0.484s / 34/32/24/10 — 저역이 닮지만 길이가 2.7배다.

"튕겨 나간다"로 들리게 하는 것
    1. **비조화 금속 모드** (1.00 / 2.41 / 4.07 / 6.15) — 두들긴 판의 모드비.
    2. **아주 빠른 감쇠** — 막은 흔들리지 않는다. 길게 울리면 종이 된다.
    3. **저역 퍽 한 번** — 충격. 이게 금화와 가르는 축이기도 하다.

실행:  python Tools/Audio/gen_shield_block.py
"""

import math
import os

import numpy as np

from dsp import (FS, biquad_bandpass, biquad_highpass, report, run_biquad,
                 write_wav)

# 🔴 0.18 로는 `GoldPickup`(0.200s / atk 4.2ms) 과 거리 **0.70** 이었다 —
#    길이·어택이 사실상 같아서 대역만으로는 못 갈랐다. 더 짧게 끊는다.
DUR = 0.13

# ① 금속 판 — 비조화. 🔑 정수배로 두면 종이 된다
F0 = 1450.0
MODES = (1.00, 2.41, 4.07, 6.15)
MODE_AMP = (1.00, 0.72, 0.46, 0.26)
MODE_DEC0 = 0.038                        # 🔴 짧다. 막은 흔들리지 않는다
MODE_DEC_FALL = 0.72

# ② 충격의 저역 — 🔴 GoldPickup 과 가르는 유일한 손잡이 (금화는 0~800 이 0%)
THUD_F0, THUD_F1 = 190.0, 105.0
THUD_DEC = 0.030
# 🔴 이 값은 "진폭"이 아니라 **금속 층 대비 rms 비율**이다.
#    0.28 을 진폭으로 넣었더니 0~200Hz 가 **0.3%** 로 파묻혀
#    3k~9k 62.4% = `GoldPickup`(62%) 과 **정확히 겹쳤다.** 길이(0.18 vs 0.20)와
#    어택(0.1 vs 4.2ms)까지 닮아 있어서 저역이 유일한 손잡이인데 그게 없었다.
THUD_RMS_RATIO = 1.60    # 🔴 저역이 금화와 가르는 유일한 축이라 확실히 준다

# ③ 부딪히는 잡음 — 어택을 2ms 로 만든다
STRIKE_LO, STRIKE_HI = 2500.0, 9000.0
STRIKE_DEC = 0.0015
STRIKE_GAIN = 0.70       # 🔴 3k~9k 를 줄인다 — 거기가 금화의 몸이다

HPF = 80.0
TARGET_PEAK = 0.44

OUT = os.path.join("Assets", "Game", "Audio", "SFX_ShieldBlock.wav")


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260908)
    noise = rng.uniform(-1.0, 1.0, n)

    # ① 금속 모드
    metal = np.zeros(n)
    for i, (r, a) in enumerate(zip(MODES, MODE_AMP)):
        dec = MODE_DEC0 * (MODE_DEC_FALL ** i)
        metal += a * np.sin(2.0 * math.pi * F0 * r * t) * np.exp(-t / dec)

    # ② 저역 퍽
    tf = THUD_F0 + (THUD_F1 - THUD_F0) * np.clip(t / DUR, 0, 1)
    thud = np.sin(2.0 * math.pi * np.cumsum(tf) / FS) * np.exp(-t / THUD_DEC)
    thud *= (np.sqrt((metal ** 2).mean()) / max(1e-9, np.sqrt((thud ** 2).mean()))) * THUD_RMS_RATIO

    # ③ 때리는 잡음 — rms 를 맞춰 얹는다(눈대중으로 두면 금속에 파묻힌다)
    c = math.sqrt(STRIKE_LO * STRIKE_HI)
    strike = run_biquad(noise, biquad_bandpass(c, c / (STRIKE_HI - STRIKE_LO)))
    strike = strike * np.exp(-t / STRIKE_DEC)
    strike *= (np.sqrt((metal ** 2).mean()) / max(1e-9, np.sqrt((strike ** 2).mean()))) * STRIKE_GAIN

    mono = metal + thud + strike
    mono = run_biquad(mono, biquad_highpass(HPF))
    mono *= TARGET_PEAK / max(1e-9, np.abs(mono).max())

    stereo = np.stack([mono, mono * 0.95], axis=1)
    write_wav(OUT, stereo)
    report(OUT, stereo)


if __name__ == "__main__":
    main()
