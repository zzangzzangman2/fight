using UnityEngine;

namespace JoseonMurimTactics
{
    public enum WorldMapFactionGroup
    {
        Joseon,
        NineSects,
        FiveHouses,
        MartialAlliance,
        DemonCult,
        Other
    }

    public struct WorldMapFactionPin
    {
        public readonly string factionId;
        public readonly string displayName;
        public readonly string subtitle;
        public readonly string regionName;
        public readonly WorldMapFactionGroup group;
        public readonly Vector2 uv;
        public readonly string glyph;
        public readonly Color color;
        public readonly string description;
        public readonly Vector2 labelOffset;

        public WorldMapFactionPin(string factionId, string displayName, string subtitle, string regionName,
                                  WorldMapFactionGroup group, Vector2 uv, string glyph, Color color,
                                  string description, Vector2 labelOffset)
        {
            this.factionId = factionId;
            this.displayName = displayName;
            this.subtitle = subtitle;
            this.regionName = regionName;
            this.group = group;
            this.uv = uv;
            this.glyph = glyph;
            this.color = color;
            this.description = description;
            this.labelOffset = labelOffset;
        }
    }

    public struct WorldMapFactionGroupInfo
    {
        public readonly WorldMapFactionGroup group;
        public readonly string groupId;
        public readonly string displayName;
        public readonly Vector2 centerUv;
        public readonly string glyph;
        public readonly Color color;
        public readonly string description;
        public readonly float revealZoom;

        public WorldMapFactionGroupInfo(WorldMapFactionGroup group, string groupId, string displayName, Vector2 centerUv,
                                        string glyph, Color color, string description, float revealZoom)
        {
            this.group = group;
            this.groupId = groupId;
            this.displayName = displayName;
            this.centerUv = centerUv;
            this.glyph = glyph;
            this.color = color;
            this.description = description;
            this.revealZoom = revealZoom;
        }
    }

    public static class WorldMapFactionPinCatalog
    {
        public static readonly WorldMapFactionGroupInfo[] Groups =
        {
            new WorldMapFactionGroupInfo(WorldMapFactionGroup.NineSects, "group:nine_sects", "구파일방",
                                         new Vector2(0.355f, 0.505f), "派", UiTheme.Gold,
                                         "중원 명문 정파 세력권. 낮은 줌에서는 권역으로 보이고, 확대하면 열 문파가 흩어져 드러난다.",
                                         1.25f),
            new WorldMapFactionGroupInfo(WorldMapFactionGroup.FiveHouses, "group:five_houses", "오대세가",
                                         new Vector2(0.485f, 0.505f), "家", UiTheme.Teal,
                                         "혈연과 가문 무공으로 중원 정치와 전선을 흔드는 다섯 본산.",
                                         1.25f),
            new WorldMapFactionGroupInfo(WorldMapFactionGroup.DemonCult, "group:demon_cult", "마교",
                                         new Vector2(0.145f, 0.695f), "魔", UiTheme.SealRed,
                                         "천산과 혈월고원 너머의 후반 대전 권역. 마교 대전 이후 본격적으로 열린다.",
                                         1.35f)
        };

        public static readonly WorldMapFactionPin[] Pins =
        {
            new WorldMapFactionPin("nine:shaolin", "소림사", "하남 숭산", "하남 숭산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.472f, 0.430f), "佛", UiTheme.Gold,
                                   "중원 무림의 상징. 계율과 명분, 무승의 벽을 넘어야 하는 고난도 전투.",
                                   new Vector2(20f, -42f)),
            new WorldMapFactionPin("nine:wudang", "무당파", "호북 무당산", "호북 무당산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.420f, 0.500f), "太", UiTheme.NavyLight,
                                   "태극검과 진법, 반격형 보스전으로 장기전을 요구하는 문파.",
                                   new Vector2(22f, 18f)),
            new WorldMapFactionPin("nine:emei", "아미파", "사천 아미산", "사천 아미산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.315f, 0.575f), "峨", UiTheme.NavyLight,
                                   "산악, 치유, 봉쇄 패턴을 중심으로 독과 화상 대비가 중요한 문파.",
                                   new Vector2(-126f, 10f)),
            new WorldMapFactionPin("nine:huashan", "화산파", "섬서 화산", "섬서 화산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.390f, 0.405f), "華", UiTheme.SealRed,
                                   "절벽과 검진, 낙하 위험이 어울리는 섬서권 검파.",
                                   new Vector2(-118f, -44f)),
            new WorldMapFactionPin("nine:kunlun", "곤륜파", "서역·청해 곤륜산", "서역·청해 곤륜산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.190f, 0.445f), "崑", UiTheme.Teal,
                                   "고산과 빙설 내성을 요구하며 마교령으로 향하는 서부 관문.",
                                   new Vector2(-128f, -20f)),
            new WorldMapFactionPin("nine:gaibang", "개방", "낙양·개봉 총타", "낙양·개봉 총타",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.485f, 0.470f), "丐", UiTheme.Teal,
                                   "정보망과 군중전, 엄폐와 다수전을 특징으로 하는 일방 세력.",
                                   new Vector2(24f, 10f)),
            new WorldMapFactionPin("nine:qingcheng", "청성파", "사천 청성산", "사천 청성산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.330f, 0.535f), "靑", UiTheme.Navy,
                                   "사천권 서브보스 축. 아미와 당문 사이에서 지형전 압박을 만든다.",
                                   new Vector2(-120f, -4f)),
            new WorldMapFactionPin("nine:kongtong", "공동파", "감숙 공동산", "감숙 공동산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.285f, 0.385f), "崆", UiTheme.Gold,
                                   "감숙과 서북 지역을 채우는 구파일방의 누락 문파. 곤륜과 화산 사이를 잇는다.",
                                   new Vector2(-124f, -38f)),
            new WorldMapFactionPin("nine:diancang", "점창파", "운남 점창산", "운남 점창산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.300f, 0.720f), "點", UiTheme.Teal,
                                   "남서부 독지형, 습지, 비탈을 활용하는 후반 원정 문파.",
                                   new Vector2(-110f, 18f)),
            new WorldMapFactionPin("nine:zhongnan", "종남파", "섬서 종남산", "섬서 종남산",
                                   WorldMapFactionGroup.NineSects, new Vector2(0.365f, 0.425f), "終", UiTheme.NavyLight,
                                   "화산과 가까운 섬서권 문파. 화산과 라벨 방향을 반대로 둔다.",
                                   new Vector2(20f, 24f)),

