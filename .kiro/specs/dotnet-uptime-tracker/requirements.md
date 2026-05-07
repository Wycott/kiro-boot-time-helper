# Requirements Document

## Introduction

A .NET Core 10 console application that monitors and displays system uptime in real time. The application shows when the machine was last booted and how long it has been running. It supports configurable time thresholds read from a config file that change the display color of the uptime readout — from default, to yellow (`warn`), to red (`reboot`), to flashing red-and-white (`overdue`) — giving operators a clear visual signal of how long a system has been running without a restart. The highest matching threshold always takes precedence. Each threshold supports optional color configuration using .NET `ConsoleColor` names; defaults apply when color fields are omitted.

## Glossary

- **Application**: The .NET Core 10 console application described in this document.
- **Boot_Time**: The date and time at which the operating system last started.
- **Uptime**: The elapsed duration since Boot_Time, expressed in days, hours, minutes, and seconds.
- **warn**: A threshold object read from the Config_File containing an `after` duration (expressed in `HH:MM:SS` format) and optional color fields, after which the uptime display changes to the configured or default foreground color (default: yellow).
- **reboot**: A threshold object read from the Config_File containing an `after` duration (expressed in `HH:MM:SS` format) and optional color fields, after which the uptime display changes to the configured or default foreground color (default: red).
- **overdue**: A threshold object read from the Config_File containing an `after` duration (expressed in `HH:MM:SS` format) and optional `flash` color pairs, after which the uptime display alternates between two configurable color pairs (default: red foreground on white background and white foreground on red background).
- **Display**: The console output area where Boot_Time and Uptime are rendered.
- **Config_File**: A JSON file named `uptime-tracker.json` located in the same directory as the Application executable, from which the Application reads threshold settings. All three threshold keys (`warn`, `reboot`, `overdue`) are required.
- **Threshold_Configuration**: The set of `warn`, `reboot`, and `overdue` objects read from the Config_File, each containing an `after` duration and optional color configuration.
- **ConsoleColor**: A named color value from the .NET `System.ConsoleColor` enumeration (e.g., `"Yellow"`, `"Red"`, `"White"`, `"Cyan"`). Color fields in the Config_File must use these names exactly (case-insensitive).
- **Flash_Pair**: An object with `foreground` and `background` fields (both ConsoleColor names) representing one of the two alternating color states used in the overdue flash effect.
- **Flash_Interval**: The duration, in milliseconds, that each Flash_Pair is displayed before switching to the other. Configured via the optional `flashIntervalMs` field in the `overdue` threshold object. Default: 1000 ms (1 second), so a full cycle (pair A → pair B → pair A) completes in 2 seconds by default.
- **Threshold_Color**: The optional color configuration for a `warn` or `reboot` threshold, consisting of a `foreground` field and an optional `background` field (both ConsoleColor names).
- **Test_Mode**: A boolean flag in the Config_File (`"testMode": true/false`). When `true`, the Application uses the time at which the Application process started as the Boot_Time instead of the actual OS boot time. Defaults to `false` when the field is absent. Intended to make it easy to observe color threshold transitions without waiting for real uptime to accumulate.

## Requirements

### Requirement 1: Display Boot Time

**User Story:** As an operator, I want to see when the machine was last booted, so that I can understand the system's restart history at a glance.

#### Acceptance Criteria

1. THE Application SHALL retrieve the Boot_Time from the operating system.
2. WHEN the Application starts, THE Display SHALL show the Boot_Time formatted as a human-readable local date and time (e.g., `yyyy-MM-dd HH:mm:ss`).
3. IF the Application cannot retrieve the Boot_Time, THEN THE Application SHALL display a descriptive error message and exit with a non-zero exit code.

---

### Requirement 2: Display Live Uptime

**User Story:** As an operator, I want to see how long the machine has been running, so that I can monitor system stability over time.

#### Acceptance Criteria

1. THE Application SHALL calculate Uptime as the difference between the current local time and Boot_Time.
2. THE Display SHALL show Uptime expressed as days, hours, minutes, and seconds (e.g., `3d 04h 22m 11s`).
3. WHEN the Application is running, THE Display SHALL refresh the Uptime value once per second.
4. THE Application SHALL update the Uptime in place on the console without scrolling or appending new lines.

---

### Requirement 3: Configure Color Thresholds

**User Story:** As an operator, I want to specify warning, reboot, and overdue uptime thresholds in a config file, so that the display alerts me when the machine has been running longer than expected.

#### Acceptance Criteria

1. THE Application SHALL read Threshold_Configuration from a JSON file named `uptime-tracker.json` located in the same directory as the Application executable, structured as follows:
   ```json
   {
     "testMode": true,
     "warn":    { "after": "02:00:00", "foreground": "Yellow" },
     "reboot":  { "after": "06:00:00", "foreground": "Red" },
     "overdue": { "after": "12:00:00", "flashIntervalMs": 500, "flash": [ { "foreground": "Red", "background": "White" }, { "foreground": "White", "background": "Red" } ] }
   }
   ```
   Color fields (`foreground`, `background`, `flash`) are optional; defaults apply when omitted. The `flashIntervalMs` field is also optional (default: 1000). The `testMode` field is optional (default: `false`).
