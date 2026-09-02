# -*- coding: utf-8 -*-
"""
Docs/PERF.md 의 측정값에서 포트폴리오용 SVG 그림을 만든다.

🔴 숫자는 전부 이 파일 상단의 DATA 에 모아 두었다 — 그림을 손으로 그리지 않는 이유가
   이것이다. 재측정하면 DATA 만 고치고 다시 돌리면 된다.

사용법:  python Tools/Perf/make_figures.py
출력:    Docs/Figures/*.svg   (벡터. PDF 조판에 그대로 넣으면 된다)
"""
import io
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Figures")

# ── 색 ────────────────────────────────────────────────────────────
INK   = "#1a1a1a"   # 본문
MUTE  = "#8a8a8a"   # 보조선·부가 라벨
GRID  = "#e2e2e2"
HI    = "#d94f2b"   # 강조 (문제 · before)
OK    = "#2f7d5d"   # 해결 · after
BLUE  = "#3a6ea5"   # 중립 데이터
BG    = "#ffffff"

FONT = "system-ui, -apple-system, 'Segoe UI', 'Malgun Gothic', sans-serif"


def head(w, h, title):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" '
            f'viewBox="0 0 {w} {h}" font-family="{FONT}">\n'
            f'<title>{title}</title>\n'
            f'<rect width="{w}" height="{h}" fill="{BG}"/>\n')


def txt(x, y, s, size=13, fill=INK, anchor="start", weight="400"):
    return (f'<text x="{x}" y="{y}" font-size="{size}" fill="{fill}" '
            f'text-anchor="{anchor}" font-weight="{weight}">{s}</text>\n')


def save(name, body):
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, name)
    io.open(p, "w", encoding="utf-8").write(body + "</svg>\n")
    print("wrote", os.path.normpath(p))


# ══════════════════════════════════════════════════════════════════
# DATA — 전부 Docs/PERF.md 의 실측값이다
# ══════════════════════════════════════════════════════════════════

# §7-C · §8-2 — 이웃 수 n 의 히스토그램 (적 800, 시나리오 A)
HIST_BUF12 = [0, 949, 1855, 2311, 2166, 2521, 3269, 3404, 3503, 3590, 3674, 3789, 79317]
HIST_BUF64 = [0, 1080, 1872, 2216, 2612, 2957, 3651, 4056, 4331, 4668, 4956, 4911, 5125,
              5018, 4892, 4407, 4211, 4030, 3762, 3514, 3354, 3262, 3176, 3005, 2895,
              2970, 2917, 2915, 2898, 2763, 2700, 2445, 2240, 2122, 1991, 1852, 1707,
              1591, 1513, 1415, 1245, 1175, 1072, 949, 892, 782, 659, 649, 575, 484,
              446, 384, 323, 305, 252, 251, 187, 180, 156, 170, 139, 151, 140, 135, 2373]

# §8-4 — 절단 비율 (%)
TRUNC = [  # (적 수, buf12 값들, buf64 값들)
    (130, [0.92, 0.50],               [0.0, 0.0]),
    (400, [22.14, 21.16],             [0.0, 0.0]),
    (800, [71.88, 70.80, 71.07],      [1.74, 2.07, 1.80]),
]

# §7-G — 프레임 구간 분해 (적 800 · 시나리오 A · 렌더러 ON · p50 13.035 ms)
BREAKDOWN = [
    ("무리 분리 (질의+누산)", 2.451, HI),
    ("적 렌더 (대조군 분리)",  1.740, BLUE),
    ("LateUpdate (EnemyVisual)", 1.115, BLUE),
    ("Physics2D.Simulate",    0.524, BLUE),
    ("BehaviourUpdate",       0.245, BLUE),
    ("PlayerLoop 내 나머지",   2.043, MUTE),
    ("PlayerLoop 바깥 (에디터)", 4.917, MUTE),
]
FRAME_TOTAL = 13.035

# §8-8-2 — 빈 Update() 더미 수 vs BehaviourUpdate (ms)
DUMMY = [(0, 0.043), (500, 0.114), (1000, 0.187), (2000, 0.321)]
DUMMY_SLOPE_US = 0.139

