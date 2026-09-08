# -*- coding: utf-8 -*-
"""
보호막이 다 찼다 — Assets/Game/Audio/SFX_ShieldUp.wav  (SfxId 29)

무엇인가
    D113 의 보호막 패시브. 6~14초마다 **막이 다시 선다.**
    지금은 `BuffPickup`(26)을 빌려 쓰는데 그건 **먹는** 소리다 —
    보호막은 아무것도 안 먹었는데 픽업 소리가 나면 바닥을 쳐다보게 된다.

🔑 픽업이 아니라 **상태**다 — 그래서 "얻는 소리"가 아니라 "서는 소리"여야 한다
    · 픽업 소리들은 전부 **올라간다**(LevelUp·Magnet·BuffPickup 셋 다 어택 230ms 대의 상승형).
    · 이 소리는 **딩 하고 울린 뒤 남는다.** 어택이 짧고 꼬리가 길다 — 방향이 반대다.

가까이 있는 것들과 어떻게 갈리나
    · `BuffPickup`(26) 0.500s / atk 235ms / 800~3k 60% + 3k~9k 39%
      — 대역이 가장 닮았다. **어택으로 가른다**(235ms vs ~35ms). 하나는 피어오르고 하나는 친다.
    · `GoldPickup`(27) 0.200s / atk 4ms — 짧은 금속. **길이가 2배**다(0.42s).
    · `Heal`(24) 0.914s / 3k~9k 61% — 길이가 절반 이하다.
    · `UiSelect`(40) 0.133s — 아주 짧다. 겹칠 일이 없다.

"유리 막이 선다"로 들리게 하는 것
    1. **완전5도 두 음**(A6 1760 / E7 2637). 3화음은 "성공"이고 5도는 "구조물"이다.
    2. 🔴 **비조화 부분음**을 얹는다(1.00 / 2.76 / 5.40). 정수배로 두면 실로폰이 된다 —
       유리·크리스털은 부분음이 어긋나 있어서 유리로 들린다.
    3. 뒤에 **얇은 쉬머**(6k 위)가 남는다. 막이 "떠 있는" 느낌은 꼬리에서 나온다.

실행:  python Tools/Audio/gen_shield_up.py
"""

import math
import os

import numpy as np

from dsp import (FS, biquad_bandpass, biquad_highpass, report, run_biquad,
                 write_wav)

# 🔴 **대역만 옮기다 두더지잡기가 됐다** — 0.42s 로는 어디로 가도 이웃이 있었다:
#    A6/E7 이면 `WeaponCast`(0.65), 한 옥타브 내리면 `XpPickup`(0.64).
#    ⇒ 축을 바꿨다. **길이가 비어 있는 자리**로 간다 —
#    0.70~0.85s 사이에는 `Magnet`(0.685) 과 `BombPickup`(0.850) 뿐이고
#    둘 다 대역이 완전히 다르다(43% 저중역 / 65% 저역).
DUR = 0.78

# ① 두 음 — 완전5도. 뒤 음이 살짝 늦게 들어온다
# 🔴 처음엔 A6/E7(1760/2637) 이었는데 `WeaponCast`(2) 와 거리 **0.65** — 목록 최악이 됐다.
#    (0.524s/13ms/0-0-12-64 vs 0.420s/24ms/0-0-20-50 — 길이·어택·대역이 셋 다 붙었다)
#    ⇒ 한 옥타브 내려 기음을 800~3k 로 옮겼다. 마법 시전음은 위쪽에 있고 이건 아래로 내려온다.
NOTES = ((880.0,  0.000, 1.00),          # A5
         (1319.0, 0.045, 0.85))          # E6
# 🔑 비조화 부분음 — 여기가 "유리"의 전부다. 정수배(1,2,3)로 두면 실로폰이 된다
PARTIALS = (1.00, 2.76, 5.40)
PARTIAL_AMP = (1.00, 0.42, 0.18)
NOTE_DEC0 = 0.280                        # 기음의 감쇠 — 길게 남는다 (막이 서서 유지된다)
NOTE_DEC_FALL = 0.62                     # 윗부분음일수록 빨리 죽는다 (실제 유리)
# 🔴 픽업 무리(230ms 대)와 빠른 무리(4~20ms) 사이의 **빈 자리**를 쓴다.
#    너무 빠르면 XpPickup·GoldPickup 쪽으로, 너무 느리면 BuffPickup 쪽으로 붙는다.
NOTE_ATK = 0.090

# ② 쉬머 — 막이 떠 있는 층
SHIM_LO, SHIM_HI = 6000.0, 12000.0
SHIM_AT = 0.020
SHIM_DEC = 0.300
SHIM_GAIN = 0.55         # 🔴 위쪽을 줄인다 — WeaponCast 가 3k~9k 64% 다

HPF = 260.0                              # 🔴 저역은 비우되, 기음이 살 만큼만 (폭발 자리를 안 먹는다)
TARGET_PEAK = 0.38

OUT = os.path.join("Assets", "Game", "Audio", "SFX_ShieldUp.wav")


def bell(t, f0, at, amp):
    """비조화 부분음을 가진 한 음. 부분음마다 다른 속도로 죽는다."""
    late = np.clip(t - at, 0.0, None)
    gate = (t >= at).astype(float)
    # 어택을 아주 짧게 — 딱 하고 서야 "상태"로 들린다
    atk = np.clip(late / NOTE_ATK, 0.0, 1.0)
    out = np.zeros(len(t))
    for i, (r, a) in enumerate(zip(PARTIALS, PARTIAL_AMP)):
        dec = NOTE_DEC0 * (NOTE_DEC_FALL ** i)
        out += a * np.sin(2.0 * math.pi * f0 * r * late) * np.exp(-late / dec)
    return out * gate * atk * amp


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260908)
    noise = rng.uniform(-1.0, 1.0, n)

    mono = np.zeros(n)
    for f0, at, amp in NOTES:
        mono += bell(t, f0, at, amp)

    # 🔴 사인 합은 잡음보다 에너지가 훨씬 크다 (gen_mic_pulse 의 교훈).
    #    쉬머는 rms 를 맞춰 얹는다 — 눈대중으로 두면 종소리에 파묻힌다.
    c = math.sqrt(SHIM_LO * SHIM_HI)
    sh = run_biquad(noise, biquad_bandpass(c, c / (SHIM_HI - SHIM_LO)))
    late = np.clip(t - SHIM_AT, 0.0, None)
    sh = sh * np.exp(-late / SHIM_DEC) * (t >= SHIM_AT)
    sh *= (np.sqrt((mono ** 2).mean()) / max(1e-9, np.sqrt((sh ** 2).mean()))) * SHIM_GAIN
    mono += sh

    mono = run_biquad(mono, biquad_highpass(HPF))
    mono *= TARGET_PEAK / max(1e-9, np.abs(mono).max())

    # 막은 플레이어를 둘러싼다 — 조금 넓게
    stereo = np.stack([mono, np.concatenate([[0.0] * 12, mono[:-12]]) * 0.92], axis=1)
    write_wav(OUT, stereo)
    report(OUT, stereo)


if __name__ == "__main__":
    main()
