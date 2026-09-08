# -*- coding: utf-8 -*-
"""
순간이동 — Assets/Game/Audio/SFX_Blink.wav  (SfxId 28)

무엇인가
    D113 의 텔레포트 패시브. 5~9초에 한 번 **자동으로** 난다.
    지금은 `Magnet`(23)을 빌려 쓰는데 그건 **빨려 들어오는** 소리다 —
    순간이동은 반대로 **빠져나가는** 것이라 뜻이 거꾸로 들린다.

🔑 이 소리의 정체는 "두 번 난다"는 것이다
    사라졌다가 나타난다. 그래서 **흡입(짧게) → 정적 → 출현(짧게)** 세 마디로 만든다.
    목록에 **두 마디로 된 소리가 하나도 없다** — 숫자로는 안 잡히지만 귀로는 이게 제일 잘 갈린다.
    (`compare_sfx.py` 는 리듬을 못 재므로 대역·길이도 따로 챙긴다)

가까이 있는 것들과 어떻게 갈리나
    · `WeaponSwing`(4) 0.280s / 3k~9k 83% — **길이가 거의 같다.** 대역을 아래로 벌린다.
    · `GoldPickup`(27) 0.200s / atk 4ms — 짧고 빠른 자리의 주인. 여기는 **0~800Hz 에 몸**을 준다
      (금화는 그 대역이 **0%** 다).
    · `Magnet`(23) 0.685s / atk 242ms — 빌려 쓰던 소리. 길이·어택이 극단으로 다르다.

"공간이 접힌다"로 들리게 하는 것
    1. **흡입은 올라가고 출현은 내려온다.** 같은 방향으로 두 번 하면 그냥 두 번 친 소리다.
    2. 사이의 **정적 40ms** — 여기가 "없어져 있던 시간"이다. 붙이면 한 마디가 된다.
    3. 출현에만 **저역 퍽** 을 준다. 도착에는 무게가 있어야 한다.

실행:  python Tools/Audio/gen_blink.py
"""

import math
import os

import numpy as np

from dsp import (FS, biquad_bandpass, biquad_highpass, report, run_biquad,
                 smooth, sweep_bandpass, write_wav)

DUR = 0.26

# ① 흡입 — 위로 빨려 올라간다
IN_AT = 0.000
IN_LEN = 0.070
IN_F0, IN_F1 = 700.0, 4200.0
IN_Q = 1.4
# 🔴 흡입이 **더 커야** 한다. 출현이 피크면 어택이 114ms 로 잡혀
#    `TentacleLash`(0.280s / 100ms) 와 거리 0.99 까지 붙는다.
#    사라지는 순간이 날카로운 게 뜻으로도 맞다 — 도착은 도착음이지 타격이 아니다.
IN_GAIN = 1.85

# ② 정적 — 🔑 "없어져 있던 시간". 이걸 없애면 두 마디가 한 마디로 붙는다
GAP = 0.040

# ③ 출현 — 아래로 떨어지며 나타난다
OUT_AT = IN_LEN + GAP                    # 0.110s
OUT_F0, OUT_F1 = 5200.0, 900.0
OUT_Q = 1.2
OUT_DEC = 0.055
OUT_GAIN = 0.85

# ④ 도착의 무게 — 출현에만 붙는 저역. 🔴 GoldPickup 과 가르는 손잡이다(금화는 0~800 이 0%)
THUD_F0, THUD_F1 = 260.0, 130.0
THUD_DEC = 0.045
# 🔴 진폭이 아니라 **스윕 두 층 대비 rms 비율**이다.
#    0.055 를 진폭으로 넣었더니 0~800Hz 가 **2%** 로 파묻혀 GoldPickup 과 가르는 손잡이가 사라졌다.
THUD_RMS_RATIO = 0.55

HPF = 90.0
TARGET_PEAK = 0.40

OUT = os.path.join("Assets", "Game", "Audio", "SFX_Blink.wav")


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260908)
    noise = rng.uniform(-1.0, 1.0, n)

    # ① 흡입 — 짧게 올라갔다 끊긴다
    k_in = np.clip((t - IN_AT) / IN_LEN, 0.0, 1.0)
    f_in = smooth(IN_F0 + (IN_F1 - IN_F0) * (k_in ** 1.4), 2.0)
    env_in = np.where(t < IN_AT + IN_LEN,
                      np.sin(math.pi * np.clip(k_in, 0, 1)) ** 0.8, 0.0)
    suck = sweep_bandpass(noise, f_in, IN_Q) * env_in * IN_GAIN

    # ③ 출현 — 늦게 시작해 아래로 떨어진다
    late = np.clip(t - OUT_AT, 0.0, None)
    k_out = np.clip(late / 0.090, 0.0, 1.0)
    f_out = smooth(OUT_F0 + (OUT_F1 - OUT_F0) * (k_out ** 0.7), 2.0)
    env_out = np.exp(-late / OUT_DEC) * (t >= OUT_AT)
    pop = sweep_bandpass(noise, f_out, OUT_Q) * env_out * OUT_GAIN

    # ④ 도착의 무게
    tf = THUD_F0 + (THUD_F1 - THUD_F0) * k_out
    thud = (np.sin(2.0 * math.pi * np.cumsum(tf) / FS)
            * np.exp(-late / THUD_DEC) * (t >= OUT_AT))
    air = suck + pop
    thud *= (np.sqrt((air ** 2).mean()) / max(1e-9, np.sqrt((thud ** 2).mean()))) * THUD_RMS_RATIO

    mono = air + thud
    mono = run_biquad(mono, biquad_highpass(HPF))
    mono *= TARGET_PEAK / max(1e-9, np.abs(mono).max())

    stereo = np.stack([mono, mono * 0.96], axis=1)
    write_wav(OUT, stereo)
    report(OUT, stereo)


if __name__ == "__main__":
    main()
