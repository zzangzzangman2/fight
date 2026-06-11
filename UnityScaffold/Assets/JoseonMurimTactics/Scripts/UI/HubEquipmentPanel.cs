using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// 허브 "장비" 메뉴 — 캐릭터 정비창(설계 §D).
/// 좌: 캐릭터 목록 / 중: 캐릭터 카드(상징색 인장 + 유대 게이지) / 우: 장비 3칸 /
/// 하: 인벤토리 그리드(장비·선물·소모품·재료 탭) + 상세(장착/해제/강화/선물).
/// HubController가 비대해지지 않도록 별도 패널 클래스로 분리한다.
/// </summary>
public sealed class HubEquipmentPanel
{
    private static readonly Color Rose = new Color(0.94f, 0.45f, 0.62f, 1f);

    private readonly GameRoot root;
    private readonly Action<string> showToast;
    private readonly Action<string> addLog;
    private readonly HubInventoryGrid grid = new HubInventoryGrid();

    private string selectedCharId = CharacterGrowthCatalog.ProtagonistId;
    private int invTab;
    private string selectedItemId;
    private Vector2 charScroll;

    public HubEquipmentPanel(GameRoot root, Action<string> showToast, Action<string> addLog)
    {
        this.root = root;
        this.showToast = showToast;
        this.addLog = addLog;
    }

    private static readonly string[] InvTabs = { "장비", "선물", "소모품", "재료" };

    public static Color SignatureColor(string characterId)
    {
        switch (CharacterGrowthCatalog.NormalizeCharacterId(characterId))
        {
        case "baek_ryeon":
            return new Color(0.56f, 0.92f, 1f, 1f);
        case "do_arin":
            return new Color(1f, 0.42f, 0.18f, 1f);
        case "jin_seoyul":
            return new Color(0.42f, 0.74f, 1f, 1f);
        case "seo_a":
            return new Color(0.54f, 0.98f, 0.84f, 1f);
        case "han_biyeon":
            return new Color(0.66f, 0.42f, 0.96f, 1f);
        default:
            return new Color(1f, 0.82f, 0.30f, 1f); // 박성준 — 천광의 금빛
        }
    }

    public void Draw(Rect r, float s)
    {
        List<string> roster = Roster();
        if (!roster.Contains(selectedCharId))
        {
            selectedCharId = roster[0];
        }

        // ── 머리글: 제목 + 보유 은냥 ──
        GUI.Label(new Rect(r.x, r.y, r.width * 0.6f, 36f * s), "정비 — 장비와 선물", UiTheme.Heading);
        GUIStyle silverStyle = new GUIStyle(UiTheme.Heading)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = Mathf.RoundToInt(22f * s)
        };
        silverStyle.normal.textColor = UiTheme.GoldBright;
        GUI.Label(new Rect(r.x + r.width * 0.5f, r.y, r.width * 0.5f, 36f * s),
                  $"보유 은냥  {root.Flags.GetInt("silver")}", silverStyle);

        float topY = r.y + 42f * s;
        float mainH = Mathf.Max(208f * s, (r.height - 50f * s) * 0.44f);
        float gap = 12f * s;
        float listW = Mathf.Max(150f * s, r.width * 0.20f);
        float cardW = r.width * 0.33f;
        float slotsW = r.width - listW - cardW - gap * 2f;

        DrawCharacterList(new Rect(r.x, topY, listW, mainH), s, roster);
        DrawCharacterCard(new Rect(r.x + listW + gap, topY, cardW, mainH), s);
        DrawSlots(new Rect(r.x + listW + cardW + gap * 2f, topY, slotsW, mainH), s);

