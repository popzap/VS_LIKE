"""바닥 타일 층별 색 분리 (C37).

문제 — 층 테마가 켜졌는데 화면이 1.4 % 밖에 안 달라진다 (D49 실측)
  타일 10종의 **원본** 밝기는 0.142~0.318 로 폭이 0.175 나 된다.
  그런데 `I-28`(퀼트 제거)이 넣은 m_Color 평준화가 전부 한 점으로 끌어내렸다:

      10종 전부 화면 색 (0.154, 0.166, 0.115) · 밝기 0.156 ± 0.002

  ⇒ Grass 든 Flagstone 이든 화면에선 **같은 올리브**다. 층 가중치를 아무리 뒤집어도
    같은 색이 다른 무늬로 깔릴 뿐이다.

🔴 그래서 DEV 가 준 A(PNG 다시 칠하기)·B(새 타일)는 둘 다 헛수고다
  무엇을 그리든 이 평준화가 또 한 점으로 끌어내린다. **손잡이는 PNG 가 아니라 m_Color 다.**

해법 — 한 점이 아니라 **두 점**으로 평준화한다
  `I-28` 의 목적은 *한 층 안에서* 이웃 타일이 달라 보이는 퀼트를 없애는 것이었다.
  `D49` 가 원하는 것은 *층과 층 사이가* 달라 보이는 것이다. **둘은 안 부딪힌다** —
  같이 깔리는 타일끼리만 같으면 되기 때문이다.

  GroundTiler 는 얕은 층에서 앞 4종을, 깊은 층에서 뒤 4종을 각각 81 % 로 깐다.
  ⇒ 그 두 무리를 각각 다른 한 점으로 모은다. 무리 안에서는 여전히 동일하므로 **퀼트가 안 돌아온다.**

⚠️ 배율은 1을 못 넘는다 (`Color32`). 밝히는 방향으로는 조정이 안 되므로
   **얕은 무리는 지금 밝기를 지키고 깊은 무리만 내린다.** 초반이 어두워지면 안 된다.
"""
from PIL import Image
import io, re, os

ORDER  = ['Grass','GrassPebble','Weeds','Dirt','Gravel','Roots',
          'CrackedEarth','MossyCobble','StoneSlab','Flagstone']
WEIGHT = [40, 18, 13, 10, 7, 5, 3, 2, 1, 1]      # GroundTiler.tiles 순서·가중치

# 목표 화면 색 — 얕은 끝과 깊은 끝을 정하고 **연속 램프**로 잇는다.
# 밝기(0.299/0.587/0.114)와 색조를 같이 옮긴다 — 어둡게만 가면 적·투사체가 안 읽힌다
# (Bonecaller 0.371). 색조로도 갈라야 안전하다.
SHALLOW = (0.150, 0.180, 0.108)   # 따뜻한 풀색   L 0.163
DEEP    = (0.080, 0.090, 0.125)   # 차가운 청회색 L 0.091

# 🔴 두 무리로 뚝 끊지 않고 램프로 잇는 이유
#   처음엔 얕은 4종 / 깊은 4종을 각각 한 점에 모았다. 층 구분(dL 0.0533)은 더 컸는데
#   **얕은 층에 짙은 파랑 조각이 튀었다** — 층0 에서도 깊은 타일이 7 % 는 깔리는데
#   그게 가장 먼 색이라 구멍처럼 보인다. `I-28` 이 없애려던 퀼트가 일부 돌아온 것이다.
#   ⇒ 인덱스를 따라 이으면 이웃한 가중치의 타일이 이웃한 색을 갖는다.
#
#   실측 비교 (편차 = 그 층 가중치로 본 밝기 표준편차 = 퀼트의 세기)
#       두 무리     dL 0.0533 | 층0 편차 0.0215
#       램프 g1.0   dL 0.0438 | 층0 편차 0.0168
#       램프 g1.6   dL 0.0443 | 층0 편차 0.0138   <- 채택
#       램프 g2.2   dL 0.0434 | 층0 편차 0.0120  (대신 층9 편차가 0.0231 로 오른다)
#   g1.6 은 통과선(0.04)을 넘기면서 층0 퀼트를 36 % 줄이고,
#   층0 밝기도 0.1542 로 지금(0.1563)과 거의 같다. **초반이 어두워지지 않는다.**
GAMMA = 1.6

