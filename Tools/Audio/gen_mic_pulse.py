# -*- coding: utf-8 -*-
"""
마이크가 지르는 소리 — Assets/Game/Audio/SFX_MicPulse.wav  (SfxId 8)

무엇인가
    D112 가 넣은 마이크(원뿔 범위 근접). 쿨다운 1.1~0.7초라 **판 내내 계속 난다.**
    지금은 `WeaponSwing`(검의 휘두름)을 빌려 쓰는데, 그건 **칼바람 소리**다 —
    마이크를 들고 있는데 칼 소리가 나면 무엇을 쓰고 있는지 귀로 알 수 없다.

🔴 검과 **정반대 대역**으로 간다 — 여기가 이 소리의 전부다
    | | 길이 | 어택 | 대역 |
    |---|---|---|---|
    | `WeaponSwing` (4) | 0.280s | 63.3ms | 3k~9k **83%** (전부 공기) |
    | 🔑 **이 소리** | **0.32s** | **~6ms** | 0~200 + 200~800 이 **몸** |

    검은 위(공기)에 있고 마이크는 아래(몸통)에 있다. **같은 무기 칸에서 번갈아 나도 안 헷갈린다.**

가까이 있는 것들과 어떻게 갈리나
    · `WeaponFire`(1) 6/53/20/16 — 대역이 가장 닮았다. **어택으로 가른다**(69ms vs 6ms).
      총은 "탕-" 하고 뜸을 들이고 이건 "웁!" 하고 즉시 터진다.
    · `Magnet`(23) 33/43/15/5 — 대역이 거의 같다. **길이와 어택이 극단으로 다르다**
      (0.685s / 242ms vs 0.32s / 6ms). 하나는 서서히 빨려들고 하나는 때린다.
    · `Explosion`(3) 80/13/6/1 — 저역 덩어리. 여기는 저역을 **30% 넘게 안 준다**
      (`gen_dragon_spit.py` 가 폭발에게 저역을 넘긴 것과 같은 규칙).

"확성기"로 들리게 하는 것
    1. **포먼트 스윕** 380 -> 900Hz. 사람 목소리의 "우~아" 가 이 움직임이다.
       고정 밴드패스로 두면 그냥 북소리가 된다.
    2. **하울링 꼬리** — 1.6kHz 근처의 얇은 사인이 뒤에 살짝 남는다.
       확성기의 정체는 되먹임이다. 없으면 그냥 낮은 펄스다.
    3. **짧은 클릭** — 스위치가 들어가는 순간. 어택을 6ms 로 만드는 것이 이것이다.

실행:  python Tools/Audio/gen_mic_pulse.py
"""

import math
import os

import numpy as np

from dsp import (FS, biquad_bandpass, biquad_highpass, report, run_biquad,
                 smooth, sweep_bandpass, write_wav)

DUR = 0.32

# ① 몸통 — 포먼트가 올라간다. 🔑 이 움직임이 "목소리"다
BODY_F0, BODY_F1 = 280.0, 620.0
BODY_Q = 1.15
BODY_DEC = 0.085
BODY_GAIN = 1.00

# ② 저역 — 확성기 콘이 밀어내는 공기. 🔴 30% 를 넘기지 않는다 (Explosion 자리다)
SUB_F0, SUB_F1 = 150.0, 92.0            # 살짝 떨어진다 = 밀려 나간 뒤 멎는다
SUB_DEC = 0.070
SUB_GAIN = 0.045

# ③ 하울링 꼬리 — 확성기의 정체. 뒤늦게 들어와 짧게 운다
HOWL_F = 1280.0
HOWL_AT = 0.055
HOWL_DEC = 0.075
HOWL_GAIN = 0.035

# ④ 스위치 클릭 — 어택을 만든다
CLICK_LO, CLICK_HI = 1800.0, 6000.0
CLICK_DEC = 0.0040
CLICK_GAIN = 0.42

# 🔴 이득을 눈대중으로 잡았다가 두 번 틀렸다 — 둘 다 `compare_sfx.py` 가 잡았다.
#    ① 첫 판 **0~200Hz 81%** — Explosion 자리를 통째로 먹었다.
#       사인(저역·하울링)은 결맞음이라 같은 진폭에서 잡음보다 에너지가 수십 배다.
#       층마다 rms 를 따로 재서 `g = sqrt(목표비율) / rms` 로 다시 잡았다
#       (실측 rms: body 0.0396 · sub 0.2339 · howl 0.2420 · click 0.0144).
#    ② 두 번째 판 31/33/28/7 — 보기엔 고르게 퍼졌는데 **`EnemyHit`(34/32/24/10) 과 거리 0.79** 로
#       목록 전체에서 가장 가까운 짝이 됐다(대역 거리 **0.12**). 적 타격음은 **끊임없이** 난다.
#    ⇒ 🔑 **200~800Hz 를 중심으로 밀어 올렸다.** 거기 있는 것들(`UiCancel` 0.52s/75ms ·
#       `WeaponFire` 0.45s/69ms · `LevelUp` 1.02s/231ms)은 전부 **길고 느리다** —
#       짧고(0.32s) 빠른(4ms) 소리가 하나도 없는 자리다.

HPF = 55.0                              # DC 와 들리지 않는 초저역을 뺀다
TARGET_PEAK = 0.42

OUT = os.path.join("Assets", "Game", "Audio", "SFX_MicPulse.wav")


def band_noise(noise, lo, hi):
    c = math.sqrt(lo * hi)
    return run_biquad(noise, biquad_bandpass(c, c / (hi - lo)))


def main():
    n = int(FS * DUR)
    t = np.linspace(0.0, DUR, n, endpoint=False)
    rng = np.random.default_rng(20260908)
    noise = rng.uniform(-1.0, 1.0, n)

    # ① 몸통 — 잡음을 포먼트 스윕에 통과시킨다
    k = np.clip(t / 0.11, 0.0, 1.0) ** 0.6          # 앞이 빠르게 오른다
    freqs = smooth(BODY_F0 + (BODY_F1 - BODY_F0) * k, 3.0)
    body = sweep_bandpass(noise, freqs, BODY_Q) * np.exp(-t / BODY_DEC) * BODY_GAIN

    # ② 저역 — 사인 하나. 잡음으로 만들면 폭발이 된다
    sub_f = SUB_F0 + (SUB_F1 - SUB_F0) * np.clip(t / DUR, 0, 1)
    phase = 2.0 * math.pi * np.cumsum(sub_f) / FS
    sub = np.sin(phase) * np.exp(-t / SUB_DEC) * SUB_GAIN

    # ③ 하울링 — 늦게 들어온다
    late = np.clip(t - HOWL_AT, 0.0, None)
    howl = (np.sin(2.0 * math.pi * HOWL_F * late)
            * np.exp(-late / HOWL_DEC) * (t >= HOWL_AT) * HOWL_GAIN)

    # ④ 클릭
    click = band_noise(noise, CLICK_LO, CLICK_HI) * np.exp(-t / CLICK_DEC) * CLICK_GAIN

    mono = body + sub + howl + click
    mono = run_biquad(mono, biquad_highpass(HPF))

    # 🔴 프로젝트가 forceToMono 라 모노 피크로 맞춘다 (dsp.py 머리말)
    mono *= TARGET_PEAK / max(1e-9, np.abs(mono).max())

    # 아주 얕은 스테레오 — 확성기는 한 점에서 난다. 넓히면 위치감이 사라진다
    stereo = np.stack([mono, mono * 0.97], axis=1)
    write_wav(OUT, stereo)
    report(OUT, stereo)


if __name__ == "__main__":
    main()
