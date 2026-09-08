"""마이크의 음파 원뿔 6프레임 (D115).

왜 만드나
  D112 가 마이크를 넣을 때 이펙트를 `Fx_SwingArc`(검격) 복제 + 보라 틴트로 뒀다.
  색으로 구분은 되지만 **모양이 초승달 검격**이라 "소리가 앞으로 퍼진다"로 안 읽힌다.

🔴 SwingArc 와 같은 약속을 지킨다 (SwingArcFx 가 그 약속 위에 서 있다)
  · 시트 1536x256 = 256 칸 6개 (Multiple · 왼쪽부터 프레임 0~5)
  · PPU 100 · 피벗 (0.5, 0.5) · alignment 0(Center)
  · **회전 중심이 칸 중앙이고 그림은 오른쪽 반쪽에만** 있다.
    그래야 오브젝트를 플레이어 자리에 놓고 z 만 돌리면 그 방향을 치는 그림이 된다.
  · **흰색으로만 그린다.** 색은 프리팹의 SpriteRenderer.color 가 정한다
    (gen_fx.py 와 같은 방식 — 같은 그림을 다른 색으로 돌려 쓸 수 있게).

🔴 검격과 반대로 그린다 — 그게 이 그림의 존재 이유다
  검격 : 한 덩어리 초승달이 **호를 따라 쓸고 지나간다**(옆으로 움직인다).
  음파 : 여러 겹의 띠가 **중심에서 바깥으로 밀려 나간다**(앞으로 움직인다).
  ⇒ 프레임이 넘어갈 때 눈이 따라가는 방향이 다르다.

각도
  MeleeWeapon 의 halfAngle 과 **같은 값이라야 그림이 사거리를 속이지 않는다.**
  Weapon_Mic.prefab 의 halfAngle 이 32 이므로 여기도 32 다.
  🔴 프리팹에서 각을 바꾸면 이 값도 같이 바꿔 다시 구울 것.

반지름
  바깥 끝 118px / PPU 100 = **1.18 유닛**.
  🔴 띠 두께(BAND_W)만큼 더 나가므로 칸(128px)에 여유를 남겨야 한다 —
     118 x 1.075 = 126.9 < 128. 124 로 잡았더니 마지막 프레임이 칸에 걸렸다.
  SwingArcFx.spriteRadiusAtScaleOne 에 이 값을 넣어야 호 끝이 정확히 사거리에 닿는다.
  (SwingArc 는 107.4px 라 1.074 였다 — 원뿔은 좁아서 더 길게 뽑을 수 있다)
"""
from PIL import Image
import math, os

CELL, FRAMES, SS = 256, 6, 3
HALF = CELL / 2.0
R_OUT_PX = 118.0
HALF_ANGLE = 32.0                 # 🔴 Weapon_Mic.prefab 의 halfAngle 과 같아야 한다

BAND_W   = 0.075                  # 띠 두께 (반지름 정규화 · 1.0 = 바깥 끝)
BAND_GAP = 0.17                   # 프레임당 띠가 나아가는 거리
R_START  = 0.16                   # 새 띠가 태어나는 반지름 (마이크 머리 언저리)
WEDGE_A  = 0.13                   # 부채꼴 바탕의 옅은 빛

OUT = os.path.join(os.path.dirname(__file__), "..", "..",
                   "Assets", "Game", "Sprites", "Effects", "SoundCone.png")


def smooth(t):
    """0~1 를 부드럽게. 띠 가장자리와 각도 가장자리를 계단이 아니게 만든다."""
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def alpha_at(k, x, y):
    """칸 좌표(-1~1, 바깥 끝이 1)에서의 알파. k 는 프레임 번호."""
    r = math.hypot(x, y)
    if r > 1.0 or r < 0.02:
        return 0.0

    # 🔴 오른쪽 반쪽만. x <= 0 이면 각도가 90도를 넘으므로 아래 판정에서 걸린다.
    ang = abs(math.degrees(math.atan2(y, x)))
    if ang > HALF_ANGLE:
        return 0.0

    # 각도 가장자리를 부드럽게 — 칼로 자르면 부채꼴이 "잘린 파이"로 읽힌다.
    edge = smooth((HALF_ANGLE - ang) / (HALF_ANGLE * 0.42))

    # 🔴 바깥 끝도 부드럽게. 안 하면 마지막 프레임의 띠가 r=1.0 에서 **칼같이 잘려**
    #    소리가 퍼지다 만 게 아니라 "잘린 그림"으로 보인다 (첫 판이 그랬다).
    edge *= smooth((1.0 - r) / 0.11)

    # ── 바탕 부채꼴: 안쪽이 밝고 바깥으로 사라진다 (닿는 범위를 알려 준다)
    a = WEDGE_A * edge * (1.0 - smooth(r)) * smooth(r / 0.25)

    # ── 파면: 프레임 j 에 태어난 띠가 바깥으로 밀려 나간다
    for j in range(k + 1):
        rc = R_START + BAND_GAP * (k - j)
        if rc - BAND_W > 1.0:
            continue                       # 화면 밖으로 나갔다
        d = abs(r - rc)
        if d > BAND_W:
            continue
        band = smooth(1.0 - d / BAND_W)
        # 멀리 갈수록 옅어진다. 소리는 퍼지면서 약해진다.
        fade = 1.0 - 0.55 * smooth(rc)
        a += band * fade * edge

    return min(1.0, a)


def main():
    sheet = Image.new("RGBA", (CELL * FRAMES, CELL), (0, 0, 0, 0))
    step, inv = 1.0 / SS, 1.0 / (SS * SS)

    for k in range(FRAMES):
        cell = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
        px = cell.load()
        for j in range(CELL):
            for i in range(CELL):
                A = 0.0
                for sj in range(SS):
                    for si in range(SS):
                        # 🔴 y 를 뒤집는다 — 이미지 j 는 아래로 늘지만 월드 y 는 위로 는다.
                        x = ((i + (si + .5) * step) - HALF) / R_OUT_PX
                        y = (HALF - (j + (sj + .5) * step)) / R_OUT_PX
                        A += alpha_at(k, x, y)
                a = A * inv
                if a > 0.003:
                    px[i, j] = (255, 255, 255, min(255, int(a * 255 + .5)))
        sheet.paste(cell, (CELL * k, 0))
        lo, hi = cell.getchannel("A").getextrema()
        print(f"  frame {k}  alpha {lo}~{hi}")

    sheet.save(os.path.abspath(OUT))
    lo, hi = sheet.getchannel("A").getextrema()
    print(f"{os.path.basename(OUT)}  {sheet.size}  alpha {lo}~{hi}  "
          f"(바깥 반지름 {R_OUT_PX}px / PPU 100 = {R_OUT_PX/100:.2f} 유닛)")


if __name__ == "__main__":
    main()
