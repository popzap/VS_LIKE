# -*- coding: utf-8 -*-
"""
드래곤 화염구 뱉기 SFX — _Incoming/Audio/SFX_DragonSpit.wav  (SfxId 7)

왜 필요한가
    `SummonWeapon.cs:159` 가 `SfxId.WeaponCast` 를 빌려 쓴다.
    촉수(총소리)만큼 틀리진 않지만 — **마법 시전음이지 짐승의 숨이 아니다.**

🔴 이 소리만의 제약 — 저역을 비워 둬야 한다
    화염구는 날아가서 `Explosion`(Volume 0.55, 사다리 최상단)으로 터진다.
    쿨 0.56s(Lv5+공속Lv5) 에 비행시간까지 겹치면 **뱉는 소리와 터지는 소리가 같이 운다.**
    폭발은 저역이 전부다. 여기서 0~200Hz 를 먹으면 폭발이 흐려진다.
    ⇒ HPF 380Hz. 검 클립에서 0~200Hz 를 0%로 만든 것과 같은 이유다.

"불"로 들리게 하는 것 — 스펙트럼이 아니라 **흔들림**이다
    좁은 대역의 노이즈 버스트는 그냥 "칙"(스프레이)이다. 불은 **일정하지 않다.**
    1. **불규칙 진폭 변조**(~85Hz) — 화염이 펄럭인다. 이게 없으면 에어브러시 소리다.
    2. **크래클 3발** — 아주 짧은 고역 틱. 장작 튀는 소리의 역할이다.
    ⚠️ 변조는 고역 대역에 **거는** 것이라 저역 성분이 생기지 않는다. HPF 규칙과 안 부딪친다.

다른 소리와 갈라 놓은 것
    | | 이 소리 | 부딪칠 상대 | 어떻게 갈랐나 |
    |---|---|---|---|
    | 하강 스윕 | 2800 -> 850Hz | 장판 900 -> 260Hz | **바닥이 3배 높다** + 길이 0.22 vs 0.70 |
    | 짧은 버스트 | 0.22s | 총(`WeaponFire`) | 총은 트랜지언트가 전부, 이건 **펄럭이는 꼬리**가 있다 |
    | 저역 | 🔴 **0%** | 폭발(`Explosion`) | 아예 안 겹치게 비웠다 |

길이 0.22 의 근거 — 점유율
    Lv5 쿨 0.8s · `AttackSpeed` Lv5(-0.30) 까지면 **0.56s** 마다.
    0.22s 면 39% — 장판과 같은 수준이다. 소환수는 쉬지 않으므로 더 늘리지 않는다.

실행:  python Tools/Audio/gen_dragon_spit.py
"""

import math
import os

import numpy as np

from dsp import (FS, biquad_bandpass, biquad_highpass, band_share, report,
                 run_biquad, smooth, sweep_bandpass, write_wav)

DUR = 0.22

# ① 숨 — 내려가는 넓은 스윕. 압력이 빠지는 소리다
BR_F0 = 2200.0
BR_F1 = 780.0           # 🔴 장판의 260Hz 와 3배 벌려 놨다
BR_FALL = 0.055
BR_Q = 0.95             # 낮다 = 넓다 = 숨결. 높이면 휘파람이 된다
BR_ATK = 0.006          # 6ms. 뱉는 건 순간이다
BR_DEC = 0.085
BR_GAIN = 1.00

# ② 펄럭임 — 불이 "일정하지 않다"를 만드는 층
FL_LO = 1200.0
FL_HI = 4800.0
FL_MOD = 0.80           # 🔑 0 으로 두면 불이 아니라 에어브러시가 된다
FL_MOD_HZ = 85.0
FL_DEC = 0.100          # 숨보다 오래 남는다 = 꼬리가 펄럭인다
FL_GAIN = 0.55

# ③ 크래클 — 장작 튀는 틱
N_CRACKLE = 3
CR_T = (0.015, 0.150)
CR_F = (3200.0, 7000.0)
CR_LEN = 0.006
CR_GAIN = 0.28

HPF = 380.0             # 🔴 Explosion 에게 저역을 통째로 넘긴다
TARGET_PEAK = 0.36
CORR = 0.62

OUT = os.path.join("_Incoming", "Audio", "SFX_DragonSpit.wav")


def breath_layer(noise, n):
    t = np.linspace(0.0, DUR, n, endpoint=False)
    freqs = BR_F1 + (BR_F0 - BR_F1) * np.exp(-t / BR_FALL)
    sig = sweep_bandpass(noise, freqs, BR_Q)

    atk = np.clip(t / BR_ATK, 0, 1)
    atk = atk * atk * (3.0 - 2.0 * atk)
    return sig * atk * np.exp(-np.clip(t - BR_ATK, 0, None) / BR_DEC)


def flutter_layer(noise, n, rng):
    t = np.linspace(0.0, DUR, n, endpoint=False)
    band = math.sqrt(FL_LO * FL_HI)
    sig = run_biquad(noise, biquad_bandpass(band, band / (FL_HI - FL_LO)))

    # 🔑 불규칙 진폭 변조. 이 층이 "불"의 전부다
    mod = smooth(rng.random(n), 1000.0 / FL_MOD_HZ)
    mod = (mod - mod.min()) / (mod.max() - mod.min() + 1e-9)
    sig = sig * (1.0 - FL_MOD + FL_MOD * mod)

    rise = np.clip(t / 0.010, 0, 1)
    return sig * rise * np.exp(-t / FL_DEC)


def crackle_layer(n, rng):
    """아주 짧은 고역 틱 몇 발. 장작 튀는 소리."""
    out = np.zeros(n)
    clen = int(FS * CR_LEN)
    ct = np.arange(clen) / FS
    for _ in range(N_CRACKLE):
        f = rng.uniform(*CR_F)
        env = np.exp(-ct / 0.0015)
        tick = np.sin(2.0 * math.pi * f * ct) * env
        start = int(FS * rng.uniform(*CR_T))
        end = min(n, start + clen)
        out[start:end] += tick[:end - start] * rng.uniform(0.5, 1.0)
    return out


def main():
    n = int(FS * DUR)
    rng = np.random.default_rng(20260830)

    ca = rng.normal(0.0, 1.0, n)
    cb = rng.normal(0.0, 1.0, n)
    crackles = crackle_layer(n, rng)

    chans = []
    for _ in range(2):
        na = CORR * ca + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        nb = CORR * cb + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        mix = (BR_GAIN * breath_layer(na, n)
               + FL_GAIN * flutter_layer(nb, n, rng)
               + CR_GAIN * crackles)
        chans.append(run_biquad(mix, biquad_highpass(HPF)))

    st = np.stack(chans, axis=1)
    tail = np.clip(np.linspace(1.0, 0.0, n) / 0.06, 0, 1)
    st *= tail[:, None]
    st *= TARGET_PEAK / np.abs(st).max()

    write_wav(OUT, st)
    report(OUT, st)

    mono = st.mean(axis=1)
    head = int(FS * 0.04)
    print("  head 40ms peak %.3f / tail peak %.3f  (뱉고 나서 펄럭이는 꼬리가 남아야 한다)"
          % (np.abs(mono[:head]).max(), np.abs(mono[head:]).max()))
    print("  vs explosion: 0-200Hz %.2f%%  (0 에 가까울수록 폭발이 안 흐려진다)"
          % band_share(mono, 0, 200))


if __name__ == "__main__":
    main()
