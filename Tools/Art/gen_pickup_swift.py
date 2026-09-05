"""이동속도 픽업 아이콘 — Pickup_Swift (C44).

왜 만드나
  D68 이 `Pickup_Swift` 를 급히 넣으면서 **`Pickup_Haste` 를 복제하고 색만 연두로** 물들였다.
  ⇒ 공속 픽업과 **모양이 같고 색만 다르다.** 난전에서 못 가른다 —
  `C33`(탄환)에서와 같은 문제고, 답도 같다: **색이 아니라 모양으로 가른다.**

🔴 실루엣을 어떻게 갈랐나
  Haste 는 **세로로 선 지그재그 번개**다. 그래서 이건 **가로로 흐르는 빨리감기 꺾쇠(»)**로 잡았다.
  둘 다 대각선 요소가 있지만 **주축이 90도 다르다** — 작게 줄여도 갈린다.

규격 — 기존 픽업 6장을 재서 맞췄다
  1024x1024 · PPU 2048(=0.5유닛) · Point · 논리 **32x32** 를 32배 확대 · 색 5개(외곽선 포함)

🔴 색을 중립으로 그린다
  프리팹이 `m_Color = (0.55, 1.00, 0.45)` 연두로 곱한다. D68 이 그걸 유지하자고 했다 —
  **줍고 나면 뜨는 오라가 연두라 짝이 맞아야** "저걸 먹으면 저 색이 된다"가 읽힌다.
  ⇒ 여기서 초록으로 칠하면 **두 번 곱해져** 형광이 된다. 명암만 주고 색은 프리팹에 맡긴다.
"""
from PIL import Image
import os

L, SCALE = 32, 32          # 논리 32x32 -> 1024

PAL = {
    'o': (26, 22, 38, 255),      # 외곽선
    'L': (244, 246, 242, 255),   # 밝은 면
    'M': (188, 196, 186, 255),   # 중간
    'D': (132, 142, 130, 255),   # 어두운 면 (밑창)
    '.': (0, 0, 0, 0),
}

# 🔴 **빨리감기 꺾쇠 3개**(»). 첫 판은 날개 부츠였는데 32x32 에서는 날개와 부츠가
#    한 덩어리로 뭉쳐 **아래 화살표**로 읽혔다 — 게다가 아래 방향은 "느려짐"으로 보인다.
#    꺾쇠는 ① 주축이 **가로**라 세로 번개(Haste)와 안 겹치고 ② 32px 에서도 안 뭉개지고
#    ③ "빨리감기"라는 뜻이 이미 통용된다.
CX = (7, 14, 21)     # 꺾쇠 3개의 x 시작점
ARM = 6              # 팔 길이(=세로 반높이)
TH  = 2.2            # 두께

def chevron_cells():
    """오른쪽을 향한 꺾쇠 3개가 차지하는 칸과 밝기 단계."""
    cells = {}
    cy = L // 2
    for k, cx in enumerate(CX):
        for y in range(L):
            dy = abs(y - cy)
            if dy > ARM:
                continue
            xline = cx + (ARM - dy)          # 꺾쇠 선의 x
            for x in range(L):
                d = abs(x - xline)
                if d <= TH:
                    # 앞쪽 꺾쇠일수록 밝게 — 진행 방향이 읽힌다
                    tone = 'L' if (d <= TH - 1.1) else 'M'
                    if k == 0: tone = 'M' if tone == 'L' else 'D'
                    cells[(x, y)] = tone
    return cells

def build():
    cells = chevron_cells()
    # 외곽선 두르기 (8방향)
    out = dict(cells)
    for (x, y) in cells:
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                q = (x + dx, y + dy)
                if 0 <= q[0] < L and 0 <= q[1] < L and q not in cells:
                    out[q] = 'o'
    img = Image.new('RGBA', (L, L), (0, 0, 0, 0))
    px = img.load()
    for (x, y), ch in out.items():
        px[x, y] = PAL[ch]
    return img.resize((L * SCALE, L * SCALE), Image.NEAREST)


    rows = [r for r in ART.split('\n') if r]
    assert len(rows) == L, f'{len(rows)} rows'
    for i, r in enumerate(rows):
        assert len(r) == L, f'row {i}: {len(r)}'
    img = Image.new('RGBA', (L, L), (0, 0, 0, 0))
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            px[x, y] = PAL[ch]
    return img.resize((L * SCALE, L * SCALE), Image.NEAREST)

if __name__ == '__main__':
    out = 'Assets/Game/Sprites/Pickups'
    os.makedirs(out, exist_ok=True)
    im = build()
    p = f'{out}/Swift.png'
    im.save(p)
    print(f'{p}  {im.size}  alpha {im.getchannel("A").getextrema()}  '
          f'colors {len(im.convert("RGB").getcolors(maxcolors=99999))}')
