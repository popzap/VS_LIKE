# -*- coding: utf-8 -*-
"""
독 장판 SFX 생성 — _Incoming/Audio/SFX_ToxinSpill.wav

왜 필요한가
    FieldWeapon.cs:32 가 `SfxId.WeaponCast` 를 재사용한다 —
    **독을 뿌리는데 마법 시전음이 난다.** 검(C17)과 같은 처지였다.

왜 절차 생성인가
    장판은 쿨 3.0 → 1.8초로 **한 판 내내 계속 깔린다.** 한 번 들어서 괜찮아도
    30번 들으면 거슬릴 수 있다. 길이·꼬리·거품 수를 고쳐 다시 구울 수 있어야 한다.

소리의 구조 — "액체"로 들리게 하는 3층
    1. **철퍽(splat)** — 밴드패스가 900 → 260Hz 로 **내려간다.**
       올라가면 튀는 소리(검)고, 내려가야 **주저앉는** 소리다.
    2. **치익(fizz)** — 3~8kHz 고역에 **불규칙한 진폭 변조**를 건다.
       변조가 없으면 그냥 화이트노이즈("쉬")고, 있어야 산이 끓는 소리가 된다.
    3. **거품(bubble)** — 꼬리에 흩뿌린 짧은 하강 사인 7개.
       🔑 **이 층이 없으면 액체가 아니라 스프레이로 들린다.**

검(C17)과 일부러 다르게 한 것
    | | 검 | 독 장판 |
    |---|---|---|
    | 스윕 방향 | 700 → 5200Hz (**올라감**) | 900 → 260Hz (**내려감**) |
    | 길이 | 0.28s | 0.70s (깔리고 **남는다**) |
    | 어택 | 25% 완만 (트랜지언트 = 총소리) | 8ms (철퍽은 **순간**이어야 한다) |
    두 소리가 같은 판에서 겹쳐도 **헷갈리지 않는 게 목적**이다.

규격 (기존 SFX_*.wav 실측에 맞춤)
    44100Hz · 16bit · 스테레오 · 0.70초

실행:  python Tools/Audio/gen_toxin_splash.py
"""

import math
import os
import wave

import numpy as np

FS = 44100
DUR = 0.70

# ① 철퍽 — 내려가는 스윕
SPLAT_F0 = 900.0
SPLAT_F1 = 260.0
SPLAT_Q = 1.1
SPLAT_ATK = 0.008          # 8ms. 철퍽은 순간이다 (검과 정반대)
SPLAT_DEC = 0.14
SPLAT_GAIN = 2.10           # 🔑 가장 큰 순간은 "철퍽"이어야 한다 (아래 FIZZ/BUB 와 같이 본다)

# ② 치익 — 고역 + 불규칙 진폭 변조
FIZZ_LO = 3000.0
FIZZ_HI = 8000.0
FIZZ_MOD = 0.75            # 변조 깊이. 0 이면 그냥 "쉬" 소리가 된다
FIZZ_MOD_HZ = 55.0         # 변조를 얼마나 잘게 흔드나
FIZZ_GAIN = 0.45

# ③ 거품 — 꼬리에 흩뿌리는 하강 사인
N_BUBBLE = 7
BUB_T = (0.10, 0.62)       # 이 구간에 무작위로 놓는다
BUB_F = (520.0, 190.0)     # 각 거품이 이만큼 떨어진다
BUB_LEN = 0.028
BUB_GAIN = 0.20

HPF = 120.0                # 저역 정리
TARGET_PEAK = 0.42
CORR = 0.55                # 좌우 공통 비율. 검(0.70)보다 낮다 = 더 넓다(웅덩이는 퍼진다)

OUT = os.path.join("_Incoming", "Audio", "SFX_ToxinSpill.wav")


def biquad_bandpass(f0, q):
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


def run_biquad(x, coeffs):
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


def sweep_bandpass(x, freqs, q):
    """중심 주파수가 움직이는 밴드패스. 64샘플마다 계수를 갱신한다."""
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


def smooth(x, ms):
    """이동평균. 변조 신호를 부드럽게 만들어 지직거림을 없앤다."""
    k = max(1, int(FS * ms / 1000.0))
    return np.convolve(x, np.ones(k) / k, mode="same")


