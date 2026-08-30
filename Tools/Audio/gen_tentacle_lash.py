# -*- coding: utf-8 -*-
"""
촉수 후리기 SFX — _Incoming/Audio/SFX_TentacleLash.wav  (SfxId 6)

왜 필요한가
    `SummonWeapon.cs:174` 가 `SfxId.WeaponFire` 를 빌려 쓴다 —
    **문어가 촉수를 후리는데 총소리가 난다.** 검(C17)·장판(C18)과 똑같은 병이다.

🔴 이 소리만의 제약 — 피크 위치가 코드로 못박혀 있다
    `SummonWeapon.Lash()` 는 이렇게 돈다:

        SpawnLash(range);                              // t = 0
        AudioManager.Play(SfxId.WeaponFire);           // t = 0  <- 소리가 여기서 시작한다
        yield return new WaitForSeconds(lashHitDelay); // 0.1s
        OverlapCircleAll(...)                          // t = 0.1  <- 피해가 여기서 들어간다

    소리는 **후리기 시작에** 재생되는데 맞는 건 **0.1초 뒤**다.
    그래서 클립은 "때리는 소리"가 아니라 **"휘두르다 0.1초에 때리는 소리"** 여야 한다.
    ⇒ HIT_AT = 0.100 에 피크를 둔다. 그림의 프레임 3(30fps)과도 같은 순간이다.
    ⚠️ DEV 가 `lashHitDelay` 를 바꾸면 **이 상수도 같이 바꿔 다시 구워야 한다.**

길이를 0.28 로 묶은 이유 — 점유율
    소환수는 따라다니며 쉬지 않고 싸운다. Lv5 쿨 0.8s 에 `AttackSpeed` Lv5(-0.30)까지
    겹치면 **최소 0.56s** 마다 운다. 0.28s 면 점유율 50%.
    더 늘리면 한 판 내내 이 소리만 들린다. 🔴 **꼬리를 늘리고 싶어도 여기가 상한이다.**

검(`WeaponSwing`)과 갈라 놓은 것 — 둘 다 "휘두름"이라 제일 헷갈릴 짝이다
    | | 검 | 촉수 |
    |---|---|---|
    | 트랜지언트 | 🔴 **일부러 없앰** (있으면 총소리) | 🔴 **있어야 함** (젖은 것이 부딪는 소리) |
    | 피크 위치 | 시작 부근 | **0.100s** (코드가 정한다) |
    | 스윕 | 700 -> 5200 -> 1500 (**고역까지 올라감**) | 타격 후 1700 -> 430 (**급히 떨어짐**) |
    | 꼬리 | 마른 히스 | **물기** — 저중역 진폭 흔들림 |
    검은 "쉭", 촉수는 "쉬-철썩". 같은 휘두름이라도 **피크가 뒤에 있고 젖었다.**

장판(`ToxinSpill`)과 갈라 놓은 것 — 둘 다 "젖었다"
    길이 0.28 vs 0.70 · 거품층 없음 · 피크가 앞이 아니라 0.1s.
    장판은 깔리고 **남고**, 촉수는 때리고 **끝난다.**

실행:  python Tools/Audio/gen_tentacle_lash.py
"""

import math
import os

import numpy as np

from dsp import (FS, biquad_bandpass, biquad_highpass, band_share, report,
                 run_biquad, smooth, sweep_bandpass, write_wav)

DUR = 0.28
HIT_AT = 0.100          # 🔴 SummonWeapon.lashHitDelay 와 같아야 한다

# ① 휘두름 — 타격 전까지 올라오는 바람 소리
SW_F0 = 700.0
SW_F1 = 2400.0
SW_Q = 2.0
SW_GAIN = 1.50          # 타격의 0.4 배쯤. 더 키우면 검처럼 들리고, 줄이면 안 들린다

# ② 젖은 타격 — 0.1s 의 그 순간
SLAP_F0 = 1700.0
SLAP_F1 = 430.0         # 급락. 채찍이 살에 박히는 소리다
SLAP_FALL = 0.045       # 이 시간에 걸쳐 떨어진다
SLAP_Q = 1.3
SLAP_ATK = 0.004        # 4ms. 검(0.25 비율)과 정반대로 날카롭게
SLAP_DEC = 0.050
# 🔴 타격층을 이만큼 **먼저** 시작한다. 어택 4ms + 밴드패스 군지연 + 물기층이 겹쳐
#    포락선 최대점이 뒤로 밀리기 때문이다. 실측으로 맞춘 값이라 위 상수를 고치면 다시 재야 한다
#    (스크립트가 `env peak err` 로 찍어 준다. 목표는 0.0 ms).
SLAP_LEAD = 0.0065
SLAP_GAIN = 2.60

# ③ 물기 — 타격 뒤 남는 저중역 흔들림. 없으면 그냥 마른 채찍이다
WET_LO = 500.0
WET_HI = 1400.0
WET_MOD = 0.70          # 진폭 변조 깊이. 0 이면 "물"이 사라진다
WET_MOD_HZ = 38.0       # 장판의 55Hz 보다 느리다 = 방울이 굵게 흔들린다
WET_DEC = 0.075
WET_GAIN = 0.60

