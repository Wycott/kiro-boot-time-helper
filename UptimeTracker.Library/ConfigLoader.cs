using System.Text.Json;
using UptimeTracker.Exceptions;
using UptimeTracker.Models;
using UptimeTracker.Models.Dtos;

namespace UptimeTracker;

internal static class ConfigLoader
{
    /// <summary>
    /// Loads and validates the application configuration from uptime-tracker.json
    /// located in the given executableDirectory.
    /// Throws ConfigurationException with a descriptive message on any error.
    /// </summary>
    public static AppConfiguration Load(string executableDirectory)
    {
        // 1. File existence check
        var configPath = Path.Combine(executableDirectory, "uptime-tracker.json");

        if (!File.Exists(configPath))
        {
            throw new ConfigurationException($"Error: Configuration file not found: {configPath}");
        }

        // 2. JSON deserialization
        var json = File.ReadAllText(configPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        RawConfig? raw;
        
        try
        {
            raw = JsonSerializer.Deserialize<RawConfig>(json, options);
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Error: Failed to parse configuration file: {ex.Message}");
        }

        if (raw is null)
        {
            throw new ConfigurationException("Error: Configuration file is empty or null.");
        }

        // 3. Required keys
        if (raw.Warn is null)
        {
            throw new ConfigurationException("Error: Missing required key 'warn' in configuration file.");
        }

        if (raw.Reboot is null)
        {
            throw new ConfigurationException("Error: Missing required key 'reboot' in configuration file.");
        }

        if (raw.Overdue is null)
        {
            throw new ConfigurationException("Error: Missing required key 'overdue' in configuration file.");
        }

        // 4. Parse 'after' fields
        var warnAfter = ParseAfter(raw.Warn.After, "warn");
        var rebootAfter = ParseAfter(raw.Reboot.After, "reboot");
        var overdueAfter = ParseAfter(raw.Overdue.After, "overdue");

        // 5. ConsoleColor validation
        var warnForeground = ParseOptionalColor(raw.Warn.Foreground, "warn", "foreground")
                             ?? ConsoleColor.Yellow;
        var warnBackground = ParseOptionalColor(raw.Warn.Background, "warn", "background");

        var rebootForeground = ParseOptionalColor(raw.Reboot.Foreground, "reboot", "foreground")
                               ?? ConsoleColor.Red;
        var rebootBackground = ParseOptionalColor(raw.Reboot.Background, "reboot", "background");

        // Parse flash pairs for overdue
        FlashPair pairA;
        FlashPair pairB;

        if (raw.Overdue.Flash is null)
        {
            pairA = new FlashPair(ConsoleColor.Red, ConsoleColor.White);
            pairB = new FlashPair(ConsoleColor.White, ConsoleColor.Red);
        }
        
        if (raw.Overdue.Flash is { Length: >= 2 })
        {
            pairA = ParseFlashPair(raw.Overdue.Flash[0], "overdue", "flash[0]");
            pairB = ParseFlashPair(raw.Overdue.Flash[1], "overdue", "flash[1]");
        }
        else if (raw.Overdue.Flash is { Length: 1 })
        {
            pairA = ParseFlashPair(raw.Overdue.Flash[0], "overdue", "flash[0]");
            pairB = new FlashPair(ConsoleColor.White, ConsoleColor.Red);
        }
        else
        {
            pairA = new FlashPair(ConsoleColor.Red, ConsoleColor.White);
            pairB = new FlashPair(ConsoleColor.White, ConsoleColor.Red);
        }

        // 6. Ascending order check
        if (warnAfter >= rebootAfter || rebootAfter >= overdueAfter)
        {
            throw new ConfigurationException(
                "Error: Threshold 'after' values must be in ascending order: warn < reboot < overdue.");
        }

        // 7. flashIntervalMs validation
        var flashIntervalMs = 1000;

        if (raw.Overdue.FlashIntervalMs.HasValue)
        {
            if (raw.Overdue.FlashIntervalMs.Value <= 0)
            {
                throw new ConfigurationException(
                    $"Error: 'flashIntervalMs' in 'overdue' must be a positive integer (≥ 1). Got: {raw.Overdue.FlashIntervalMs.Value}.");
            }

            flashIntervalMs = raw.Overdue.FlashIntervalMs.Value;
        }

        // Build the configuration
        var warn = new WarnThreshold(warnAfter, warnForeground, warnBackground);
        var reboot = new RebootThreshold(rebootAfter, rebootForeground, rebootBackground);
        var overdue = new OverdueThreshold(overdueAfter, pairA, pairB, flashIntervalMs);
        var thresholds = new ThresholdConfiguration(warn, reboot, overdue);
        var testMode = raw.TestMode ?? false;

        return new AppConfiguration(thresholds, testMode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses an HH:MM:SS duration string where HH can be any non-negative integer,
    /// MM must be 0–59, and SS must be 0–59.
    /// </summary>
    private static TimeSpan ParseAfter(string? value, string key)
    {
        if (value is null)
        {
            throw new ConfigurationException(
                $"Error: Invalid 'after' value '' for '{key}'. Expected HH:MM:SS with non-negative components.");
        }

        var parts = value.Split(':');

        if (parts.Length != 3)
        {
            throw new ConfigurationException(
                $"Error: Invalid 'after' value '{value}' for '{key}'. Expected HH:MM:SS with non-negative components.");
        }

        if (!int.TryParse(parts[0], out var hours) || hours < 0 ||
            !int.TryParse(parts[1], out var minutes) || minutes < 0 || minutes > 59 ||
            !int.TryParse(parts[2], out var seconds) || seconds < 0 || seconds > 59)
        {
            throw new ConfigurationException(
                $"Error: Invalid 'after' value '{value}' for '{key}'. Expected HH:MM:SS with non-negative components.");
        }

        return new TimeSpan(hours, minutes, seconds);
    }

    /// <summary>
    /// Parses an optional ConsoleColor name. Returns null if the value is null/absent.
    /// Throws ConfigurationException if the value is present but not a valid ConsoleColor name.
    /// </summary>
    private static ConsoleColor? ParseOptionalColor(string? value, string key, string field)
    {
        if (value is null)
        {
            return null;
        }

        if (!Enum.TryParse<ConsoleColor>(value, ignoreCase: true, out var color))
        {
            throw new ConfigurationException(
                $"Error: '{value}' is not a valid ConsoleColor name in '{key}.{field}'.");
        }

        return color;
    }

    /// <summary>
    /// Parses a RawFlashPair into a FlashPair, validating both color fields.
    /// </summary>
    private static FlashPair ParseFlashPair(RawFlashPair raw, string key, string field)
    {
        var fg = ConsoleColor.White;
        var bg = ConsoleColor.Black;

        if (raw.Foreground is not null)
        {
            if (!Enum.TryParse<ConsoleColor>(raw.Foreground, ignoreCase: true, out fg))
            {
                throw new ConfigurationException(
                    $"Error: '{raw.Foreground}' is not a valid ConsoleColor name in '{key}.{field}.foreground'.");
            }
        }

        if (raw.Background is not null)
        {
            if (!Enum.TryParse<ConsoleColor>(raw.Background, ignoreCase: true, out bg))
            {
                throw new ConfigurationException(
                    $"Error: '{raw.Background}' is not a valid ConsoleColor name in '{key}.{field}.background'.");
            }
        }

        return new FlashPair(fg, bg);
    }
}
