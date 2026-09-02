"""보스 내려찍기 예고 원 (C30).

왜 전용 스프라이트가 필요한가
  기존에는 ToxinField.png 를 붉게 틴트해 썼는데 모양이 원이 아니라 블롭이라
  **보이는 반경과 피해 반경이 달랐다.** C30 이 이 기술을 "정확히 피하는" 것으로
  다시 잡았으므로(SlamWindup 1.15 / SlamRadius 2.4) 보이는 것이 판정과 같아야 한다.

규격 — BossSlam.spriteRadiusAtScaleOne = 0.5 에 맞춘다
  512x512 · PPU 512 -> 화면상 1.0 유닛 폭 = 반경 0.5. 프리팹 필드를 안 고쳐도 된다.
  RGB 는 전부 흰색이다. BossSlam 이 telegraph.color 를 통째로 덮어쓰므로
  (warnColor -> burstColor) 색은 코드가 정하고 텍스처는 **알파로 모양만** 준다.

  알파 구성 : 바깥 링 굵게(1.0) + 안쪽 옅은 채움(0.30) + 조준선 4개
  안티에일리어싱은 서브픽셀 4x4 슈퍼샘플링으로 준다 — Point 필터가 아니라
  Bilinear 로 임포트될 물건이라 계단이 그대로 보인다.
"""
from PIL import Image
import math, os

N   = 512          # 텍스처 한 변
SS  = 4            # 슈퍼샘플링 배수
R   = 0.98         # 원 반경 (반지름 1.0 = 텍스처 절반). 살짝 안쪽에서 끝내 잘림 방지
RING_W  = 0.085    # 링 두께 (반지름 비율)
FILL_A  = 0.30     # 안쪽 채움 알파
RING_A  = 1.00     # 링 알파
TICK_W  = 0.030    # 조준선 두께 (반지름 비율)
TICK_IN = 0.62     # 조준선 시작 지점

def alpha_at(x, y):
    """중심 기준 정규화 좌표(-1..1)에서 알파를 낸다."""
    d = math.hypot(x, y)
    if d > R:
        return 0.0
    # 바깥 링
    if d >= R - RING_W:
        return RING_A
    a = FILL_A
    # 조준선 4개 — 어두운 타일 위에서 원이 안 보일 때 방향을 잡아 준다
    if d >= R * TICK_IN and (abs(x) <= TICK_W or abs(y) <= TICK_W):
        a = max(a, 0.75)
    return a

def build():
    img = Image.new("RGBA", (N, N), (255, 255, 255, 0))
    px  = img.load()
    half = N / 2.0
    step = 1.0 / SS
    inv  = 1.0 / (SS * SS)
    for j in range(N):
        for i in range(N):
            acc = 0.0
            for sj in range(SS):
                for si in range(SS):
                    x = ((i + (si + 0.5) * step) - half) / half
                    y = ((j + (sj + 0.5) * step) - half) / half
                    acc += alpha_at(x, y)
            px[i, j] = (255, 255, 255, int(round(acc * inv * 255)))
    return img

if __name__ == "__main__":
    out = "Assets/Game/Sprites/Effects/BossSlamRing.png"
    os.makedirs(os.path.dirname(out), exist_ok=True)
    im = build()
    im.save(out)
    a = im.getchannel("A")
    print(f"{out}  {im.size}  alpha {a.getextrema()}")