HPF = 180.0
TARGET_PEAK = 0.38
CORR = 0.60

OUT = os.path.join("_Incoming", "Audio", "SFX_TentacleLash.wav")


def swish_layer(noise, n):
    """0 -> HIT_AT 에 걸쳐 올라오는 접근음. 타격 직전에 가장 크다."""
    t = np.linspace(0.0, DUR, n, endpoint=False)
    u = np.clip(t / HIT_AT, 0.0, 1.0)
    freqs = SW_F0 + (SW_F1 - SW_F0) * u ** 1.4
    sig = sweep_bandpass(noise, freqs, SW_Q)
    # 타격 시점에 딱 끊지 않고 조금 물린다. 끊으면 그 자리에 클릭이 생긴다
    env = (u ** 2.0) * np.exp(-np.clip(t - HIT_AT, 0, None) / 0.030)
    return sig * env


def slap_layer(noise, n):
    """HIT_AT 에서 시작하는 급락 스윕. 이 층이 전체 피크를 만든다."""
    t = np.linspace(0.0, DUR, n, endpoint=False)
    # 🔴 맞춰야 하는 건 소리가 **시작하는** 순간이 아니라 **가장 큰** 순간이다.
    ts = t - (HIT_AT - SLAP_LEAD)
    freqs = SLAP_F1 + (SLAP_F0 - SLAP_F1) * np.exp(-np.clip(ts, 0, None) / SLAP_FALL)
    sig = sweep_bandpass(noise, freqs, SLAP_Q)

    atk = np.clip(ts / SLAP_ATK, 0, 1)
    atk = atk * atk * (3.0 - 2.0 * atk)
    env = np.where(ts < 0, 0.0, atk * np.exp(-np.clip(ts, 0, None) / SLAP_DEC))
    return sig * env


def wet_layer(noise, n, rng):
    """타격 뒤 남는 물기. 불규칙 진폭 변조가 '젖음'을 만든다."""
    t = np.linspace(0.0, DUR, n, endpoint=False)
    ts = t - HIT_AT
    band = math.sqrt(WET_LO * WET_HI)
    sig = run_biquad(noise, biquad_bandpass(band, band / (WET_HI - WET_LO)))

    mod = smooth(rng.random(n), 1000.0 / WET_MOD_HZ)
    mod = (mod - mod.min()) / (mod.max() - mod.min() + 1e-9)
    sig = sig * (1.0 - WET_MOD + WET_MOD * mod)

    env = np.where(ts < 0, 0.0,
                   np.clip(ts / 0.006, 0, 1) * np.exp(-np.clip(ts, 0, None) / WET_DEC))
    return sig * env


def main():
    n = int(FS * DUR)
    rng = np.random.default_rng(20260830)

    ca = rng.normal(0.0, 1.0, n)
    cb = rng.normal(0.0, 1.0, n)
    cc = rng.normal(0.0, 1.0, n)

    chans = []
    for _ in range(2):
        na = CORR * ca + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        nb = CORR * cb + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        nc = CORR * cc + (1.0 - CORR) * rng.normal(0.0, 1.0, n)
        mix = (SW_GAIN * swish_layer(na, n)
               + SLAP_GAIN * slap_layer(nb, n)
               + WET_GAIN * wet_layer(nc, n, rng))
        chans.append(run_biquad(mix, biquad_highpass(HPF)))

    st = np.stack(chans, axis=1)
    tail = np.clip(np.linspace(1.0, 0.0, n) / 0.06, 0, 1)     # 끝 클릭 방지
    st *= tail[:, None]
    st *= TARGET_PEAK / np.abs(st).max()

    write_wav(OUT, st)
    peak_t = report(OUT, st)

    mono = st.mean(axis=1)
    # ⚠️ 접근음은 **타격이 시작되기 전**까지만 잰다. HIT_AT 까지 재면 이미 시작된
    #    타격층 앞부분이 섞여 들어와 접근음이 실제보다 크게 나온다.
    hit = int(FS * (HIT_AT - SLAP_LEAD))
    # 🔑 원 샘플의 최대점은 노이즈라 매번 몇 ms 씩 튄다. 귀가 듣는 건 포락선이므로
    #    8ms 이동평균의 최대점으로 잰다. 3ms 로 재면 이웃한 노이즈 봉우리 사이를 오가며
    #    ±5ms 씩 흔들려 **튜닝의 기준으로 못 쓴다.**
    env_t = float(np.argmax(smooth(np.abs(mono), 8.0))) / FS
    print("  hit at %.0f ms -> env peak err %+.1f ms  (허용 +-10ms. 그 아래는 귀로 못 가른다)"
          % (1000.0 * HIT_AT, 1000.0 * (env_t - HIT_AT)))
    print("  wind-up peak %.3f / slap peak %.3f  (접근음이 타격보다 작아야 한다)"
          % (np.abs(mono[:hit]).max(), np.abs(mono[hit:]).max()))
    print("  vs sword: 200-800Hz %.1f%% (sword 1%%)  vs toxin: len %.2fs (toxin 0.70s)"
          % (band_share(mono, 200, 800), DUR))


if __name__ == "__main__":
    main()