        float invY = topY + mainH + 10f * s;
        DrawInventory(new Rect(r.x, invY, r.width, r.yMax - invY), s);
    }

    private List<string> Roster()
    {
        List<string> roster = new List<string> { CharacterGrowthCatalog.ProtagonistId };
        foreach (string id in root.Session.recruitedCompanionIds)
        {
            string normalized = CharacterGrowthCatalog.NormalizeCharacterId(id);
            if (!roster.Contains(normalized) && CompanionCatalog.Info(normalized) != null)
            {
                roster.Add(normalized);
            }
        }

        return roster;
    }

    // ── 좌측: 캐릭터 목록 ──

    private void DrawCharacterList(Rect rect, float s, List<string> roster)
    {
        UiTheme.DrawFill(rect, new Color(0.012f, 0.018f, 0.018f, 0.55f));
        float pad = 8f * s;
        float cellH = 54f * s;
        float contentH = roster.Count * (cellH + pad) + pad;
        Rect view = new Rect(0f, 0f, rect.width - 18f * s, Mathf.Max(contentH, rect.height));
        charScroll = GUI.BeginScrollView(rect, charScroll, view);

        for (int i = 0; i < roster.Count; i++)
        {
            string id = roster[i];
            Rect cell = new Rect(pad, pad + i * (cellH + pad), view.width - pad * 2f, cellH);
            bool selected = id == selectedCharId;
            bool hover = cell.Contains(Event.current.mousePosition);
            UiTheme.DrawFill(cell, selected ? new Color(0.105f, 0.165f, 0.140f, 0.96f)
                                            : hover ? new Color(0.085f, 0.100f, 0.092f, 0.94f)
                                                    : new Color(0.045f, 0.056f, 0.054f, 0.92f));
            Color accent = SignatureColor(id);
            UiTheme.DrawFill(new Rect(cell.x, cell.y, 4f * s, cell.height), accent);
            if (selected)
            {
                DrawFrame(cell, Mathf.Max(1f, 1.3f * s), UiTheme.GoldBright);
            }

            DrawPip(new Vector2(cell.x + 18f * s, cell.y + cell.height * 0.5f), 9f * s, accent);

            GUIStyle name = new GUIStyle(UiTheme.Body)
            {
                fontSize = Mathf.RoundToInt(17f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(cell.x + 32f * s, cell.y + 4f * s, cell.width - 40f * s, 26f * s),
                      CharacterGrowthCatalog.DisplayName(id), name);

            string subText = id == CharacterGrowthCatalog.ProtagonistId
                                 ? "문주 · 18세"
                                 : $"{StageLabel(id)} {root.Approval.Get(id)}";
            GUIStyle sub = new GUIStyle(UiTheme.SmallMuted)
            {
                fontSize = Mathf.RoundToInt(12f * s),
                alignment = TextAnchor.UpperLeft
            };
            GUI.Label(new Rect(cell.x + 32f * s, cell.y + 30f * s, cell.width - 40f * s, 20f * s), subText, sub);

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                selectedCharId = id;
            }
        }

        GUI.EndScrollView();
    }

    private string StageLabel(string companionId)
    {
        return root.Approval.GetStageLabel(companionId);
    }

    // ── 중앙: 캐릭터 카드 ──

    private void DrawCharacterCard(Rect rect, float s)
    {
        UiTheme.DrawPanel(rect, true);
        Color accent = SignatureColor(selectedCharId);
        Rect inner = new Rect(rect.x + 16f * s, rect.y + 14f * s, rect.width - 32f * s, rect.height - 28f * s);

        bool isHero = selectedCharId == CharacterGrowthCatalog.ProtagonistId;
        CompanionInfo info = CompanionCatalog.Info(selectedCharId);
        string displayName = CharacterGrowthCatalog.DisplayName(selectedCharId);

        // 상징색 인장(초상화 대체 — 캐릭터 비주얼이 Resources에 없으므로 이름 카드 방식, 설계 §D)
        float sealSize = Mathf.Min(74f * s, inner.height * 0.42f);
        Rect seal = new Rect(inner.x, inner.y, sealSize, sealSize);
        UiTheme.DrawFill(new Rect(seal.x + 3f * s, seal.y + 4f * s, seal.width, seal.height),
                         new Color(0f, 0f, 0f, 0.35f));
        UiTheme.DrawFill(seal, new Color(accent.r * 0.24f, accent.g * 0.24f, accent.b * 0.24f, 0.95f));
        DrawFrame(seal, Mathf.Max(1.5f, 2f * s), accent);
        GUIStyle glyph = new GUIStyle(UiTheme.Title)
        {
            fontSize = Mathf.RoundToInt(sealSize * 0.52f),
            alignment = TextAnchor.MiddleCenter
        };
        glyph.normal.textColor = accent;
        GUI.Label(seal, displayName.Substring(0, 1), glyph);

        float textX = seal.xMax + 14f * s;
        float textW = inner.xMax - textX;
        GUIStyle nameStyle = new GUIStyle(UiTheme.Heading)
        {
            fontSize = Mathf.RoundToInt(24f * s),
            alignment = TextAnchor.UpperLeft
        };
        nameStyle.normal.textColor = UiTheme.Ink;
        GUI.Label(new Rect(textX, inner.y, textW, 30f * s), displayName, nameStyle);

        string title = isHero ? "백두천광검문 소문주" : info != null ? info.title : string.Empty;
        GUI.Label(new Rect(textX, inner.y + 30f * s, textW, 24f * s), title, UiTheme.SmallMuted);

        string traits = isHero ? "18세 · ENFJ · 빛/검"
                               : info != null ? $"{info.age}세 · {info.mbti} · {info.element}/{info.weapon}"
                                              : string.Empty;
        GUI.Label(new Rect(textX, inner.y + 52f * s, textW, 24f * s), traits, UiTheme.Small);

        float y = seal.yMax + 12f * s;

        if (!isHero)
        {
            // 유대 게이지 — 동료와의 신뢰 지표(기본 시스템 표기는 유대, 후속 지시 §2).
            int approval = root.Approval.Get(selectedCharId);
            GUIStyle gaugeTitle = new GUIStyle(UiTheme.Small) { fontStyle = FontStyle.Bold };
            gaugeTitle.normal.textColor = UiTheme.Teal;
            GUI.Label(new Rect(inner.x, y, inner.width * 0.5f, 24f * s), "유대", gaugeTitle);
            GUIStyle stageStyle = new GUIStyle(UiTheme.Small) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(inner.x + inner.width * 0.5f, y, inner.width * 0.5f, 24f * s),
                      $"{StageLabel(selectedCharId)} · {approval}/100", stageStyle);
            y += 26f * s;

            Rect barBg = new Rect(inner.x, y, inner.width, 12f * s);
            UiTheme.DrawFill(barBg, UiTheme.HanjiPanelAlt);
            UiTheme.DrawFill(new Rect(barBg.x, barBg.y, barBg.width * Mathf.Clamp01(approval / 100f), barBg.height),
                             UiTheme.Teal);
            DrawFrame(barBg, Mathf.Max(1f, 1f * s),
                      new Color(UiTheme.Teal.r, UiTheme.Teal.g, UiTheme.Teal.b, 0.45f));
            y += 20f * s;

            int pips = Mathf.Clamp(approval / 20, 0, 5);
            for (int i = 0; i < 5; i++)
            {
                Vector2 c = new Vector2(inner.x + 10f * s + i * 24f * s, y + 8f * s);
                DrawPip(c, 11f * s,
                        i < pips ? UiTheme.Teal
                                 : new Color(UiTheme.Teal.r, UiTheme.Teal.g, UiTheme.Teal.b, 0.18f));
            }

            bool giftedToday = root.Gifts != null && root.Gifts.HasGiftedToday(selectedCharId);
            GUIStyle giftState = new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(inner.x + inner.width * 0.4f, y, inner.width * 0.6f, 22f * s),
                      giftedToday ? "오늘 선물 완료" : "오늘 선물 가능", giftState);
            y += 26f * s;
        }
        else
        {
            GUI.Label(new Rect(inner.x, y, inner.width, 44f * s),
                      "꺼져 가던 천광을 다시 세우는 주인공. 동료들과의 인연은 동료 메뉴와 선물로 깊어진다.",
                      UiTheme.Small);
            y += 50f * s;
        }

        // 장비 보정 합계
        EquipmentBonus bonus = root.Equipment.BuildBonus(selectedCharId);
        UiTheme.DrawHLine(new Rect(inner.x, y, inner.width, Mathf.Max(1f, 1f * s)),
                          new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.4f));
        y += 8f * s;
        GUI.Label(new Rect(inner.x, y, inner.width * 0.42f, 24f * s), "장비 보정", UiTheme.SmallMuted);
        GUIStyle bonusStyle = new GUIStyle(UiTheme.Small) { alignment = TextAnchor.UpperRight,
                                                            fontStyle = FontStyle.Bold };
        bonusStyle.normal.textColor = UiTheme.GoldBright;
        GUI.Label(new Rect(inner.x + inner.width * 0.32f, y, inner.width * 0.68f, 24f * s),
                  bonus.IsEmpty ? "없음" : bonus.ToString(), bonusStyle);
    }

    // ── 우측: 장비 슬롯 3칸 ──

    private void DrawSlots(Rect rect, float s)
    {
        UiTheme.DrawFill(rect, new Color(0.012f, 0.018f, 0.018f, 0.55f));
        float pad = 10f * s;
        float slotH = (rect.height - pad * 4f) / 3f;

        for (int i = 0; i < EquipmentService.SlotOrder.Length; i++)
        {
            EquipmentSlot slot = EquipmentService.SlotOrder[i];
            Rect row = new Rect(rect.x + pad, rect.y + pad + i * (slotH + pad), rect.width - pad * 2f, slotH);
            string equippedId = root.Equipment.GetEquipped(selectedCharId, slot);
            EquipmentInfo info = EquipmentCatalog.Get(equippedId);
            bool hover = row.Contains(Event.current.mousePosition);

            UiTheme.DrawFill(new Rect(row.x + 2f * s, row.y + 3f * s, row.width, row.height),
                             new Color(0f, 0f, 0f, 0.30f));
            UiTheme.DrawFill(row, info != null ? new Color(0.075f, 0.095f, 0.080f, 0.95f)
                                               : hover ? new Color(0.055f, 0.065f, 0.062f, 0.92f)
                                                       : new Color(0.038f, 0.046f, 0.044f, 0.90f));
            DrawFrame(row, Mathf.Max(1f, 1.2f * s),
                      info != null ? new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.85f)
                                   : new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.30f));

            GUIStyle slotTag = new GUIStyle(UiTheme.SmallMuted)
            {
                fontSize = Mathf.RoundToInt(13f * s),
                fontStyle = FontStyle.Bold
            };
            slotTag.normal.textColor = UiTheme.Teal;
            GUI.Label(new Rect(row.x + 12f * s, row.y + 6f * s, 120f * s, 20f * s),
                      EquipmentCatalog.SlotLabel(slot), slotTag);

            if (info != null)
            {
                int level = root.Equipment.GetUpgradeLevel(info.id);
                GUIStyle nameStyle = new GUIStyle(UiTheme.Body)
                {
                    fontSize = Mathf.RoundToInt(18f * s),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
                string label = level > 0 ? $"{info.displayName} <color=#F5C75C>+{level}</color>" : info.displayName;
                GUI.Label(new Rect(row.x + 12f * s, row.y + 26f * s, row.width - 110f * s, 26f * s), label,
                          nameStyle);
                GUIStyle fx = new GUIStyle(UiTheme.SmallMuted)
                {
                    fontSize = Mathf.RoundToInt(12f * s),
                    clipping = TextClipping.Clip
                };
                GUI.Label(new Rect(row.x + 12f * s, row.y + row.height - 24f * s, row.width - 110f * s, 20f * s),
                          EquipmentCatalog.DescribeBonus(info, level), fx);

                if (GUI.Button(new Rect(row.xMax - 86f * s, row.y + row.height * 0.5f - 17f * s, 74f * s, 34f * s),
                               "해제", UiTheme.Button))
                {
                    root.Equipment.Unequip(selectedCharId, slot);
                    showToast($"{info.displayName} 해제");
                    addLog($"{CharacterGrowthCatalog.DisplayName(selectedCharId)}의 {info.displayName}을(를) 거두었다.");
                }
                else if (GUI.Button(new Rect(row.x, row.y, row.width - 96f * s, row.height), GUIContent.none,
                                    GUIStyle.none))
                {
                    selectedItemId = info.id;
                    invTab = 0;
                }
            }
            else
            {
                GUIStyle empty = new GUIStyle(UiTheme.SmallMuted)
                {
                    fontSize = Mathf.RoundToInt(15f * s),
                    alignment = TextAnchor.MiddleLeft
                };
                GUI.Label(new Rect(row.x + 12f * s, row.y + 24f * s, row.width - 20f * s, row.height - 30f * s),
                          $"{EquipmentCatalog.SlotLabel(slot)} 없음 — 장터에서 구입 후 장착", empty);
            }
        }
    }

    // ── 하단: 인벤토리 + 상세 ──

    private void DrawInventory(Rect rect, float s)
    {
        float tabH = 34f * s;
        float gap = 8f * s;
        float bw = 96f * s;
        for (int i = 0; i < InvTabs.Length; i++)
        {
            if (GUI.Button(new Rect(rect.x + i * (bw + gap), rect.y, bw, tabH), InvTabs[i],
                           invTab == i ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                invTab = i;
            }
        }

        Rect body = new Rect(rect.x, rect.y + tabH + 6f * s, rect.width, rect.height - tabH - 6f * s);
        float detailW = Mathf.Max(240f * s, body.width * 0.36f);
        Rect gridRect = new Rect(body.x, body.y, body.width - detailW - 10f * s, body.height);
        Rect detailRect = new Rect(body.xMax - detailW, body.y, detailW, body.height);

        List<InventoryStack> filtered = FilteredStacks();
        string clicked = grid.Draw(gridRect, s, filtered, selectedItemId, root.Equipment);
        if (!string.IsNullOrEmpty(clicked))
        {
            selectedItemId = clicked;
        }

        DrawDetail(detailRect, s);
    }

    private List<InventoryStack> FilteredStacks()
    {
        List<InventoryStack> filtered = new List<InventoryStack>();
        foreach (InventoryStack stack in root.Inventory.AllStacks())
        {
            bool match;
            switch (invTab)
            {
            case 0:
                match = stack.type == InventoryItemType.Equipment;
                break;
            case 1:
                match = stack.type == InventoryItemType.Gift;
                break;
            case 3:
                match = stack.type == InventoryItemType.Material;
                break;
            default:
                match = stack.type == InventoryItemType.Consumable || stack.type == InventoryItemType.KeyItem;
                break;
            }

            if (match)
            {
                filtered.Add(stack);
            }
        }

        return filtered;
    }

    private void DrawDetail(Rect rect, float s)
    {
        UiTheme.DrawFill(rect, new Color(0.030f, 0.040f, 0.038f, 0.92f));
        DrawFrame(rect, Mathf.Max(1f, 1.2f * s), new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.5f));
        Rect inner = new Rect(rect.x + 14f * s, rect.y + 10f * s, rect.width - 28f * s, rect.height - 20f * s);

        string itemId = InventoryService.NormalizeItemId(selectedItemId);
        if (string.IsNullOrEmpty(itemId) || root.Inventory.GetCount(itemId) <= 0)
        {
            GUI.Label(inner, "아이템을 선택하면 효과와 장착·강화·선물 메뉴가 열린다.", UiTheme.SmallMuted);
            return;
        }

        GUIStyle nameStyle = new GUIStyle(UiTheme.Body)
        {
            fontSize = Mathf.RoundToInt(19f * s),
            fontStyle = FontStyle.Bold
        };
        nameStyle.normal.textColor = UiTheme.GoldBright;
        GUI.Label(new Rect(inner.x, inner.y, inner.width - 60f * s, 26f * s), InventoryService.Label(itemId),
                  nameStyle);
        GUIStyle countStyle = new GUIStyle(UiTheme.Small) { alignment = TextAnchor.UpperRight };
        GUI.Label(new Rect(inner.xMax - 70f * s, inner.y + 2f * s, 70f * s, 24f * s),
                  "x" + root.Inventory.GetCount(itemId), countStyle);

        float y = inner.y + 30f * s;
        EquipmentInfo equip = EquipmentCatalog.Get(itemId);
        GiftInfo gift = GiftCatalog.Get(itemId);

        if (equip != null)
        {
            DrawEquipmentDetail(inner, ref y, s, equip);
        }
        else if (gift != null)
        {
            DrawGiftDetail(inner, ref y, s, gift);
        }
        else
        {
            MaterialCatalog.MaterialInfo material = MaterialCatalog.Get(itemId);
            string desc = material != null ? material.description : ShopDescription(itemId);
            GUI.Label(new Rect(inner.x, y, inner.width, 60f * s), desc, UiTheme.Small);
        }
    }

    private void DrawEquipmentDetail(Rect inner, ref float y, float s, EquipmentInfo equip)
    {
        int level = root.Equipment.GetUpgradeLevel(equip.id);
        string owner = equip.IsExclusive
                           ? $"{CharacterGrowthCatalog.DisplayName(equip.requiredCharacterId)} 전용"
                           : "공용";
        GUI.Label(new Rect(inner.x, y, inner.width, 22f * s),
                  $"{EquipmentCatalog.SlotLabel(equip.slot)} · {owner} · 강화 +{level}/{equip.maxUpgradeLevel}",
                  UiTheme.SmallMuted);
        y += 24f * s;
        GUI.Label(new Rect(inner.x, y, inner.width, 40f * s), equip.description, UiTheme.Small);
        y += 42f * s;

        GUIStyle fxStyle = new GUIStyle(UiTheme.Small) { fontStyle = FontStyle.Bold };
        fxStyle.normal.textColor = UiTheme.Ink;
        GUI.Label(new Rect(inner.x, y, inner.width, 22f * s), "효과  " + EquipmentCatalog.DescribeBonus(equip, level),
                  fxStyle);
        y += 24f * s;

        if (level < equip.maxUpgradeLevel)
        {
            UpgradeCost cost = EquipmentCatalog.CostFor(level + 1);
            string materialLabel = cost.materialCount > 0
                                       ? $" + {InventoryService.Label(EquipmentCatalog.MaterialFor(equip.slot))} {cost.materialCount}"
                                       : string.Empty;
            GUI.Label(new Rect(inner.x, y, inner.width, 22f * s),
                      $"+{level + 1} 강화 시  {EquipmentCatalog.DescribeBonus(equip, level + 1)}", UiTheme.SmallMuted);
            y += 24f * s;
            GUI.Label(new Rect(inner.x, y, inner.width, 22f * s), $"강화 비용  은냥 {cost.silver}{materialLabel}",
                      UiTheme.SmallMuted);
            y += 26f * s;
        }

        float bw = (inner.width - 8f * s) * 0.5f;
        float bh = 40f * s;
        float by = inner.yMax - bh;

        string equipReason;
        bool canEquip = root.Equipment.CanEquip(selectedCharId, equip.id, out equipReason);
        bool alreadyOn = root.Equipment.GetEquipped(selectedCharId, equip.slot) == equip.id;
        GUI.enabled = canEquip && !alreadyOn;
        string equipLabel = alreadyOn ? "장착 중" : $"{CharacterGrowthCatalog.DisplayName(selectedCharId)} 장착";
        if (GUI.Button(new Rect(inner.x, by, bw, bh), equipLabel, UiTheme.ButtonPrimary))
        {
            if (root.Equipment.Equip(selectedCharId, equip.id, out equipReason))
            {
                showToast($"{equip.displayName} 장착");
                addLog($"{CharacterGrowthCatalog.DisplayName(selectedCharId)}이(가) {equip.displayName}을(를) 갖췄다.");
            }
        }

        GUI.enabled = true;

        string upgradeReason;
        bool canUpgrade = root.Equipment.CanUpgrade(equip.id, out upgradeReason);
        GUI.enabled = canUpgrade;
        if (GUI.Button(new Rect(inner.x + bw + 8f * s, by, bw, bh), "강화", UiTheme.Button))
        {
            UpgradeCost cost = EquipmentCatalog.CostFor(root.Equipment.GetUpgradeLevel(equip.id) + 1);
            int reached = root.Equipment.Upgrade(equip.id, out upgradeReason);
            if (reached > 0)
            {
                string materialLog = cost.materialCount > 0
                                         ? $", {InventoryService.Label(EquipmentCatalog.MaterialFor(equip.slot))} -{cost.materialCount}"
                                         : string.Empty;
                showToast($"{equip.displayName} +{reached} 강화!");
                addLog($"{equip.displayName}을(를) +{reached}(으)로 강화했다. 은냥 -{cost.silver}{materialLog}.");
            }
        }

        GUI.enabled = true;

        string hint = !canEquip && !alreadyOn ? equipReason : !canUpgrade ? upgradeReason : string.Empty;
        if (!string.IsNullOrEmpty(hint))
        {
            GUIStyle warn = new GUIStyle(UiTheme.SmallMuted) { fontSize = Mathf.RoundToInt(12f * s) };
            warn.normal.textColor = new Color(0.92f, 0.62f, 0.45f, 1f);
            GUI.Label(new Rect(inner.x, by - 24f * s, inner.width, 22f * s), hint, warn);
        }
    }

    private void DrawGiftDetail(Rect inner, ref float y, float s, GiftInfo gift)
    {
        GUI.Label(new Rect(inner.x, y, inner.width, 40f * s), gift.description, UiTheme.Small);
        y += 44f * s;

        string favorite = string.IsNullOrEmpty(gift.favoriteCompanionId)
                              ? $"모든 동료 유대 +{gift.baseDelta}"
                              : $"{CompanionCatalog.Name(gift.favoriteCompanionId)} 최애 +{gift.favoriteDelta} · 그 외 +{gift.baseDelta}";
        GUIStyle fav = new GUIStyle(UiTheme.Small) { fontStyle = FontStyle.Bold };
        fav.normal.textColor = Rose;
        GUI.Label(new Rect(inner.x, y, inner.width, 24f * s), favorite, fav);
        y += 28f * s;
        GUI.Label(new Rect(inner.x, y, inner.width, 22f * s), "하루에 동료 1명당 선물 1회.", UiTheme.SmallMuted);

        float bh = 40f * s;
        float by = inner.yMax - bh;
        bool isHero = selectedCharId == CharacterGrowthCatalog.ProtagonistId;
        string reason = string.Empty;
        bool canGive = !isHero && root.Gifts != null && root.Gifts.CanGift(selectedCharId, gift.id, out reason);
        GUI.enabled = canGive;
        string label = isHero ? "동료를 선택하세요" : $"{CharacterGrowthCatalog.DisplayName(selectedCharId)}에게 선물";
        if (GUI.Button(new Rect(inner.x, by, inner.width, bh), label, UiTheme.ButtonPrimary))
        {
            GiftResult result = root.Gifts.Give(selectedCharId, gift.id);
            if (result.success)
            {
                showToast(result.wasFavorite ? $"최애 선물! 유대 +{result.delta}" : $"유대 +{result.delta}");
                addLog(result.message.Replace("\n", " "));
            }
        }

        GUI.enabled = true;

        if (!canGive)
        {
            string hint = isHero ? "왼쪽 목록에서 선물할 동료를 고른다." : reason;
            GUIStyle warn = new GUIStyle(UiTheme.SmallMuted) { fontSize = Mathf.RoundToInt(12f * s) };
            warn.normal.textColor = new Color(0.92f, 0.62f, 0.45f, 1f);
            GUI.Label(new Rect(inner.x, by - 24f * s, inner.width, 22f * s), hint, warn);
        }
    }

    private static string ShopDescription(string itemId)
    {
        switch (InventoryService.NormalizeItemId(itemId))
        {
        case "medicine_bundle":
            return "전투 후 회복에 쓰인다.";
        case "inner_power_pill":
            return "내공 회복 소모품.";
        case "throwing_dagger_bundle":
            return "암기 보급.";
        default:
            return "보급품.";
        }
    }

    // ── 공용 그리기 ──

    private static void DrawFrame(Rect rect, float thick, Color color)
    {
        UiTheme.DrawFill(new Rect(rect.x, rect.y, rect.width, thick), color);
        UiTheme.DrawFill(new Rect(rect.x, rect.yMax - thick, rect.width, thick), color);
        UiTheme.DrawFill(new Rect(rect.x, rect.y, thick, rect.height), color);
        UiTheme.DrawFill(new Rect(rect.xMax - thick, rect.y, thick, rect.height), color);
    }

    /// <summary>45도 회전한 마름모 핍(유대/상징색 표시).</summary>
    private static void DrawPip(Vector2 center, float size, Color color)
    {
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(45f, center);
        UiTheme.DrawFill(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), color);
        GUI.matrix = saved;
    }
}
}
