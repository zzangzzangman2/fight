using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>선물 지급 결과.</summary>
public struct GiftResult
{
    public bool success;
    public int delta;
    public int approvalAfter;
    public bool wasFavorite;
    public string message;
}

/// <summary>
/// 동료 선물 — 동료와의 호감/연애도를 올리는 공략 수단.
/// 하루 제한: 동료 1명당 1회. intVars["gift:last_day:<companionId>"] = 지급한 날(DayIndex).
/// </summary>
public sealed class GiftService
{
    public const string LastGiftDayPrefix = "gift:last_day:";

    private readonly GameSession session;
    private readonly InventoryService inventory;
    private readonly CompanionApprovalService approval;

    public GiftService(GameSession session, CompanionApprovalService approval)
    {
        this.session = session;
        this.approval = approval;
        inventory = new InventoryService(session);
    }

    /// <summary>허브와 동일한 날짜 계산(calendar:day + 1).</summary>
    public int DayIndex
    {
        get {
            int day = session != null && session.intVars.TryGetValue("calendar:day", out int value) ? value : 0;
            return Mathf.Max(1, day + 1);
        }
    }

    public bool HasGiftedToday(string companionId)
    {
        if (session == null || string.IsNullOrEmpty(companionId))
        {
            return false;
        }

        return session.intVars.TryGetValue(LastGiftDayPrefix + companionId, out int lastDay) && lastDay >= DayIndex;
    }

    public bool CanGift(string companionId, string giftId, out string reason)
    {
        reason = string.Empty;
        GiftInfo gift = GiftCatalog.Get(giftId);
        if (gift == null)
        {
            reason = "선물할 수 없는 물건이다.";
            return false;
        }

        if (CompanionCatalog.Info(companionId) == null)
        {
            reason = "선물을 받을 상대가 없다.";
            return false;
        }

        if (HasGiftedToday(companionId))
        {
            reason = "오늘은 이미 이 동료에게 선물을 줬다.";
            return false;
        }

        if (inventory.GetCount(gift.id) <= 0)
        {
            reason = "보유한 선물이 없다. 장터에서 구입할 수 있다.";
            return false;
        }

        return true;
    }

    /// <summary>선물 지급: 아이템 1개 소모 → 호감/연애도 상승 → 하루 제한 기록.</summary>
    public GiftResult Give(string companionId, string giftId)
    {
        GiftResult result = new GiftResult();
        if (!CanGift(companionId, giftId, out string reason))
        {
            result.message = reason;
            return result;
        }

        GiftInfo gift = GiftCatalog.Get(giftId);
        if (!inventory.Consume(gift.id, 1))
        {
            result.message = "보유한 선물이 없다. 장터에서 구입할 수 있다.";
            return result;
        }

        int delta = gift.DeltaFor(companionId);
        result.success = true;
        result.delta = delta;
        result.wasFavorite = gift.IsFavoriteOf(companionId);
        result.approvalAfter = approval != null ? approval.Add(companionId, delta) : 0;
        session.intVars[LastGiftDayPrefix + companionId] = DayIndex;

        string name = CompanionCatalog.Name(companionId);
        bool romance = approval != null && approval.CanApplyRomanticEffect(companionId);
        string gauge = romance ? "연애도" : "유대";
        result.message = result.wasFavorite
                             ? $"{name}에게 {gift.displayName}을(를) 건넸다. 최애 선물! {gauge} +{delta}."
                             : $"{name}에게 {gift.displayName}을(를) 건넸다. {gauge} +{delta}.";
        if (result.wasFavorite && !string.IsNullOrEmpty(gift.favoriteReaction))
        {
            result.message += "\n" + gift.favoriteReaction;
        }

        return result;
    }
}
}
