# -*- coding: utf-8 -*-
"""
폭탄 픽업이 터지는 소리 — _Incoming/Audio/SFX_BombPickup.wav  (SfxId 25 예정)

무엇인가
    화면의 적을 통째로 쓸어버리는 픽업(C23)이 터질 때 한 번 난다.
    드랍 0.7% = **런당 4번쯤**. 게임에서 가장 드물게 나는 소리다.

🔴 이 소리의 진짜 문제는 `SfxId.Explosion` 이다
    폭탄 무기(`AoeWeapon`)가 이미 폭발음을 쓴다. 같은 "폭발"이라 대역으로는 못 가른다 —
    둘 다 저역이 전부다. 그래서 **대역이 아닌 두 축**으로 갈랐다.

    | | 이 소리 | `Explosion` (3) | 왜 이렇게 갈랐나 |
    |---|---|---|---|
    | **어택** | 🔑 **~4ms** | 78.5ms(실측) | 화면 청소는 **먼저 때리고** 설명한다. 뜸 들이면 위력이 안 산다 |
    | **길이** | 🔑 **0.85s** | 0.570s(실측) | 1.5배. 꼬리 럼블이 "화면 전체"를 만든다 |
    | 0~200Hz | ~62% | 80%(실측) | 200~800 에 **몸통**을 남겼다 — 이게 "Magnet 보다 굵게"다 |

    ⚠️ 대역만 보면 둘은 가깝게 나온다. **그건 의도한 것이다** — 폭발은 폭발처럼 들려야 한다.
    `compare_sfx.py` 의 거리가 작게 나와도 어택 10배·길이 1.5배면 귀는 갈라 듣는다.

"굵게" 를 만드는 것 — 서브가 아니라 **몸통**이다
    저역만 키우면 작은 스피커·노트북에서 **아무 소리도 안 난다**(재생이 안 되는 대역이다).
    1. **슬램** 160 -> 38Hz 하강 사인 — 배를 치는 층. 큰 스피커에서만 들린다
    2. **몸통** 90~500Hz 노이즈 — 🔑 어디서 들어도 남는 층. 이게 없으면 폰에서 "틱" 이다
    3. **크랙** 2~8kHz 25ms — 가장자리. 없으면 이불 덮은 소리가 된다
    4. **럼블 꼬리** 0.45s 감쇠 — 화면 전체가 흔들렸다는 잔향

실행:  python Tools/Audio/gen_bomb_pickup.py
"""

import math
import os

import numpy as np

from dsp import (FS, band_share, biquad_bandpass, biquad_highpass, report,
                 run_biquad, smooth, write_wav)

DUR = 0.85

# ① 슬램 — 내려가는 사인. 폭발의 "몸"이다
SL_F0 = 160.0
SL_F1 = 38.0
SL_FALL = 0.055         # 빨리 내려간다 = 때린다. 느리면 신디사이저 효과음이 된다
SL_DEC = 0.170
SL_GAIN = 0.72          # ⚠️ 1.0 이면 이 층 하나가 스펙트럼을 다 먹는다 (아래 ② 주석)

# ② 몸통 — 🔑 작은 스피커에서 살아남는 유일한 층
#    ⚠️ 처음엔 90~500Hz 였는데 0~200Hz 가 **96.9%** 로 나왔다. 슬램(사인)이 에너지를 다 먹어서
#    몸통이 저역 안에 파묻힌 것이다. 대역을 통째로 올려 슬램 위로 빼냈다.
BD_LO = 180.0
BD_HI = 700.0
BD_DEC = 0.180
BD_GAIN = 2.10

# ③ 크랙 — 가장자리. 크게 하면 총소리가 된다
CR_LO = 2000.0
CR_HI = 8000.0
CR_DEC = 0.022
CR_GAIN = 1.60

# ④ 럼블 꼬리 — "화면 전체"를 만드는 층. 길고 작다
RB_HI = 240.0
RB_DEC = 0.450
RB_GAIN = 0.42
RB_MOD_HZ = 22.0        # 잔해가 구르는 느낌. 없으면 그냥 낮은 노이즈다

