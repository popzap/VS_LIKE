# -*- coding: utf-8 -*-
"""기습 이벤트 스팅어 — SFX_AmbushStart (C48).

왜 만드나
    사용자가 기습 이벤트를 만나고도 **"이벤트인지 몰랐어"** 라고 했다 (`PLAYTEST_2` 2-G).
    데이터는 요구대로다 — `Ambush1` 은 35초에 100마리를 사방에서 쏟고
    사용자도 *"짧고 빽빽했다"* 고 답했다. **그게 특별한 일이라는 신호가 없었을 뿐**이다.

🔴 왜 35초 BGM 이 아니라 2.4초 스팅어인가
    불만이 *"몰랐다"* 다. 그건 **인지(recognition)** 문제고, 인지는 **시작 순간**에 결정된다.
    35초 곡은 *분위기*를 더하는 것이라 그다음이다. DEV 가 *"효과의 절반"* 이라고 했는데
    이 불만에 한해서는 **절반이 아니라 정답 쪽**이라고 본다. (곡은 마감 뒤에)

설계 — "즉시 다르다"를 첫 20ms 안에 만든다
    ① t=0 타격      : 첫 프레임부터 소리가 있다. 페이드인이 있으면 이미 늦는다
    ② 불협 2음      : 110Hz 와 그 **삼전음**(155.6Hz). 협화음이 아니라 **불안**을 만든다
    ③ 상승 스윕     : 300 -> 2200Hz. "다가온다"를 만드는 유일한 층
    ④ 저역 럼블     : 55Hz. 화면 밖에서 몰려오는 무게

🔴 BossAppear(33) 와 갈려야 한다
    둘 다 "사건 알림"이라 자리가 가깝다. 대조 결과는 스크립트 끝에서 찍는다.

규격 — 기존 SFX 실측과 같게
    44100Hz · 16bit · 스테레오 (프로젝트가 forceToMono 로 접으므로 **모노 피크로 검산**한다)
"""
import numpy as np
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dsp import FS, write_wav, report, smooth, sweep_bandpass, biquad_highpass, run_biquad

DUR = 2.40


def env(t, attack, decay, hold=0.0):
    """어택-홀드-감쇠. 어택이 0 이면 첫 샘플부터 최대다."""
    e = np.ones_like(t)
    if attack > 0:
        e = np.minimum(e, t / attack)
    tail = np.clip((t - attack - hold) / max(decay, 1e-6), 0.0, 1.0)
    return np.clip(e, 0, 1) * (1.0 - tail) ** 1.6


def build():
    n = int(DUR * FS)
    t = np.arange(n) / FS
    rng = np.random.default_rng(20260905)

    # 🔴 대역을 BossAppear 와 갈랐다 (첫 판에서 둘 다 저역 92~98 % 로 겹쳤다)
    #    보스 등장 = **무게**(저역 98.5 %) · 기습 = **경보**(중고역).
    #    그래서 아래 층들은 전부 한 옥타브 이상 위로 올렸고 럼블은 잘라냈다.
    #    뜻도 그쪽이 맞다 — 경보는 높고, 재앙은 낮다.

    # ① t=0 타격 — 어택 0. 첫 샘플부터 최대다
    hit = (np.sin(2 * np.pi * 520 * t * np.exp(-t * 7.0))
           + 0.9 * rng.normal(0, 1, n))
    hit *= env(t, 0.0, 0.22)
    hit = run_biquad(hit, biquad_highpass(400))

    # ② 불협 2음 — 440Hz 와 그 삼전음 622.25Hz. 경보의 뼈대다
    dis = np.zeros(n)
    for f, a in ((440.0, 0.60), (622.25, 0.55), (880.0, 0.30), (1244.5, 0.22)):
        dis += a * np.sin(2 * np.pi * f * t)
        dis += a * 0.45 * np.sin(2 * np.pi * (f * 1.008) * t)   # 디튠 -> 맥놀이
    # 사이렌처럼 흔든다 — 고정음이면 "삐-" 로만 들린다
    dis *= (0.75 + 0.25 * np.sin(2 * np.pi * 5.5 * t))
    dis *= env(t, 0.015, 0.55, hold=1.4) * 0.55

    # ③ 상승 스윕 — "다가온다". 이번 판에서 가장 크게 잡는다
    noise = rng.normal(0, 1, n)
    k = (t / DUR) ** 0.7
    sweep = sweep_bandpass(noise, 700 + (5200 - 700) * k, q=1.8)
    sweep *= (0.25 + 0.85 * k) * env(t, 0.02, 0.28, hold=1.9)

    # ④ 저역은 **받침만** — 무게를 주면 BossAppear 와 겹친다
    body = np.sin(2 * np.pi * 165 * t) * env(t, 0.01, 0.45, hold=1.2) * 0.28

    mono = 0.85 * hit + 1.00 * dis + 1.15 * sweep + 0.35 * body
    mono = run_biquad(mono, biquad_highpass(230))     # 저역을 확실히 비운다
    mono = smooth(mono, 1.0)
    mono /= np.abs(mono).max() / 0.94

    left = mono + 0.07 * np.roll(sweep, 110)
    right = mono - 0.07 * np.roll(sweep, 110)
    st = np.stack([left, right], axis=1)
    st /= np.abs(st).max() / 0.95
    return st


if __name__ == "__main__":
    out = "Assets/Game/Audio/SFX_AmbushStart.wav"
    st = build()
    write_wav(out, st)
    report(out, st)

    ref = "Assets/Game/Audio/SFX_BossAppear.wav"
    if os.path.exists(ref):
        import wave
        with wave.open(ref, "rb") as w:
            a = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2")
            a = a.reshape(-1, w.getnchannels()) / 32768.0
        print("\n-- reference (must be distinguishable) --")
        report(ref, a if a.shape[1] == 2 else np.repeat(a, 2, axis=1))