# §0-1 — 죽인 가설
HYPOTHESES = [
    ("적↔적 트리거 충돌",        "매트릭스에서 이미 꺼져 있었다"),
    ("GC 정지",                  "GC.Collect = 0.000"),
    ("\"적이 많으면 느리다\"",    "적만 800마리 = 121 fps"),
    ("풀 Instantiate 폭풍",      "4회 중 1회만 기준 충족"),
    ("적 사망 처리 폭주",         "4회 중 1회만"),
    ("데미지 팝업 생성",          "4회 중 1회만"),
    ("OverlapCircleAll 할당",    "프레임의 1.3 %"),
    ("투사체 Update()",          "투사체가 1~3개뿐"),
    ("ExpDrop.Update 가 비싸다",  "개당 0.9 µs · 개선 −11 %"),
    ("DamagePopup(TMP)가 비싸다", "개당 1.6 µs · 프레임당 0.2 ms"),
    ("Update() 호출 고정 비용",   "0.139 µs — 예측과 10배 이상 차이"),
]
FOUND = [
    ("B8 — 이웃 12칸 무언 절단",
     "적 400에서 21 % · 800에서 71 % 가 이웃을 잘라 먹고 판정"),
    ("적 겹침의 원인은 밀도",
     "적 800에서 85 %가 0.5유닛 안에 이웃을 둔다 (분리 반경은 0.85)"),
]


# ══════════════════════════════════════════════════════════════════
# 그림 1 — 벽
# ══════════════════════════════════════════════════════════════════
def fig_wall():
    W, H = 900, 460
    L, R, T, B = 70, 40, 88, 70
    pw, ph = W - L - R, H - T - B
    s = head(W, H, "이웃 수 분포 — 12칸 버퍼가 만든 벽")

    s += txt(L, 34, "이웃 수 분포 — 버퍼 12칸이 만든 «벽»", 19, INK, weight="600")
    s += txt(L, 58, "적 800마리 · 시나리오 A · 분리 질의 110,348회. 12 이상이 전부 12로 접혔다.",
             13, MUTE)

    top = max(HIST_BUF12)
    n = 64
    bw = pw / (n + 1)

    def y_of(v):
        # 로그 스케일 — 79,317 과 135 를 한 그림에 담아야 한다
        import math
        if v <= 0:
            return T + ph
        return T + ph - (math.log10(v) / math.log10(top)) * ph

    # 격자
    for e in range(0, 6):
        v = 10 ** e
        if v > top:
            break
        y = y_of(v)
        s += f'<line x1="{L}" y1="{y:.1f}" x2="{L+pw}" y2="{y:.1f}" stroke="{GRID}"/>\n'
        s += txt(L - 8, y + 4, f"{v:,}", 10, MUTE, "end")

    # buf64 (배경, 연하게)
    for i, v in enumerate(HIST_BUF64):
        if i == 0 or v <= 0:
            continue
        x = L + i * bw
        y = y_of(v)
        s += (f'<rect x="{x:.1f}" y="{y:.1f}" width="{bw*0.86:.1f}" '
              f'height="{T+ph-y:.1f}" fill="{OK}" opacity="0.30"/>\n')

    # buf12 (전경)
    for i, v in enumerate(HIST_BUF12):
        if i == 0 or v <= 0:
            continue
        x = L + i * bw
        y = y_of(v)
        c = HI if i == 12 else BLUE
        s += (f'<rect x="{x:.1f}" y="{y:.1f}" width="{bw*0.86:.1f}" '
              f'height="{T+ph-y:.1f}" fill="{c}"/>\n')

    # 축
    s += f'<line x1="{L}" y1="{T+ph}" x2="{L+pw}" y2="{T+ph}" stroke="{INK}"/>\n'
    for i in [1, 12, 20, 30, 40, 50, 64]:
        x = L + i * bw + bw * 0.43
        s += txt(x, T + ph + 18, str(i), 11, MUTE, "middle")
    s += txt(L + pw / 2, H - 16, "한 번의 질의가 받은 이웃 수  n", 13, INK, "middle")
    s += txt(18, T + ph / 2, "도수 (로그)", 12, MUTE, "middle",
             ) .replace("<text", f'<text transform="rotate(-90 18 {T+ph/2})"', 1)

    # 벽 지시선
    xw = L + 12 * bw + bw * 0.43
    s += f'<line x1="{xw}" y1="{y_of(HIST_BUF12[12])-10}" x2="{xw+120}" y2="{T+22}" stroke="{HI}" stroke-width="1.2"/>\n'
    s += txt(xw + 126, T + 26, "79,317 — 11번 칸의 21배", 13, HI, weight="600")
    s += txt(xw + 126, T + 44, "자연스러운 분포라면 12는 11보다 작아야 한다", 11, MUTE)

    # 범례
    s += f'<rect x="{L}" y="{T-26}" width="11" height="11" fill="{BLUE}"/>\n'
    s += txt(L + 17, T - 16, "버퍼 12 (수정 전)", 12, INK)
    s += f'<rect x="{L+150}" y="{T-26}" width="11" height="11" fill="{OK}" opacity="0.30"/>\n'
    s += txt(L + 167, T - 16, "버퍼 64 (수정 후) — 진짜 분포", 12, INK)
    return s


