# -*- coding: utf-8 -*-
"""
SFX 끼리 갈리는지 재는 자 — `python Tools/Audio/compare_sfx.py`

왜 필요한가
    새 소리를 만들 때마다 "기존 소리와 안 헷갈리나"를 손으로 따져 왔다.
    소리가 늘수록 짝이 제곱으로 늘어난다(무기 소리만 5종 = 10짝). 자동으로 잰다.

무엇을 재나 — 귀가 소리를 가르는 세 축
    1. **길이** — 0.22s 와 0.70s 는 절대 안 헷갈린다
    2. **어택** — 피크까지 걸리는 시간. 검은 일부러 느리게(총소리 방지), 촉수는 날카롭게
    3. **대역 분포** — 어느 주파수에 에너지가 몰렸나

    마지막에 짝마다 **거리**를 낸다. 작을수록 헷갈린다.
    ⚠️ 이건 참고용 지표지 판정이 아니다. **판정은 귀로만 한다.**
    숫자가 멀어도 들으면 비슷할 수 있다 — 이 자는 "가깝다"만 믿을 값이다.

실효 음량
    `AudioLibrary.asset` 의 `Volume` x 클립 모노 피크.
    프로젝트가 `forceToMono: 1` 이라 **모노 피크가 실제로 들리는 값**이다.
"""

import io
import math
import os
import re
import wave

import numpy as np

FS = 44100
LIB = os.path.join("Assets", "Game", "Audio", "AudioLibrary.asset")

# 서로 헷갈리면 곤란한 것들. (표시이름, wav 경로, SfxId)
TARGETS = [
    ("WeaponFire  (1)", "Assets/Game/Audio/SFX_WeaponFire.wav", 1),
    ("WeaponCast  (2)", "Assets/Game/Audio/SFX_WeaponCast.wav", 2),
    ("Explosion   (3)", "Assets/Game/Audio/SFX_Explosion.wav", 3),
    ("WeaponSwing (4)", "Assets/Game/Audio/SFX_WeaponSwing.wav", 4),
    ("ToxinSpill  (5)", "Assets/Game/Audio/SFX_ToxinSpill.wav", 5),
    # ⚠️ 이 둘은 `_Incoming/` 경로였다. DEV 가 `Assets/` 로 옮겨 배선하면서 표에서 빠져 있었다 —
    #    새 소리를 만들 때 **이미 있는 소리가 목록에서 사라지는** 게 이 자의 가장 위험한 고장이다.
    ("TentacleLash(6)", "Assets/Game/Audio/SFX_TentacleLash.wav", 6),
    ("DragonSpit  (7)", "Assets/Game/Audio/SFX_DragonSpit.wav", 7),
    ("EnemyHit   (10)", "Assets/Game/Audio/SFX_EnemyHit.wav", 10),
    # ── 픽업 무리 (C23 에서 추가) ──────────────────────────────────────────────
    # 🔴 원래 이 목록은 **무기 소리만** 보고 있었다. 그래서 픽업 3종을 만들 때
    #    정작 부딪힐 상대(XpPickup·Magnet·LevelUp·Heal)가 표에 없었다.
    #    픽업끼리는 짧은 시간에 **연달아** 나므로 무기보다 더 갈려 있어야 한다.
    ("XpPickup   (20)", "Assets/Game/Audio/SFX_XpPickup.wav", 20),
    ("LevelUp    (21)", "Assets/Game/Audio/SFX_LevelUp.wav", 21),
    ("ChestOpen  (22)", "Assets/Game/Audio/SFX_ChestOpen.wav", 22),
    ("Magnet     (23)", "Assets/Game/Audio/SFX_Magnet.wav", 23),
    ("Heal       (24)", "Assets/Game/Audio/SFX_Heal.wav", 24),
    ("BombPickup (25)", "_Incoming/Audio/SFX_BombPickup.wav", 25),
    ("BuffPickup (26)", "_Incoming/Audio/SFX_BuffPickup.wav", 26),
    ("GoldPickup (27)", "_Incoming/Audio/SFX_GoldPickup.wav", 27),
    ("UiSelect   (40)", "Assets/Game/Audio/SFX_UiSelect.wav", 40),
]

