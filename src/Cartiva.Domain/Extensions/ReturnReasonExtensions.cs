using Cartiva.Domain.Enums;

namespace Cartiva.Domain.Extensions;

public static class ReturnReasonExtensions
{
    public static string ToValue(this ReturnReason reason) => reason switch
    {
        ReturnReason.DefectiveOrDamaged => "Defective or damaged",
        ReturnReason.WrongItemReceived => "Wrong item received",
        ReturnReason.DoesNotFit => "Does not fit",
        ReturnReason.NotAsDescribed => "Not as described",
        ReturnReason.ChangedMind => "Changed my mind",
        ReturnReason.Other => "Other",
        _ => reason.ToString()
    };

    public static ReturnReason FromValue(string value) => value switch
    {
        "Defective or damaged" => ReturnReason.DefectiveOrDamaged,
        "Wrong item received" => ReturnReason.WrongItemReceived,
        "Does not fit" => ReturnReason.DoesNotFit,
        "Not as described" => ReturnReason.NotAsDescribed,
        "Changed my mind" => ReturnReason.ChangedMind,
        "Other" => ReturnReason.Other,
        _ => Enum.Parse<ReturnReason>(value, true)
    };
}
