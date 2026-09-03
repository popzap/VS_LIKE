"""연출용 그림 2장 — 충격파 링 · 폭발 별 (C34).

왜 만드나
  ① 승급(Promotion)은 이 게임에서 가장 큰 성취인데 **연출이 0** 이다.
     EvolutionManager 에 이펙트·파티클·흔들림 호출이 하나도 없다.
     보스 등장에는 화면 흔들림이 있고 엘리트 처치에는 히트스톱이 있는데
     승급에는 아무것도 없다 — 비중이 거꾸로다.
  ② 레벨업 슬롯이 비어 있다. HUDManager.cs:72 의 levelUpEffect 가 {fileID: 0} 이다.

🔴 두 연출에 그림을 따로 만들지 않는다
  levelUpEffect 는 Sprite 가 아니라 **GameObject**(프리팹)라 만드는 건 DEV 몫이고,
  나는 그 프리팹이 쓸 **그림**만 댄다. 그런데 승급과 레벨업은 같은 모양이면 된다 —
  **크기와 색만 다르면** 구분된다 (승급 = 크고 금색, 레벨업 = 작고 청록).
  그래서 링 1장 + 별 1장을 **흰색**으로 만들고 색은 프리팹이 정한다.
  UI 프레임(C33)·예고 원(C30)에서 쓴 것과 같은 방식이다.

🔴 BossSlamRing.png 을 재사용하지 않는 이유
  그건 **바닥 예고**라 테두리가 굵고 안쪽이 차 있고 조준 눈금이 있다.
  피해 범위를 보여 주는 물건이라 "퍼져 나가는 느낌"이 없다.
  여기 링은 반대다 — 얇고, 안쪽이 비고, 바깥이 날카롭다.

규격 (BossSlamRing 과 같은 약속)
  512x512 · PPU 512 -> 폭 1.0 유닛 = **반경 0.5**.
  프리팹에서 반경 r 로 쓰려면 localScale = r / 0.5 다.
  Bilinear — 픽셀아트가 아니라 빛이다. Point 로 두면 테두리가 계단이 된다.
"""
from PIL import Image
import math, os

N, SS = 512, 4

def render(fn, path):
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    px, half, step, inv = img.load(), N / 2.0, 1.0 / SS, 1.0 / (SS * SS)
    for j in range(N):
        for i in range(N):
            A = 0.0
            for sj in range(SS):
                for si in range(SS):
                    x = ((i + (si + .5) * step) - half) / half
                    y = ((j + (sj + .5) * step) - half) / half
                    A += fn(x, y)
            a = A * inv
            if a > 0.002:
                px[i, j] = (255, 255, 255, min(255, int(a * 255 + .5)))
    img.save(path)
    print(f"{os.path.basename(path):16s} {N}x{N}  alpha {img.getchannel('A').getextrema()}")

# ── 충격파 링 ────────────────────────────────────────────────────
#
# 바깥은 칼같이 끊고 안쪽으로만 부드럽게 흐린다. 그래야 "퍼져 나간다"로 읽힌다.
# (양쪽을 다 흐리면 그냥 도넛이 된다)

# 🔴 띠가 두꺼우면 "덩어리"로 읽힌다. 첫 판을 0.62~0.97(폭 0.35)로 잡았더니
#    퍼져 나가는 테두리가 아니라 뭉친 원반이 됐다. 폭을 1/3 로 줄였다.
R_OUT, R_IN = 0.99, 0.87
EDGE = 0.012                              # 바깥 끝의 칼같은 흰 선

def ring(x, y):
    d = math.hypot(x, y)
    if d > R_OUT or d < R_IN:
        return 0.0
    if d > R_OUT - EDGE:                  # 가장 바깥 — 완전 불투명
        return 1.0
    k = (d - R_IN) / (R_OUT - EDGE - R_IN)   # 0(안) ~ 1(바깥)
    return k ** 1.6

# ── 폭발 별 ──────────────────────────────────────────────────────
#
# 가는 광선 12개 + 중심 코어. 광선 끝으로 갈수록 투명해진다.

RAYS = 12

def burst(x, y):
    d = math.hypot(x, y)
    if d > 1.0:
        return 0.0
    core = max(0.0, 1.0 - (d / 0.20) ** 2)          # 가운데 밝은 덩어리
    th = math.atan2(y, x)
    s = abs(math.cos(RAYS * th * 0.5)) ** 22        # 뾰족한 광선
    # 길이를 갈라 준다 — 전부 같은 길이면 톱니바퀴처럼 보인다
    s *= 0.55 + 0.45 * abs(math.cos(RAYS * th * 0.25 + 0.7))
    ray = s * (1.0 - d) ** 1.5
    return min(1.0, core + ray * 1.35)

if __name__ == "__main__":
    out = "Assets/Game/Sprites/Effects"
    os.makedirs(out, exist_ok=True)
    render(ring,  f"{out}/Fx_ShockRing.png")
    render(burst, f"{out}/Fx_Burst.png")
