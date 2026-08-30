# -*- coding: utf-8 -*-
"""
SFX 생성 스크립트 공용 DSP.

왜 따로 뺐나
    `gen_sword_swish.py` · `gen_toxin_splash.py` 가 같은 biquad 를 각자 복사해 들고 있다.
    소환수 2종(C20)까지 하면 **같은 필터가 4벌**이 된다. 여기서 끊는다.

⚠️ 기존 두 스크립트는 일부러 안 고쳤다.
    그 wav 들은 이미 Unity 에 임포트돼 있고 볼륨까지 귀로 맞춰 가는 중이다.
    리팩터링하다 출력이 1비트라도 달라지면 그 판정이 통째로 무효가 된다.
    다음에 저 둘을 **다시 구울 일이 생기면** 그때 같이 옮긴다.

규격 (기존 SFX_*.wav 실측)
    44100Hz · 16bit · 스테레오. 프로젝트가 `forceToMono: 1` 로 접으므로
    🔴 **모노 합산 피크로 검산해야 한다.** 스테레오 피크만 보면 실제보다 크게 잡는다.
"""

import math
import os
import wave

import numpy as np

FS = 44100


def biquad_bandpass(f0, q):
    """RBJ 밴드패스 (peak gain = 1). 정규화된 (b0,b1,b2,a1,a2)."""
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


def write_wav(path, stereo):
    """스테레오 float (-1..1) → 16bit PCM."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(stereo, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(FS)
        w.writeframes(pcm.tobytes())


def band_share(mono, lo, hi):
    """전체 에너지 중 lo~hi Hz 가 차지하는 비율(%). 소리끼리 갈리는지 보는 자다."""
    spec = np.abs(np.fft.rfft(mono)) ** 2
    freq = np.fft.rfftfreq(len(mono), 1.0 / FS)
    total = spec.sum()
    if total <= 0:
        return 0.0
    return 100.0 * spec[(freq >= lo) & (freq < hi)].sum() / total


def report(path, stereo):
    """구운 뒤 항상 같은 항목을 찍는다. cp949 콘솔이라 출력은 ASCII 기호만 쓴다."""
    flat = stereo.reshape(-1)
    mono = stereo.mean(axis=1)
    peak_i = int(np.argmax(np.abs(mono)))
    print("%s  %.3fs  peak %.3f  rms %.4f"
          % (path, len(mono) / FS, np.abs(flat).max(),
             float(np.sqrt((flat ** 2).mean()))))
    print("  mono peak %.3f  at %.1f ms" % (np.abs(mono).max(), 1000.0 * peak_i / FS))
    print("  band  0-200Hz %.1f%%  200-800Hz %.1f%%  800-3k %.1f%%  3k-9k %.1f%%"
          % (band_share(mono, 0, 200), band_share(mono, 200, 800),
             band_share(mono, 800, 3000), band_share(mono, 3000, 9000)))
    return peak_i / FS