# ══════════════════════════════════════════════════════════════════
# 그림 2 — 절단 비율
# ══════════════════════════════════════════════════════════════════
def fig_truncation():
    W, H = 760, 420
    L, R, T, B = 80, 220, 86, 66
    pw, ph = W - L - R, H - T - B
    s = head(W, H, "이웃 절단 비율 — 버퍼 12 vs 64")

    s += txt(L, 34, "이웃 절단 비율 — 버퍼 12 → 64", 19, INK, weight="600")
    s += txt(L, 58, "적 130은 Waves.csv 의 실제 MaxAlive 상한. 800은 게임에 존재하지 않는 조건이다.",
             13, MUTE)

    top = 80.0
    gw = pw / len(TRUNC)

    for gy in range(0, 81, 20):
        y = T + ph - gy / top * ph
        s += f'<line x1="{L}" y1="{y:.1f}" x2="{L+pw}" y2="{y:.1f}" stroke="{GRID}"/>\n'
        s += txt(L - 8, y + 4, f"{gy} %", 11, MUTE, "end")

    for gi, (cnt, before, after) in enumerate(TRUNC):
        gx = L + gi * gw
        b = sum(before) / len(before)
        a = sum(after) / len(after)
        bw = gw * 0.26

        hb = b / top * ph
        s += (f'<rect x="{gx+gw*0.16:.1f}" y="{T+ph-hb:.1f}" width="{bw:.1f}" '
              f'height="{hb:.1f}" fill="{HI}"/>\n')
        s += txt(gx + gw * 0.16 + bw / 2, T + ph - hb - 8, f"{b:.1f} %", 12, HI, "middle", "600")

        ha = max(a / top * ph, 1.5)
        s += (f'<rect x="{gx+gw*0.50:.1f}" y="{T+ph-ha:.1f}" width="{bw:.1f}" '
              f'height="{ha:.1f}" fill="{OK}"/>\n')
        s += txt(gx + gw * 0.50 + bw / 2, T + ph - ha - 8,
                 ("0 %" if a == 0 else f"{a:.1f} %"), 12, OK, "middle", "600")

        s += txt(gx + gw / 2, T + ph + 20, f"적 {cnt}", 13, INK, "middle", "600")
        if cnt == 130:
            s += txt(gx + gw / 2, T + ph + 38, "실제 게임 상한", 10, MUTE, "middle")
        if cnt == 800:
            s += txt(gx + gw / 2, T + ph + 38, "게임에 없는 조건", 10, MUTE, "middle")

    s += f'<line x1="{L}" y1="{T+ph}" x2="{L+pw}" y2="{T+ph}" stroke="{INK}"/>\n'

    # 옆 설명
    bx = L + pw + 26
    s += f'<rect x="{bx}" y="{T-24}" width="11" height="11" fill="{HI}"/>\n'
    s += txt(bx + 17, T - 14, "버퍼 12", 12, INK)
    s += f'<rect x="{bx}" y="{T-4}" width="11" height="11" fill="{OK}"/>\n'
    s += txt(bx + 17, T + 6, "버퍼 64", 12, INK)

    s += txt(bx, T + 44, "분리 비용 (프레임 대비)", 12, INK, weight="600")
    rows = [("130", "1.3 %", "1.1 %", OK),
            ("400", "7.3 %", "7.0 %", OK),
            ("800", "28 %", "41 %", HI)]
    for i, (c, a, b2, col) in enumerate(rows):
        y = T + 66 + i * 20
        s += txt(bx, y, f"적 {c}", 11, MUTE)
        s += txt(bx + 52, y, a, 11, INK)
        s += txt(bx + 92, y, "→", 11, MUTE)
        s += txt(bx + 112, y, b2, 11, col, weight="600")
    s += txt(bx, T + 148, "실제 구간에서는 비용 차이가", 11, MUTE)
    s += txt(bx, T + 164, "측정 노이즈 안이다.", 11, MUTE)
    s += txt(bx, T + 188, "🔴 800에서는 비싸진다 —", 11, HI)
    s += txt(bx, T + 204, "사전 등록 기준은 «실패»로", 11, HI)
    s += txt(bx, T + 220, "남겼다.", 11, HI)
    return s