ATK = 0.004             # 🔑 Explosion 78.5ms 의 1/20. 두 소리를 가르는 첫 번째 축
TARGET_PEAK = 0.70
CORR = 0.80             # 저역은 좌우가 붙어 있어야 한다. 벌리면 모노 합산에서 상쇄된다

OUT = os.path.join("_Incoming", "Audio", "SFX_BombPickup.wav")


def env(t, atk, dec):
    a = np.clip(t / atk, 0, 1)
    a = a * a * (3.0 - 2.0 * a)
    return a * np.exp(-np.clip(t - atk, 0, None) / dec)


def slam_layer(t):
    """내려가는 사인. 위상은 주파수를 적분해서 만든다."""
    freq = SL_F1 + (SL_F0 - SL_F1) * np.exp(-t / SL_FALL)
    phase = 2.0 * math.pi * np.cumsum(freq) / FS
    return np.sin(phase) * env(t, ATK, SL_DEC)


def band_layer(noise, t, lo, hi, dec, atk=ATK):
    center = math.sqrt(lo * hi)
    sig = run_biquad(noise, biquad_bandpass(center, center / (hi - lo)))
    return sig * env(t, atk, dec)


def rumble_layer(noise, t, rng):
    n = len(t)
    sig = run_biquad(noise, biquad_bandpass(math.sqrt(60.0 * RB_HI),
                                            math.sqrt(60.0 * RB_HI) / (RB_HI - 60.0)))
    # 잔해가 구르는 흔들림. 불(gen_dragon_spit)의 펄럭임과 같은 수법이지만 훨씬 느리다
    mod = smooth(rng.random(n), 1000.0 / RB_MOD_HZ)
    mod = (mod - mod.min()) / (mod.max() - mod.min() + 1e-9)
    return sig * (0.45 + 0.55 * mod) * env(t, 0.012, RB_DEC)


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260831)

    ca = rng.normal(0.0, 1.0, n)
    cb = rng.normal(0.0, 1.0, n)
    cc = rng.normal(0.0, 1.0, n)
    slam = slam_layer(t)          # 사인이라 좌우 공통 — 상쇄가 없어야 한다

    chans = []
    for _ in range(2):
        na = CORR * ca + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        nb = CORR * cb + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        nc = 0.5 * cc + 0.5 * rng.normal(0.0, 1.0, n)   # 크랙만 넓게 = 파편이 흩어진다
        mix = (SL_GAIN * slam
               + BD_GAIN * band_layer(na, t, BD_LO, BD_HI, BD_DEC)
               + CR_GAIN * band_layer(nc, t, CR_LO, CR_HI, CR_DEC, atk=0.001)
               + RB_GAIN * rumble_layer(nb, t, rng))
        chans.append(run_biquad(mix, biquad_highpass(28.0)))   # DC·초저역만 잘라낸다

    st = np.stack(chans, axis=1)
    tail = np.clip(np.linspace(1.0, 0.0, n) / 0.09, 0, 1)
    st *= tail[:, None]
    st *= TARGET_PEAK / np.abs(st).max()

    write_wav(OUT, st)
    report(OUT, st)

    # cp949 콘솔이라 이 아래 출력은 ASCII 만 쓴다 (한글은 깨져서 검산이 안 읽힌다)
    mono = st.mean(axis=1)
    head = int(FS * 0.010)
    print("  head 10ms peak %.3f of %.3f   [instant? Explosion peaks at 78ms]"
          % (np.abs(mono[:head]).max(), np.abs(mono).max()))
    print("  body 200-800Hz %.1f%%   [Magnet 43%%, Explosion 13%% - this is the 'thick' layer]"
          % band_share(mono, 200, 800))
    print("  edge 2k-9kHz   %.1f%%   [0 means it is inaudible on laptop speakers]"
          % band_share(mono, 2000, 9000))
    print("  tail 0.5s+ peak %.3f   [screen-wide rumble; 0 = just one short pop]"
          % np.abs(mono[int(FS * 0.5):]).max())


if __name__ == "__main__":
    main()
