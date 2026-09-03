"""UI 9-slice 프레임 3종 — 패널 · 카드 · 버튼 (C33).

왜 만드나
  씬의 Image 컴포넌트 60개 중 59개가 스프라이트 없이 단색이다 (DESIGN_ART.md §0).
  화면을 하나씩 그리는 대신 **9-slice 프레임 3종**을 만들면
  패널·카드·버튼이 전부 이걸 늘려 쓰므로 **한 번에 화면 6개가 바뀐다.**

🔴 9-slice 가 성립하는 조건 — 이 스크립트의 설계 전부가 여기서 나온다
  가장자리는 **늘어나고** 모서리는 안 늘어난다. 그러려면
    · 각 변의 단면이 **변을 따라 일정**해야 하고
    · 가운데가 **균일**해야 한다.
  그래서 색·알파를 오직 **"바깥 경계에서 안쪽으로 들어온 거리 d"** 의 함수로만 정한다.
  둥근 모서리 반지름은 border 안에 들어가야 한다 (radius <= border).

색 배분 — 틴트를 견디게
  Image.color 는 텍스처에 **곱해진다.** 그래서 명암을 알파가 아니라 **RGB** 로 준다.
  알파로만 주면 어두운 색으로 틴트했을 때 테두리와 안쪽이 구분되지 않는다.
    테두리 1.00 (가장 밝다) / 홈 0.42 (어두운 분리선) / 안쪽 하이라이트 0.72 / 채움 0.30
  ⇒ 어떤 색으로 칠하든 **밝은 테두리 + 어두운 판**이 유지된다.
"""
from PIL import Image
import os

SS = 4   # 슈퍼샘플링

def rr_depth(x, y, w, h, r):
    """둥근 사각형 바깥 경계에서 **안쪽으로** 들어온 거리(px). 밖이면 음수."""
    # 모서리 원 중심까지의 거리로 환산한다
    dx = max(r - x, x - (w - r), 0.0)
    dy = max(r - y, y - (h - r), 0.0)
    if dx > 0.0 and dy > 0.0:                 # 모서리 영역
        return r - (dx * dx + dy * dy) ** 0.5
    # 변 영역 — 가장 가까운 변까지의 거리
    return min(x, y, w - x, h - y)

def build(path, N, border, radius, out_w, groove_w, hi_w):
    """N x N 텍스처. border 는 9-slice 경계(px), radius 는 모서리 반지름."""
    assert radius <= border, "모서리 반지름이 border 를 넘으면 9-slice 가 깨진다"
    assert out_w + groove_w + hi_w < border, "테두리 구성이 border 를 넘는다"

    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    px, step, inv = img.load(), 1.0 / SS, 1.0 / (SS * SS)

    def sample(d):
        """경계에서 d px 안쪽일 때의 (v, a). v 는 RGB 밝기."""
        if d <= 0.0:
            return 0.0, 0.0
        if d <= out_w:                       # 바깥 테두리 — 가장 밝다
            return 1.00, 1.0
        if d <= out_w + groove_w:            # 홈 — 어두운 분리선
            return 0.42, 1.0
        if d <= out_w + groove_w + hi_w:     # 안쪽 하이라이트
            return 0.72, 1.0
        return 0.30, 0.92                    # 채움 — 어둡고 살짝 비친다

    for j in range(N):
        for i in range(N):
            V = A = 0.0
            for sj in range(SS):
                for si in range(SS):
                    d = rr_depth(i + (si + .5) * step, j + (sj + .5) * step, N, N, radius)
                    v, a = sample(d)
                    V += v * a; A += a
            if A > 1e-6:
                c = min(255, int(V / A * 255 + .5))
                px[i, j] = (c, c, c, min(255, int(A * inv * 255 + .5)))
    img.save(path)
    a = img.getchannel("A")
    print(f"{os.path.basename(path):16s} {N}x{N}  border {border}  alpha {a.getextrema()}")

if __name__ == "__main__":
    out = "Assets/Game/Sprites/UI"
    os.makedirs(out, exist_ok=True)
    #      파일                     N    border radius out groove hi
    build(f"{out}/UI_Panel.png",   128,   40,    26,    9,   4,    4)
    build(f"{out}/UI_Card.png",     96,   28,    18,    6,   3,    3)
    build(f"{out}/UI_Button.png",   64,   20,    14,    4,   2,    3)

    # 콘솔이 cp949 라 이모지를 못 찍는다 (한 번 밟았다). stdout 은 ASCII 만 쓴다.
    print("[!] spriteBorder (L,B,R,T) -- importer must set these or 9-slice breaks")
    print("    UI_Panel 40,40,40,40 / UI_Card 28,28,28,28 / UI_Button 20,20,20,20")
