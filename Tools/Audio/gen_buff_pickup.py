# -*- coding: utf-8 -*-
"""
버프 픽업 먹는 소리 — _Incoming/Audio/SFX_BuffPickup.wav  (SfxId 26 예정)

무엇인가
    무적(3초) · 공속(8초) 픽업을 먹었을 때. **둘이 같은 소리를 쓴다**(요청-12 명시).
    드랍 1.0% + 0.4% = 런당 7번쯤.

🔴 "상승형"은 이미 만원이다 — 여기가 이 소리의 유일한 어려움이다
    올라가는 소리가 벌써 셋 있고, 셋 다 **길다.** (실측)

    | | 길이 | 어택 | 대역이 몰린 곳 |
    |---|---|---|---|
    | `LevelUp` (21) | 1.021s | 230ms | 200~800Hz **62%** |
    | `Heal` (24) | 0.914s | 132ms | 3k~9k **61%** |
    | `Magnet` (23) | 0.685s | 242ms | 0~800Hz **76%** |
    | 🔑 **이 소리** | **0.50s** | 231ms | 800~3k **60%** + 3k~9k **39%** |

    ⇒ 저 셋은 전부 **저역~중역**에 몸이 있다. 이 소리만 **200Hz 아래가 0%** 다.
    가장 가까운 게 `Heal` 인데 거리 1.03 — 대역(3k~9k 39% vs 61%)과 길이(0.50 vs 0.91)로 버틴다.

    ⚠️ `LevelUp` 과는 특히 안 겹쳐야 한다 — 버프를 먹다가 레벨이 오르면 **동시에 난다.**
    그때 둘이 비슷하면 플레이어는 레벨업 패널이 왜 떴는지 모른다.

저역을 비운 이유 (HPF 420Hz)
    이 픽업은 **폭탄 픽업과 같은 화면에 떨어져 있다.** 둘을 연달아 먹으면 겹친다.
    폭탄은 0~200Hz 가 65% 다. 여기서 저역을 먹으면 폭탄의 타격이 흐려진다.
    `gen_dragon_spit.py` 가 `Explosion` 에게 저역을 넘긴 것과 같은 규칙이다.

"파워업"으로 들리게 하는 것
    1. **3음 상승** C6-G6-C7 (1046.5 / 1568.0 / 2093.0Hz) — 🔑 **완전5도+옥타브**.
       장3화음(도미솔)은 "성공"이지 "강해짐"이 아니다. 5도는 비어 있어서 힘으로 들린다
    2. **겹쳐 울린다** — 앞 음이 안 꺼진 채 다음이 올라온다. 그래야 하나의 상승이지 삐-삐-삐 가 아니다
    3. **쉬머** 1500 -> 7000Hz 노이즈 스윕 — 마법이 걸리는 층. 없으면 그냥 실로폰이다

실행:  python Tools/Audio/gen_buff_pickup.py
"""

import math
import os

import numpy as np

from dsp import (FS, band_share, biquad_highpass, report, run_biquad,
                 sweep_bandpass, write_wav)

DUR = 0.50

# ① 4음 상승 — 🔑 완전5도+옥타브. 장3화음으로 바꾸면 "레벨업"에 가까워진다
#
# 🔴 처음엔 0.30s / 3음이었는데 `TentacleLash`(6) 와 거리 **0.58** 이 나왔다.
#    문어 촉수는 0.280s · 어택 100ms · 800~3k 69% 다 — 길이·어택·대역이 **셋 다** 붙어 버렸다.
#    문어는 쿨마다 계속 후리는 무기라, 여기서 겹치면 버프를 먹었는지조차 모른다.
#    ⇒ **부풀어 오르는 소리**로 바꿨다. 정점을 235ms 로 끌고 가면 어택이 2.3배 벌어지고,
#      길이도 1.8배가 된다. 채찍질과 파워업은 원래 그렇게 달라야 한다.
NOTES = (1046.50, 1568.00, 2093.00, 3136.00)    # C6 · G6 · C7 · G7
NOTE_AT = (0.000, 0.075, 0.150, 0.225)          # 🔑 정점이 225ms 뒤 = 부풀어 오른다
NOTE_DEC = 0.150                                # 간격(75ms)보다 2배 길다 = 반드시 겹친다
NOTE_ATK = 0.006
HARM2 = 0.28                            # 2배음. 순수 사인은 너무 물러서 "전자음"이 된다
HARM3 = 0.10
NOTE_GAIN = 1.00