# ══════════════════════════════════════════════════════════════════
# 그림 3 — 프레임 구간 분해
# ══════════════════════════════════════════════════════════════════
def fig_breakdown():
    W, H = 860, 330
    L, R, T = 70, 40, 110
    pw = W - L - R
    s = head(W, H, "프레임 구간 분해")

    s += txt(L, 34, "13 ms 프레임은 어디로 가는가", 19, INK, weight="600")
    s += txt(L, 58, "적 800마리 · 시나리오 A · 렌더러 ON · p50 13.035 ms · 에디터 플레이 모드",
             13, MUTE)
    s += txt(L, 80, "🔴 스크립트 중에서는 «무리 분리»가 가장 크다 — 적 렌더보다도 크다.",
             13, HI)

    x = L
    bar_y, bar_h = T, 46
    for name, ms, col in BREAKDOWN:
        w = ms / FRAME_TOTAL * pw
        s += f'<rect x="{x:.1f}" y="{bar_y}" width="{w:.1f}" height="{bar_h}" fill="{col}"/>\n'
        if w > 42:
            s += txt(x + w / 2, bar_y + 28, f"{ms/FRAME_TOTAL*100:.0f}%", 13,
                     "#ffffff", "middle", "600")
        x += w

    # 범례 (2열)
    for i, (name, ms, col) in enumerate(BREAKDOWN):
        cx = L + (i % 2) * (pw / 2)
        cy = T + bar_h + 34 + (i // 2) * 24
        s += f'<rect x="{cx}" y="{cy-10}" width="11" height="11" fill="{col}"/>\n'
        s += txt(cx + 18, cy, name, 12, INK)
        s += txt(cx + pw / 2 - 60, cy, f"{ms:.3f} ms", 12, MUTE, "end")

    y = T + bar_h + 34 + ((len(BREAKDOWN) + 1) // 2) * 24 + 16
    s += txt(L, y, "Physics2D.Simulate 은 4 % 뿐이다 — Rigidbody2D 800개가 문제가 아니라 «우리가 부르는 질의»가 문제다.",
             12, INK)
    s += txt(L, y + 20, "38 %는 PlayerLoop 바깥, 즉 에디터 비용이다. 빌드에는 거의 없으므로 실제로는 나머지 비중이 더 커진다.",
             12, MUTE)
    return s


# ══════════════════════════════════════════════════════════════════
# 그림 4 — Update 호출 단가
# ══════════════════════════════════════════════════════════════════
def fig_update_cost():
    W, H = 700, 420
    L, R, T, B = 78, 210, 92, 62
    pw, ph = W - L - R, H - T - B
    s = head(W, H, "Update() 호출 단가")

    s += txt(L, 34, "Update() 호출 자체는 얼마인가", 19, INK, weight="600")
    s += txt(L, 58, "본문이 «완전히 비어 있는» 컴포넌트를 N개 세우고 BehaviourUpdate 를 쟀다.",
             13, MUTE)
    s += txt(L, 76, "기울기가 곧 호출 단가다.", 13, MUTE)

    xmax, ymax = 2200.0, 0.36
    def X(v): return L + v / xmax * pw
    def Y(v): return T + ph - v / ymax * ph

    for gy in [0, 0.1, 0.2, 0.3]:
        s += f'<line x1="{L}" y1="{Y(gy):.1f}" x2="{L+pw}" y2="{Y(gy):.1f}" stroke="{GRID}"/>\n'
        s += txt(L - 8, Y(gy) + 4, f"{gy:.1f}", 11, MUTE, "end")
    for gx in [0, 500, 1000, 1500, 2000]:
        s += txt(X(gx), T + ph + 18, f"{gx:,}", 11, MUTE, "middle")

    # 회귀선
    b0 = DUMMY[0][1]
    s += (f'<line x1="{X(0):.1f}" y1="{Y(b0):.1f}" x2="{X(2000):.1f}" '
          f'y2="{Y(b0 + 2000*DUMMY_SLOPE_US/1000):.1f}" stroke="{MUTE}" '
          f'stroke-width="1.4" stroke-dasharray="5 4"/>\n')

    for cnt, ms in DUMMY:
        s += f'<circle cx="{X(cnt):.1f}" cy="{Y(ms):.1f}" r="5.5" fill="{BLUE}"/>\n'
        s += txt(X(cnt) + 10, Y(ms) - 8, f"{ms:.3f}", 11, INK)

    s += f'<line x1="{L}" y1="{T+ph}" x2="{L+pw}" y2="{T+ph}" stroke="{INK}"/>\n'
    s += f'<line x1="{L}" y1="{T}" x2="{L}" y2="{T+ph}" stroke="{INK}"/>\n'
    s += txt(L + pw / 2, H - 14, "빈 Update() 를 가진 객체 수", 13, INK, "middle")

    bx = L + pw + 24
    s += txt(bx, T + 10, "기울기", 12, MUTE)
    s += txt(bx, T + 36, "0.139 µs", 22, OK, weight="700")
    s += txt(bx, T + 54, "호출 하나당", 11, MUTE)

    s += txt(bx, T + 92, "내 예측", 12, MUTE)
    s += txt(bx, T + 114, "1 ~ 11 µs", 16, HI, weight="600")
    s += txt(bx, T + 132, "🔴 10배 이상 틀렸다", 11, HI)

    s += txt(bx, T + 168, "객체 2,000개에서", 11, MUTE)
    s += txt(bx, T + 184, "0.28 ms —", 11, INK)
    s += txt(bx, T + 200, "60 fps 예산의 1.7 %", 11, INK)
    s += txt(bx, T + 224, "«수천 개면 문제»라는", 11, MUTE)
    s += txt(bx, T + 240, "일반론의 기준은", 11, MUTE)
    s += txt(bx, T + 256, "이 규모보다 훨씬 크다.", 11, MUTE)
    return s


# ══════════════════════════════════════════════════════════════════
# 그림 5 — 가설 지도
# ══════════════════════════════════════════════════════════════════
def fig_hypotheses():
    rows = len(HYPOTHESES)
    W = 900
    H = 150 + rows * 27 + 30 + len(FOUND) * 46
    s = head(W, H, "측정으로 죽인 가설")

    s += txt(50, 38, "측정이 죽인 가설 11개", 20, INK, weight="600")
    s += txt(50, 62, "코드를 읽고 «비싸 보인다»로 세운 가설은 대부분 틀렸다. 남은 둘은 추측으로는 찾을 수 없었다.",
             13, MUTE)

    y = 104
    s += txt(50, y, "기각", 12, MUTE, weight="600")
    s += txt(330, y, "무엇이 죽였나", 12, MUTE, weight="600")
    y += 10
    s += f'<line x1="50" y1="{y}" x2="{W-50}" y2="{y}" stroke="{INK}" stroke-width="1"/>\n'
    y += 20

    for i, (h, why) in enumerate(HYPOTHESES):
        s += f'<line x1="50" y1="{y+7}" x2="62" y2="{y-5}" stroke="{HI}" stroke-width="2"/>\n'
        s += f'<line x1="50" y1="{y-5}" x2="62" y2="{y+7}" stroke="{HI}" stroke-width="2"/>\n'
        s += txt(74, y + 5, h, 13, INK)
        s += txt(330, y + 5, why, 12, MUTE)
        y += 27

    y += 18
    s += f'<line x1="50" y1="{y}" x2="{W-50}" y2="{y}" stroke="{GRID}"/>\n'
    y += 26
    s += txt(50, y, "찾은 것", 13, OK, weight="700")
    y += 24
    for name, desc in FOUND:
        s += f'<circle cx="56" cy="{y}" r="5" fill="{OK}"/>\n'
        s += txt(74, y + 5, name, 13, INK, weight="600")
        y += 20
        s += txt(74, y + 5, desc, 12, MUTE)
        y += 26
    return s


if __name__ == "__main__":
    save("fig1_neighbor_wall.svg", fig_wall())
    save("fig2_truncation.svg",    fig_truncation())
    save("fig3_frame_breakdown.svg", fig_breakdown())
    save("fig4_update_call_cost.svg", fig_update_cost())
    save("fig5_hypotheses.svg",    fig_hypotheses())
    print("\nDocs/Figures/ 에 SVG 5장. PDF 조판에 벡터로 그대로 넣으면 된다.")