def target_of(i, n):
    """인덱스 i(0=가장 얕음)의 목표 색. 얕은 쪽에 촘촘하게 배분한다."""
    k = (i / (n - 1)) ** GAMMA
    return tuple(SHALLOW[c] + (DEEP[c] - SHALLOW[c]) * k for c in range(3))

def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]

def raw_avg(name):
    """불투명 픽셀의 평균 RGB (0~1)."""
    im = Image.open(f'Assets/Game/Sprites/Tiles/Tile_{name}.png').convert('RGBA')
    px, (W, H) = im.load(), im.size
    s, c = [0, 0, 0], 0
    for y in range(0, H, 2):
        for x in range(0, W, 2):
            r, g, b, a = px[x, y]
            if a > 128:
                s[0] += r; s[1] += g; s[2] += b; c += 1
    return [v / c / 255 for v in s]

def layer_lum(eff, t):
    """층 보간 t 에서의 가중 평균 화면 색. GroundTiler.BuildWeightTable 과 같은 식이다."""
    n = len(ORDER)
    ws = [WEIGHT[i] * (1 - t) + WEIGHT[n - 1 - i] * t for i in range(n)]
    tot = sum(ws)
    ch = [sum(ws[i] * eff[ORDER[i]][k] for i in range(n)) / tot for k in range(3)]
    return lum(ch), ch

def main(write=True):
    raw = {n: raw_avg(n) for n in ORDER}
    before, after, mults = {}, {}, {}

    for n in ORDER:
        t = target_of(ORDER.index(n), len(ORDER))
        path = f'Assets/Game/Tiles/Tile_{n}.asset'
        src = io.open(path, encoding='utf-8').read()
        m = re.search(r'm_Color: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}', src)
        assert m, path
        cur = [float(x) for x in m.groups()[:3]]
        before[n] = [raw[n][i] * cur[i] for i in range(3)]

        # 목표에 닿는 배율. 🔴 1 을 넘으면 못 간다 — 그 채널은 원본이 이미 더 어둡다는 뜻이다.
        mul = [min(1.0, t[i] / raw[n][i]) if raw[n][i] > 1e-6 else 0.0 for i in range(3)]
        mults[n] = mul
        after[n] = [raw[n][i] * mul[i] for i in range(3)]

        if write:
            new = ('m_Color: {r: %.4f, g: %.4f, b: %.4f, a: 1}' % tuple(mul))
            out = src.replace(m.group(0), new)
            assert out != src
            io.open(path, 'w', encoding='utf-8', newline='').write(out)

    print('%-14s %-22s %-22s' % ('tile', 'before (screen rgb)', 'after (screen rgb)'))
    for n in ORDER:
        print('%-14s %.3f %.3f %.3f  L%.3f   %.3f %.3f %.3f  L%.3f'
              % (n, *before[n], lum(before[n]), *after[n], lum(after[n])))

    print()
    for label, eff in (('BEFORE', before), ('AFTER', after)):
        l0, c0 = layer_lum(eff, 0.0)
        l9, c9 = layer_lum(eff, 1.0)
        dch = sum(abs(c0[k] - c9[k]) for k in range(3)) / 3 * 255
        print('%-6s  layer0 L %.4f | layer9 L %.4f | dL %.4f | dChannel %.1f/255'
              % (label, l0, l9, abs(l0 - l9), dch))
    print('\npass line (D49): dL >= 0.04  or  dChannel >= 15')

if __name__ == '__main__':
    main(write=True)