# ② 쉬머 — 올라가는 노이즈. "마법이 걸린다"를 만드는 층
SH_F0 = 1500.0
SH_F1 = 7000.0
SH_RISE = 0.230                         # 음 상승과 같은 시간에 도착해야 한다
SH_Q = 1.20
SH_DEC = 0.150
SH_GAIN = 0.55          # ⚠️ 0.34 는 3k~9k 가 7% 라 마법 층이 안 들렸다. 다만 `Heal` 이 61% 라 더는 못 올린다

HPF = 420.0                             # 🔴 폭탄 픽업에게 저역을 통째로 넘긴다
TARGET_PEAK = 0.40
CORR = 0.55                             # 고역이라 좌우를 벌려도 모노 합산에서 안 죽는다

OUT = os.path.join("_Incoming", "Audio", "SFX_BuffPickup.wav")


def note_layer(t):
    """세 음이 겹쳐 올라간다. 뒤 음일수록 조금 더 크다 = 상승의 방향이 생긴다."""
    out = np.zeros(len(t))
    for i, (f, at) in enumerate(zip(NOTES, NOTE_AT)):
        lt = t - at
        m = lt >= 0.0
        e = np.zeros(len(t))
        a = np.clip(lt[m] / NOTE_ATK, 0, 1)
        e[m] = (a * a * (3.0 - 2.0 * a)) * np.exp(-np.clip(lt[m] - NOTE_ATK, 0, None) / NOTE_DEC)

        ph = 2.0 * math.pi * f * np.clip(lt, 0, None)
        tone = np.sin(ph) + HARM2 * np.sin(2.0 * ph) + HARM3 * np.sin(3.0 * ph)
        out += tone * e * (0.55 + 0.15 * i)     # 🔑 뒤 음이 커야 "올라간다"로 들린다
    return out


def shimmer_layer(noise, t):
    rise = np.clip(t / SH_RISE, 0, 1)
    freqs = SH_F0 + (SH_F1 - SH_F0) * (rise ** 0.7)
    sig = sweep_bandpass(noise, freqs, SH_Q)
    atk = np.clip(t / 0.008, 0, 1)
    return sig * atk * np.exp(-np.clip(t - 0.030, 0, None) / SH_DEC)


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260831)

    cn = rng.normal(0.0, 1.0, n)
    notes = note_layer(t)               # 음은 좌우 공통 — 벌리면 음정이 흔들려 들린다

    chans = []
    for _ in range(2):
        nz = CORR * cn + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        mix = NOTE_GAIN * notes + SH_GAIN * shimmer_layer(nz, t)
        chans.append(run_biquad(mix, biquad_highpass(HPF)))

    st = np.stack(chans, axis=1)
    tail = np.clip(np.linspace(1.0, 0.0, n) / 0.07, 0, 1)
    st *= tail[:, None]
    st *= TARGET_PEAK / np.abs(st).max()

    write_wav(OUT, st)
    report(OUT, st)

    # cp949 콘솔이라 이 아래 출력은 ASCII 만 쓴다
    mono = st.mean(axis=1)
    # "올라가나"는 뒷부분 에너지로 재면 안 된다 - 마지막 음이 225ms 에 시작하므로
    # 뒤 1/3 은 무조건 꼬리다. **음마다의 봉우리**가 커지는지를 본다.
    wins = [(0.000, 0.075), (0.075, 0.150), (0.150, 0.225), (0.225, 0.320)]
    peaks = [float(np.abs(mono[int(FS * a):int(FS * b)]).max()) for a, b in wins]
    print("  note peaks %.3f -> %.3f -> %.3f -> %.3f   [must increase; that IS the rise]"
          % tuple(peaks))
    print("  low 0-200Hz %.2f%%   [must be ~0 so the bomb pickup keeps its punch]"
          % band_share(mono, 0, 200))
    print("  vs LevelUp 200-800Hz: %.1f%% here / 62%% there   [these two can play together]"
          % band_share(mono, 200, 800))


if __name__ == "__main__":
    main()