2. THE Application SHALL require all three keys (`warn`, `reboot`, and `overdue`) to be present in the Config_File, each containing an `after` field expressed in `HH:MM:SS` format.
3. THE Application SHALL validate that the `after` value of `warn` is less than the `after` value of `reboot`, and the `after` value of `reboot` is less than the `after` value of `overdue`.
4. IF the Config_File is not present, THEN THE Application SHALL display a descriptive error message and exit with a non-zero exit code.
5. IF any required threshold key (`warn`, `reboot`, or `overdue`) is missing from the Config_File, THEN THE Application SHALL display a descriptive error message and exit with a non-zero exit code.
6. IF any `after` value in the Config_File is not a valid `HH:MM:SS` duration or contains a negative component, THEN THE Application SHALL display a descriptive error message and exit with a non-zero exit code.
7. IF any color field (`foreground`, `background`, or a color within a `flash` pair) contains a value that is not a valid ConsoleColor name, THEN THE Application SHALL display a descriptive error message and exit with a non-zero exit code.

---

### Requirement 4: Warning Color Display

**User Story:** As an operator, I want the uptime to change color when it meets or exceeds the warn threshold (but not a higher threshold), so that I receive an early visual alert.

#### Acceptance Criteria

1. WHILE Uptime is greater than or equal to the `warn` `after` value and less than the `reboot` `after` value, THE Display SHALL render the Uptime value using the configured Threshold_Color for `warn` (default: yellow foreground).
2. WHERE a `foreground` field is present in the `warn` threshold configuration, THE Application SHALL use the specified ConsoleColor as the foreground color for the warn display state.
3. WHERE a `background` field is present in the `warn` threshold configuration, THE Application SHALL use the specified ConsoleColor as the background color for the warn display state.
4. WHERE no color fields are present in the `warn` threshold configuration, THE Display SHALL render the Uptime value in yellow foreground with the default console background.
5. WHEN Uptime transitions from below the `warn` `after` value to at or above it, THE Display SHALL immediately apply the warn color on the next refresh cycle.

---

### Requirement 5: Critical Color Display

**User Story:** As an operator, I want the uptime to change color when it meets or exceeds the reboot threshold (but not the overdue threshold), so that I receive a strong visual alert.

#### Acceptance Criteria

1. WHILE Uptime is greater than or equal to the `reboot` `after` value and less than the `overdue` `after` value, THE Display SHALL render the Uptime value using the configured Threshold_Color for `reboot` (default: red foreground).
2. WHERE a `foreground` field is present in the `reboot` threshold configuration, THE Application SHALL use the specified ConsoleColor as the foreground color for the reboot display state.
3. WHERE a `background` field is present in the `reboot` threshold configuration, THE Application SHALL use the specified ConsoleColor as the background color for the reboot display state.
4. WHERE no color fields are present in the `reboot` threshold configuration, THE Display SHALL render the Uptime value in red foreground with the default console background.
5. WHEN Uptime transitions from below the `reboot` `after` value to at or above it, THE Display SHALL immediately apply the reboot color on the next refresh cycle.

---

### Requirement 6: Urgent Flashing Display

**User Story:** As an operator, I want the uptime to flash when it meets or exceeds the overdue threshold, so that I receive an unmistakable visual alert for critically long uptimes.

#### Acceptance Criteria

1. WHILE Uptime is greater than or equal to the `overdue` `after` value, THE Display SHALL alternate the Uptime text between the two configured Flash_Pairs (default: red foreground on white background, then white foreground on red background).
2. WHERE a `flash` array is present in the `overdue` threshold configuration containing exactly two Flash_Pair objects, THE Application SHALL use those two Flash_Pairs as the alternating color states for the overdue display.
3. WHERE no `flash` field is present in the `overdue` threshold configuration, THE Display SHALL alternate between red foreground on white background and white foreground on red background.
4. THE Display SHALL complete one full flash cycle (first pair → second pair → first pair) within two Flash_Interval periods.
5. WHEN Uptime transitions from below the `overdue` `after` value to at or above it, THE Display SHALL begin flashing on the next refresh cycle.
6. THE Application SHALL apply exactly one color format per refresh cycle, determined by the highest matching threshold: `overdue` takes priority over `reboot`, and `reboot` takes priority over `warn`.
7. WHERE the `flashIntervalMs` field is absent from the `overdue` threshold configuration, THE Application SHALL use a Flash_Interval of 1000 ms.
8. IF the `flashIntervalMs` field is present in the `overdue` threshold configuration and its value is not a positive integer (i.e., it is zero, negative, or not an integer), THEN THE Application SHALL display a descriptive error message and exit with a non-zero exit code.

---

### Requirement 7: Graceful Exit

**User Story:** As an operator, I want to stop the application cleanly, so that the console is left in a usable state after the application exits.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+C, THE Application SHALL stop the uptime refresh loop.
2. WHEN the Application exits, THE Application SHALL restore the console foreground and background colors to their values before the Application started.
3. WHEN the Application exits, THE Application SHALL print a final newline so that the terminal prompt appears on a new line.

---

### Requirement 8: Test Mode

**User Story:** As a developer, I want to enable a test mode via the config file, so that I can observe color threshold transitions immediately without waiting for real system uptime to accumulate.

#### Acceptance Criteria

1. WHEN `testMode` is `true` in the Config_File, THE Application SHALL use the time at which the Application process started as the Boot_Time instead of the actual OS boot time.
2. WHEN `testMode` is `true`, THE Display SHALL show the program start time (not the actual OS boot time) as the Boot_Time.
3. WHEN `testMode` is `true`, THE Display SHALL clearly indicate that test mode is active by showing a `[TEST MODE]` label alongside the boot time line.
4. WHEN `testMode` is `false` or the `testMode` field is absent from the Config_File, THE Application SHALL behave normally and use the actual OS boot time as the Boot_Time.
5. THE `testMode` field is optional; if absent it defaults to `false`.