def splat_layer(noise, n):
    u = np.linspace(0.0, 1.0, n, endpoint=False)
    t = u * DUR
    # 내려가는 스윕. 지수로 떨어져야 "주저앉는" 느낌이 난다
    freqs = SPLAT_F1 + (SPLAT_F0 - SPLAT_F1) * np.exp(-t / 0.09)
    sig = sweep_bandpass(noise, freqs, SPLAT_Q)

    atk = np.clip(t / SPLAT_ATK, 0, 1)
    atk = atk * atk * (3.0 - 2.0 * atk)
    env = atk * np.exp(-np.clip(t - SPLAT_ATK, 0, None) / SPLAT_DEC)
    return sig * env


def fizz_layer(noise, n, rng):
    t = np.linspace(0.0, DUR, n, endpoint=False)
    band = np.sqrt(FIZZ_LO * FIZZ_HI)                      # 기하 중심
    q = band / (FIZZ_HI - FIZZ_LO)
    sig = run_biquad(noise, biquad_bandpass(band, q))

    # 🔑 불규칙 진폭 변조 — 이게 "끓는" 느낌을 만든다
    mod = smooth(rng.random(n), 1000.0 / FIZZ_MOD_HZ)
    mod = (mod - mod.min()) / (mod.max() - mod.min() + 1e-9)
    sig = sig * (1.0 - FIZZ_MOD + FIZZ_MOD * mod)

    # 철퍽보다 살짝 늦게 올라와서 오래 남는다 (장판이 깔리고 남는다)
    rise = np.clip(t / 0.05, 0, 1)
    env = rise * np.exp(-t / 0.30) * np.clip((DUR - t) / 0.12, 0, 1)
    return sig * env


def bubble_layer(n, rng):
    """짧은 하강 사인 몇 개. 없으면 액체가 아니라 스프레이로 들린다."""
    out = np.zeros(n)
    blen = int(FS * BUB_LEN)
    bt = np.arange(blen) / FS
    for _ in range(N_BUBBLE):
        f0 = BUB_F[0] * rng.uniform(0.75, 1.35)
        f1 = BUB_F[1] * rng.uniform(0.75, 1.35)
        # 순간 주파수를 적분해야 위상이 매끄럽다
        inst = f0 + (f1 - f0) * (bt / BUB_LEN)
        phase = 2.0 * math.pi * np.cumsum(inst) / FS
        env = np.exp(-bt / 0.010) * np.clip(bt / 0.002, 0, 1)
        start = int(FS * rng.uniform(*BUB_T))
        end = min(n, start + blen)
        out[start:end] += (np.sin(phase) * env)[:end - start] * rng.uniform(0.5, 1.0)
    return out


def main():
    n = int(FS * DUR)
    rng = np.random.default_rng(20260830)

    common_a = rng.normal(0.0, 1.0, n)
    common_b = rng.normal(0.0, 1.0, n)
    bubbles = bubble_layer(n, rng)

    chans = []
    for _ in range(2):
        na = CORR * common_a + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        nb = CORR * common_b + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        mix = (SPLAT_GAIN * splat_layer(na, n)
               + FIZZ_GAIN * fizz_layer(nb, n, rng)
               + BUB_GAIN * bubbles)
        mix = run_biquad(mix, biquad_highpass(HPF))
        chans.append(mix)

    st = np.stack(chans, axis=1)
    # 끝을 0 으로 확실히 내린다 (클릭 방지)
    tail = np.clip(np.linspace(1.0, 0.0, n) / 0.06, 0, 1)
    st *= tail[:, None]
    st *= TARGET_PEAK / np.abs(st).max()

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    pcm = (np.clip(st, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(OUT, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(FS)
        w.writeframes(pcm.tobytes())

    flat = st.reshape(-1)
    mono = st.mean(axis=1)
    head = int(FS * 0.05)
    print("%s  %.3fs  peak %.3f  rms %.4f"
          % (OUT, n / FS, np.abs(flat).max(), float(np.sqrt((flat ** 2).mean()))))
    print("  모노 합산 피크 %.3f  (좌우 상쇄 없음 확인용)" % np.abs(mono).max())
    # cp949 콘솔에서 em dash 가 죽는다. 여기 출력문에는 ASCII 기호만 쓴다
    print("  철퍽 스윕 %.0f -> %.0f Hz (내려간다. 검은 올라간다)"
          % (SPLAT_F0, SPLAT_F1))
    print("  앞 50ms 피크 %.3f / 뒤 꼬리 피크 %.3f  (철퍽 먼저, 치익이 남는다)"
          % (np.abs(st[:head]).max(), np.abs(st[head:]).max()))


if __name__ == "__main__":
    main()