BANDS = [(0, 200), (200, 800), (800, 3000), (3000, 9000), (9000, 22050)]


def read_mono(path):
    with wave.open(path, "rb") as w:
        n, ch = w.getnframes(), w.getnchannels()
        raw = np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float64) / 32768.0
    return raw.reshape(-1, ch).mean(axis=1) if ch > 1 else raw


def volumes():
    """AudioLibrary.asset 에서 Id -> Volume. 정규식이지 YAML 파서가 아니다 (읽기 전용)."""
    if not os.path.exists(LIB):
        return {}
    txt = io.open(LIB, encoding="utf-8").read()
    out = {}
    for m in re.finditer(r"- Id: (\d+)\s*\n(?:.*\n)?\s*Volume: ([\d.]+)", txt):
        out.setdefault(int(m.group(1)), float(m.group(2)))
    return out


def profile(mono):
    spec = np.abs(np.fft.rfft(mono)) ** 2
    freq = np.fft.rfftfreq(len(mono), 1.0 / FS)
    total = spec.sum() or 1.0
    shares = [spec[(freq >= lo) & (freq < hi)].sum() / total for lo, hi in BANDS]

    env = np.convolve(np.abs(mono), np.ones(int(FS * 0.008)) / (FS * 0.008), mode="same")
    return {
        "dur": len(mono) / FS,
        "peak": float(np.abs(mono).max()),
        "atk": float(np.argmax(env)) / FS,
        "bands": np.array(shares),
    }


def main():
    vol = volumes()
    rows = []
    for name, path, sid in TARGETS:
        if not os.path.exists(path):
            print("  (없음) %s" % path)
            continue
        p = profile(read_mono(path))
        p["name"], p["vol"] = name, vol.get(sid)
        rows.append(p)

    print("%-16s %6s %6s %7s | %5s %5s %5s %5s | %6s %7s"
          % ("clip", "len", "peak", "atk_ms", "0-200", "-800", "-3k", "-9k", "Vol", "eff"))
    for p in rows:
        v = p["vol"]
        eff = "  -  " if v is None else "%.3f" % (v * p["peak"])
        print("%-16s %5.3fs %6.3f %7.1f | %4.0f%% %4.0f%% %4.0f%% %4.0f%% | %6s %7s"
              % (p["name"], p["dur"], p["peak"], 1000.0 * p["atk"],
                 *(100.0 * p["bands"][:4]),
                 "-" if v is None else "%.2f" % v, eff))

    # 짝 거리 — 길이·어택·대역을 각각 정규화해서 유클리드로 합친다
    print("\n가까운 짝 (작을수록 헷갈린다. 1.0 미만은 들어 보고 확인할 것)")
    pairs = []
    for i in range(len(rows)):
        for j in range(i + 1, len(rows)):
            a, b = rows[i], rows[j]
            d_len = abs(math.log(a["dur"] / b["dur"]))            # 배수로 본다
            d_atk = abs(math.log((a["atk"] + 0.003) / (b["atk"] + 0.003)))
            d_bnd = float(np.linalg.norm(a["bands"] - b["bands"])) * 2.0
            pairs.append((math.sqrt(d_len ** 2 + d_atk ** 2 + d_bnd ** 2),
                          a["name"], b["name"], d_len, d_atk, d_bnd))
    for d, an, bn, dl, da, db in sorted(pairs)[:8]:
        print("  %5.2f  %s  vs  %s   (len %.2f / atk %.2f / band %.2f)"
              % (d, an, bn, dl, da, db))


if __name__ == "__main__":
    main()