            new WorldMapFactionPin("house:namgung", "남궁세가", "안휘·강남 검각", "안휘·강남 검각",
                                   WorldMapFactionGroup.FiveHouses, new Vector2(0.520f, 0.575f), "南", UiTheme.Gold,
                                   "검왕과 검진을 앞세운 오대세가의 정통 무력형 본산.",
                                   new Vector2(22f, 18f)),
            new WorldMapFactionPin("house:tang", "사천당문", "파촉·성도", "파촉·성도",
                                   WorldMapFactionGroup.FiveHouses, new Vector2(0.285f, 0.555f), "唐", UiTheme.Navy,
                                   "독, 암기, 함정이 핵심인 파촉권 가문. 아미와 청성 사이에서 살짝 서쪽으로 뺀다.",
                                   new Vector2(-130f, 22f)),
            new WorldMapFactionPin("house:peng", "하북팽가", "하북·연조", "하북·연조",
                                   WorldMapFactionGroup.FiveHouses, new Vector2(0.535f, 0.315f), "彭", UiTheme.SealRed,
                                   "대도와 중갑, 돌파형 전투로 북중원에서 조선 진입로를 압박한다.",
                                   new Vector2(24f, -46f)),
            new WorldMapFactionPin("house:zhuge", "제갈세가", "호북·양양", "호북·양양",
                                   WorldMapFactionGroup.FiveHouses, new Vector2(0.430f, 0.535f), "諸", UiTheme.Teal,
                                   "진법과 기관, 책략이 핵심인 지휘형 가문.",
                                   new Vector2(24f, 16f)),
            new WorldMapFactionPin("house:moyong", "모용세가", "요동·연경 사절로", "요동·연경 사절로",
                                   WorldMapFactionGroup.FiveHouses, new Vector2(0.625f, 0.335f), "慕", UiTheme.SealRed,
                                   "조선 압박선과 가장 가까운 세가 본산. 기존 사절로 노드와 분리된 후반 공략 지점.",
                                   new Vector2(24f, -42f)),

            new WorldMapFactionPin("demon:cult", "마교 총단", "천산·혈월고원", "천산·혈월고원",
                                   WorldMapFactionGroup.DemonCult, new Vector2(0.145f, 0.695f), "魔", UiTheme.SealRed,
                                   "구파일방과 오대세가 공략 뒤 열리는 후반 대전 지역.",
                                   new Vector2(28f, -58f))
        };

        public static int Count(WorldMapFactionGroup group)
        {
            int count = 0;
            for (int i = 0; i < Pins.Length; i++)
            {
                if (Pins[i].group == group)
                {
                    count++;
                }
            }

            return count;
        }

        public static int DefeatedCount(GameSession session, WorldMapFactionGroup group)
        {
            FactionConquestService service = new FactionConquestService(session);
            int count = 0;
            for (int i = 0; i < Pins.Length; i++)
            {
                if (Pins[i].group == group && service.GetState(Pins[i].factionId).defeated)
                {
                    count++;
                }
            }

            return count;
        }

        public static WorldMapFactionPin GetByFactionId(string factionId)
        {
            for (int i = 0; i < Pins.Length; i++)
            {
                if (Pins[i].factionId == factionId)
                {
                    return Pins[i];
                }
            }

            return new WorldMapFactionPin(factionId, factionId, "미확인", "미확인", WorldMapFactionGroup.Other,
                                          new Vector2(0.5f, 0.5f), "?", UiTheme.InkSoft, "등록되지 않은 세력 핀.",
                                          new Vector2(20f, -42f));
        }

        public static WorldMapFactionGroupInfo GetGroupInfo(WorldMapFactionGroup group)
        {
            for (int i = 0; i < Groups.Length; i++)
            {
                if (Groups[i].group == group)
                {
                    return Groups[i];
                }
            }

            return new WorldMapFactionGroupInfo(WorldMapFactionGroup.Other, "group:other", "미확인",
                                                new Vector2(0.5f, 0.5f), "?", UiTheme.InkSoft,
                                                "등록되지 않은 권역.", 1.25f);
        }

        public static string GroupName(WorldMapFactionGroup group)
        {
            return GetGroupInfo(group).displayName;
        }

        public static void LevelRange(WorldMapFactionGroup group, out int minLevel, out int maxLevel)
        {
            minLevel = int.MaxValue;
            maxLevel = int.MinValue;
            for (int i = 0; i < Pins.Length; i++)
            {
                if (Pins[i].group != group)
                {
                    continue;
                }

                FactionConquestInfo info = FactionConquestCatalog.Get(Pins[i].factionId);
                minLevel = Mathf.Min(minLevel, info.recommendedLevel);
                maxLevel = Mathf.Max(maxLevel, info.recommendedLevel);
            }

            if (minLevel == int.MaxValue)
            {
                minLevel = 1;
                maxLevel = 1;
            }
        }
    }
}
