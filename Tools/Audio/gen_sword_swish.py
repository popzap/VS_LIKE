# -*- coding: utf-8 -*-
"""
검 휘두름 SFX 생성 — _Incoming/Audio/SFX_WeaponSwing.wav

왜 절차 생성인가
    MeleeWeapon 은 Lv5 에서 0.70초 쿨다운에 **3연타**를 때린다.
    같은 소리가 0.1초 간격으로 3번 겹치므로, 저음이 조금만 남아도
    "쉭쉭쉭" 이 아니라 "웅" 하고 뭉친다. 겹쳐서 들어 보고
    파라미터를 고칠 수 있어야 해서 스크립트로 남긴다.

소리의 구조 (총소리가 되지 않기 위한 3가지)
    1. **트랜지언트가 없다** — 어택을 25% 구간에 걸쳐 부드럽게 올린다.
       딱 하는 시작음이 곧 총소리다.
    2. **저음이 없다** — 300Hz 하이패스. 3연타 저음 뭉침을 막는다.
    3. **주파수가 움직인다** — 밴드패스 중심이 올라갔다 내려온다.
       칼이 귀 앞을 지나가는 도플러다. 고정 노이즈는 그냥 잡음이다.

규격 (기존 SFX_*.wav 실측에 맞춤)
    44100Hz · 16bit · 스테레오 · 0.28초 (DEV 요청 0.2~0.4초)
    피크 0.40 / RMS 0.055 — SFX_WeaponFire(0.476/0.091)보다 의도적으로 작다.
    3연타로 겹치면 체감은 3배가 되기 때문이다.

실행:  python Tools/Audio/gen_sword_swish.py
"""

import math
import os
import struct
import wave

import numpy as np

FS = 44100
DUR = 0.28

# 밴드패스 중심 주파수 — 올라갔다 내려온다 (칼이 지나간다)
F_START = 700.0
F_PEAK = 5200.0
F_END = 1500.0
PEAK_AT = 0.38          # 정점이 오는 위치. 0.5 면 대칭이라 밋밋하다
Q = 2.2                 # 높으면 휘파람, 낮으면 그냥 히스. 2 근처가 "쉭"

HPF = 300.0             # 3연타 저음 뭉침 방지
ATTACK = 0.25           # 이 구간에 걸쳐 부드럽게 올린다 (트랜지언트 제거)
DECAY = 4.2

TARGET_PEAK = 0.40
CORR = 0.70             # 좌우 공통 성분 비율. 모노로 합쳐도 안 사라지게

OUT = os.path.join("_Incoming", "Audio", "SFX_WeaponSwing.wav")


def biquad_bandpass(f0, q):
    """RBJ 밴드패스 (peak gain = 1). 정규화된 (b0,b1,b2,a1,a2) 를 준다."""
    w0 = 2.0 * math.pi * f0 / FS
    alpha = math.sin(w0) / (2.0 * q)
    a0 = 1.0 + alpha
    return (alpha / a0, 0.0, -alpha / a0,
            (-2.0 * math.cos(w0)) / a0, (1.0 - alpha) / a0)


def biquad_highpass(f0, q=0.707):
    w0 = 2.0 * math.pi * f0 / FS
    alpha = math.sin(w0) / (2.0 * q)
    c = math.cos(w0)
    a0 = 1.0 + alpha
    return ((1.0 + c) / 2.0 / a0, -(1.0 + c) / a0, (1.0 + c) / 2.0 / a0,
            (-2.0 * c) / a0, (1.0 - alpha) / a0)


def sweep_curve(n):
    """0..1 위치마다 밴드패스 중심 주파수. PEAK_AT 에서 정점."""
    u = np.linspace(0.0, 1.0, n, endpoint=False)
    up = u / PEAK_AT
    dn = (u - PEAK_AT) / (1.0 - PEAK_AT)
    rise = F_START + (F_PEAK - F_START) * np.sin(np.clip(up, 0, 1) * math.pi / 2) ** 1.3
    fall = F_PEAK + (F_END - F_PEAK) * np.clip(dn, 0, 1) ** 0.8
    return np.where(u < PEAK_AT, rise, fall)


def envelope(n):
    """부드러운 어택 + 지수 감쇠 + 끝 8% 페이드아웃(클릭 방지)."""
    u = np.linspace(0.0, 1.0, n, endpoint=False)
    atk = np.clip(u / ATTACK, 0, 1)
    atk = atk * atk * (3.0 - 2.0 * atk)          # smoothstep
    dec = np.exp(-DECAY * np.clip(u - ATTACK, 0, None) / (1.0 - ATTACK))
    tail = np.clip((1.0 - u) / 0.08, 0, 1)
    return atk * dec * tail


def sweep_filter(x, freqs, q):
    """중심 주파수가 매 샘플 바뀌는 밴드패스. 64샘플마다 계수를 갱신한다."""
    y = np.empty_like(x)
    x1 = x2 = y1 = y2 = 0.0
    b0 = b1 = b2 = a1 = a2 = 0.0
    for i in range(len(x)):
        if i % 64 == 0:
            b0, b1, b2, a1, a2 = biquad_bandpass(float(freqs[i]), q)
        xi = float(x[i])
        yi = b0 * xi + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, xi
        y2, y1 = y1, yi
        y[i] = yi
    return y


def static_filter(x, coeffs):
    b0, b1, b2, a1, a2 = coeffs
    y = np.empty_like(x)
    x1 = x2 = y1 = y2 = 0.0
    for i in range(len(x)):
        xi = float(x[i])
        yi = b0 * xi + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, xi
        y2, y1 = y1, yi
        y[i] = yi
    return y


def main():
    n = int(FS * DUR)
    rng = np.random.default_rng(20260830)

    freqs = sweep_curve(n)
    env = envelope(n)

    # 공통 성분 + 좌우 독립 성분 → 모노 호환되는 넓이
    common = rng.normal(0.0, 1.0, n)
    chans = []
    for seed_noise in (rng.normal(0.0, 1.0, n), rng.normal(0.0, 1.0, n)):
        raw = CORR * common + (1.0 - CORR) * seed_noise
        sig = sweep_filter(raw, freqs, Q)
        sig = static_filter(sig, biquad_highpass(HPF))
        chans.append(sig * env)

    st = np.stack(chans, axis=1)
    st *= TARGET_PEAK / np.abs(st).max()

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    pcm = np.clip(st, -1.0, 1.0)
    pcm = (pcm * 32767.0).astype("<i2")
    with wave.open(OUT, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(FS)
        w.writeframes(pcm.tobytes())

    flat = st.reshape(-1)
    mono = st.mean(axis=1)
    print("%s  %.3fs  peak %.3f  rms %.4f" % (OUT, n / FS, np.abs(flat).max(),
                                              float(np.sqrt((flat ** 2).mean()))))
    print("  모노 합산 피크 %.3f  (좌우 상쇄 없음 확인용)" % np.abs(mono).max())
    print("  주파수 스윕 %.0f -> %.0f -> %.0f Hz" % (F_START, F_PEAK, F_END))
    print("  첫 10ms 피크 %.4f  (트랜지언트 없음 확인용)"
          % np.abs(st[:int(FS * 0.01)]).max())


if __name__ == "__main__":
    main()
