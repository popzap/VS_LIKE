# -*- coding: utf-8 -*-
"""
도전 석상이 깨어난다 — Assets/Game/Audio/SFX_StatueAwaken.wav  (SfxId 35)

무엇인가
    D123 의 도전 석상. `E` 를 누르면 **돌이 갈리며 엘리트가 깨어난다.**
    지금은 `BossAppear`(33)를 빌려 쓰는데 그건 **보스가 나타났다**는 통보다 —
    석상은 **내가 누른 결과**라서 성격이 반대다(통보 vs 응답).

🔑 이 소리가 하는 말은 *"네가 방금 뭔가를 깨웠다"* 다
    · 앞이 무거워야 한다 — 가벼우면 버튼 소리가 되고, 그러면 **위험을 산 느낌**이 안 난다.
    · 그런데 **때리는 소리는 아니다.** 폭발(3)처럼 터지면 피해로 들린다.
      ⇒ 어택을 느리게(≈120ms) 두어 **일어나는** 소리로 만든다.

어디가 비어 있나 (compare_sfx.py 실측)
    · **1.02초보다 긴 소리가 없다.** 제일 긴 셋이 `PlayerHit`(1.015) · `ChestOpen`(1.012) · `LevelUp`(1.021)
      인데 셋 다 저역이 3% 이하다. ⇒ **길이 1.35s + 저역 우세**는 아무도 안 쓰는 자리다.
    · 저역 무리는 전부 짧다 — `Explosion`(0.570/80%) · `BombPickup`(0.850/65%) · `EnemyShoot`(0.627/56%).
      길이만으로 이미 갈린다.
    · 🔴 `Explosion` 과는 **어택으로도** 갈라 둔다(78ms vs 120ms). 지뢰·폭탄이 자주 터지므로
      여기가 제일 붙기 쉬운 이웃이다.

"돌이 갈린다"로 들리게 하는 것
    1. **갈림**: 200~900 Hz 대역잡음에 **느린 진폭 떨림**(6~9 Hz)을 건다.
       매끈한 잡음은 바람이고, 떨리는 잡음이라야 돌끼리 긁히는 소리가 된다.
    2. **디딤**: 0.14s 에 55/82 Hz 짧은 충격 — 발이 바닥을 딛는다.
    3. **울림**: 비조화 부분음(1.00 / 2.31 / 3.87)을 가진 110 Hz 저음 종.
       🔴 정수배로 두면 악기가 된다 — 석상은 악기가 아니라 **돌덩이**다.

실행:  python Tools/Audio/gen_statue_awaken.py
"""

import math
import os

import numpy as np

from dsp import (FS, biquad_bandpass, biquad_highpass, report, run_biquad,
                 smooth, write_wav)

# 🔑 길이가 이 소리의 자리다 — 목록에서 제일 긴 것보다 길다(1.021s → 1.35s).
DUR = 1.35

# ① 갈림 — 돌끼리 긁히는 층
GRIND_LO, GRIND_HI = 170.0, 720.0
GRIND_AT   = 0.000
GRIND_RISE = 0.095        # 천천히 차오르되 EnemyShoot(229ms)만큼은 아니다 (아래 주석)
GRIND_DEC  = 0.620
GRIND_WOB  = 7.5          # 진폭 떨림(Hz) — 🔴 이게 없으면 그냥 바람 소리다
GRIND_WOB_DEPTH = 0.55
GRIND_GAIN = 0.62         # rms 기준 (D116 의 교훈: 눈대중 금지)

# ② 디딤 — 발이 바닥을 딛는 한 방
STOMP_AT  = 0.140
STOMP_F   = (55.0, 82.0)
STOMP_DEC = 0.170
STOMP_AMP = 1.05

# ③ 울림 — 비조화 저음 종
BELL_F0   = 110.0
BELL_AT   = 0.120         # 🔴 어택. Explosion(78ms)과 여기서 갈린다
BELL_PART = (1.00, 2.31, 3.87)
BELL_AMP  = (1.00, 0.38, 0.16)
BELL_DEC0 = 0.520
BELL_FALL = 0.58          # 윗부분음일수록 빨리 죽는다

HPF = 38.0                # 🔴 40 Hz 아래는 스피커에서 안 들리고 헤드룸만 먹는다
TARGET_PEAK = 0.52

OUT = os.path.join("Assets", "Game", "Audio", "SFX_StatueAwaken.wav")


def env(t, at, rise, dec):
    """느리게 차올랐다 길게 죽는 포락선."""
    late = np.clip(t - at, 0.0, None)
    gate = (t >= at).astype(float)
    up = np.clip(late / max(1e-6, rise), 0.0, 1.0)
    return gate * up * np.exp(-np.clip(late - rise, 0.0, None) / dec)


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260909)
    noise = rng.uniform(-1.0, 1.0, n)

    # ③ 울림부터 만든다 — 나머지 층의 이득을 이 rms 에 맞춘다
    late_b = np.clip(t - BELL_AT, 0.0, None)
    gate_b = (t >= BELL_AT).astype(float)
    atk_b = np.clip(late_b / 0.045, 0.0, 1.0)
    bell = np.zeros(n)
    for i, (r, a) in enumerate(zip(BELL_PART, BELL_AMP)):
        dec = BELL_DEC0 * (BELL_FALL ** i)
        bell += a * np.sin(2.0 * math.pi * BELL_F0 * r * late_b) * np.exp(-late_b / dec)
    bell *= gate_b * atk_b
    ref = math.sqrt(float((bell ** 2).mean()))

    # ① 갈림
    c = math.sqrt(GRIND_LO * GRIND_HI)
    grind = run_biquad(noise, biquad_bandpass(c, c / (GRIND_HI - GRIND_LO)))
    wob = 1.0 - GRIND_WOB_DEPTH * (0.5 + 0.5 * np.sin(2.0 * math.pi * GRIND_WOB * t))
    grind = grind * env(t, GRIND_AT, GRIND_RISE, GRIND_DEC) * smooth(wob, 6.0)
    grind *= (ref / max(1e-9, math.sqrt(float((grind ** 2).mean())))) * GRIND_GAIN

    # ② 디딤
    late_s = np.clip(t - STOMP_AT, 0.0, None)
    gate_s = (t >= STOMP_AT).astype(float)
    stomp = np.zeros(n)
    for f in STOMP_F:
        stomp += np.sin(2.0 * math.pi * f * late_s)
    stomp *= gate_s * np.exp(-late_s / STOMP_DEC)
    stomp *= (ref / max(1e-9, math.sqrt(float((stomp ** 2).mean())))) * STOMP_AMP

    mono = bell + grind + stomp
    mono = run_biquad(mono, biquad_highpass(HPF))
    mono *= TARGET_PEAK / max(1e-9, float(np.abs(mono).max()))

    # 석상은 한 자리에 있다 — 넓히지 않는다(위치가 곧 정보다).
    stereo = np.stack([mono, mono], axis=1)
    write_wav(OUT, stereo)
    report(OUT, stereo)


if __name__ == "__main__":
    main()
