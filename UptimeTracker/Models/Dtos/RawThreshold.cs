namespace UptimeTracker.Models.Dtos;

internal sealed record RawThreshold(string? After, string? Foreground, string? Background, RawFlashPair[]? Flash, int? FlashIntervalMs);
