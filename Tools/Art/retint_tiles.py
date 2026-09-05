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

# ── D79 (2026-09-05) — 램프를 버리고 **한 점**으로 모은다 ───────────────
#
# 사용자 판정: *"스샷같이 특정색이 너무 튀어 좀더 자연스러운 배경이 되게"*
#
# 🔴 위 C37 램프의 대가가 그것이다. 층0 에서도 깊은 타일이 7 % 는 깔리는데
#    그게 가장 먼 색(차가운 청회색)이라 **올리브 바닥에 남색 구멍처럼 박힌다.**
#    C37 도 이미 알고 있었다 — GAMMA 를 1.6 으로 올려 36 % 줄였을 뿐 없애지는 못했다.
#    램프가 남아 있는 한 **한 층 안에 서로 다른 색이 섞이는 구조**는 그대로다.
#
# 🔑 해법 — <b>층 구분을 타일에서 빼내 `Tilemap.color` 로 옮긴다</b> (GroundTiler, D79).
#    타일 10종은 전부 <b>같은 한 점</b>으로 모은다 ⇒ 한 층 안의 색 편차가 <b>원리적으로 0</b>.
#    층과 층 사이는 타일맵 전체에 곱하는 색 하나로 가른다 ⇒ 모든 타일이 <b>같이</b> 움직이므로
#    아무리 세게 갈라도 퀼트가 안 생긴다. 두 목표가 더는 서로를 깎지 않는다.
#
# ⚠️ 목표는 **채널별 원본 최솟값 이하**여야 한다 (배율이 1 을 못 넘으므로).
#    실측 최솟값 (0.1560, 0.1586, 0.1130) — 아래 값은 그 안에 들어간다.
#    밝기 L 0.150 으로 C37 램프의 층0(0.1542)과 사실상 같다. **초반이 안 어두워진다.**
TARGET = (0.150, 0.158, 0.108)

def target_of(i, n):
    """모든 타일이 같은 한 점으로 간다 (D79). i·n 은 안 쓰지만 호출부는 그대로 둔다."""
    return TARGET

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
