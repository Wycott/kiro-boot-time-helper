# Implementation Plan: dotnet-uptime-tracker

## Overview

Implement a .NET 10 console application that reads OS boot time, displays it once, and continuously refreshes a live uptime counter in-place. Color thresholds (warn/reboot/overdue) are read from `uptime-tracker.json` next to the executable. The solution consists of two projects: `UptimeTracker` (main) and `UptimeTracker.Tests` (xUnit + FsCheck).

## Tasks

- [x] 1. Create solution and project structure
  - Create the solution file and two projects: `UptimeTracker` (console) and `UptimeTracker.Tests` (xUnit + FsCheck)
  - Configure `UptimeTracker.csproj` targeting `net10.0` with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`; set `<Nullable>enable</Nullable>` in the test project as well
  - Add xUnit (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`) and FsCheck (`FsCheck`, `FsCheck.Xunit`) package references to `UptimeTracker.Tests`
  - Add a project reference from `UptimeTracker.Tests` to `UptimeTracker`
  - Confirm the solution builds and the test project runs (zero tests pass, zero fail)
  - _Requirements: all_

- [x] 2. Define data models and exceptions
  - [x] 2.1 Implement all data model records and `ConfigurationException`
    - Create `ConfigurationException` with `ExitCode = 1` and a message constructor
    - Create `FlashPair`, `WarnThreshold`, `RebootThreshold`, `OverdueThreshold`, `ThresholdConfiguration`, `AppConfiguration`, and `ColorState` (sealed abstract record with `Default`, `Warn`, `Reboot`, `Overdue` nested records) in the `UptimeTracker` project
    - Create the JSON DTO records: `RawConfig`, `RawThreshold`, `RawFlashPair`
    - _Requirements: 3.1, 4.1, 5.1, 6.1_

- [x] 3. Implement `UptimeFormatter`
  - [x] 3.1 Implement `UptimeFormatter.Format`
    - Write `internal static class UptimeFormatter` with `public static string Format(TimeSpan uptime)` returning `"Xd HHh MMm SSs"` (e.g., `"3d 04h 22m 11s"`)
    - _Requirements: 2.2_

  - [x]* 3.2 Write example-based unit tests for `UptimeFormatter`
    - Cover all rows in the `UptimeFormatterTests` table from the design: `TimeSpan.Zero`, 1 s, 1 min, 1 h, 1 day, 3d 4h 22m 11s, 999d 23h 59m 59s
    - _Requirements: 2.2_

  - [x]* 3.3 Write property test P1 for `UptimeFormatter` — uptime format round-trip
    - `// Feature: dotnet-uptime-tracker, Property 1: Uptime format round-trip`
    - Generator: random `TimeSpan` in [0, 999 days] (whole seconds only)
    - Assert parsed components equal `(int)t.TotalDays`, `t.Hours`, `t.Minutes`, `t.Seconds`
    - _Requirements: 2.2_

- [x] 4. Implement `ThresholdResolver`
  - [x] 4.1 Implement `ThresholdResolver.Resolve`
    - Write `internal static class ThresholdResolver` with `public static ColorState Resolve(TimeSpan uptime, ThresholdConfiguration config, int flashTick)`
    - Priority: overdue ≥ reboot ≥ warn ≥ default; `flashTick % 2` selects PairA (even) or PairB (odd)
    - _Requirements: 4.1, 5.1, 6.1, 6.6_

  - [x]* 4.2 Write example-based unit tests for `ThresholdResolver`
    - Cover all boundary rows from the design (0 s, warn−1 s, warn, warn+1 s, reboot−1 s, reboot, reboot+1 s, overdue−1 s, overdue, overdue+1 s)
    - Cover flash alternation rows (flashTick 0–3, 100, 101)
    - Cover color configuration tests (defaults, custom foreground, custom background, default/custom flash pairs)
    - _Requirements: 4.1, 5.1, 6.1, 6.4, 6.6_

  - [x]* 4.3 Write property test P2 for `ThresholdResolver` — highest threshold wins
    - `// Feature: dotnet-uptime-tracker, Property 2: Threshold resolver — highest threshold wins`
    - Generator: random uptime (0–30 days) + random valid `ThresholdConfiguration` (three strictly ascending `after` values)
    - Assert the four mutually exclusive `ColorState` outcomes match the threshold ranges
    - _Requirements: 4.1, 5.1, 6.1, 6.6_

  - [x]* 4.4 Write property test P3 for `ThresholdResolver` — flash alternation correctness
    - `// Feature: dotnet-uptime-tracker, Property 3: Flash alternation within two Flash_Interval periods`
    - Generator: random sequence of consecutive `flashTick` integers (length 2–20) starting from a random non-negative integer, with uptime in overdue state
    - Assert `flashTick % 2 == 0` always yields PairA and `flashTick % 2 == 1` always yields PairB
    - _Requirements: 6.4_

- [x] 5. Checkpoint — ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement `ConfigLoader`
  - [x] 6.1 Implement `ConfigLoader.Load`
    - Write `internal static class ConfigLoader` with `public static AppConfiguration Load(string executableDirectory)`
    - Deserialize `uptime-tracker.json` using `System.Text.Json` into `RawConfig` DTOs
    - Apply validation in order: file existence, JSON parse, required keys, `after` format (HH:MM:SS, no negative components), ConsoleColor names (case-insensitive `Enum.TryParse`), ascending order, `flashIntervalMs` positive integer
    - Apply defaults: `warn.Foreground = Yellow`, `reboot.Foreground = Red`, default flash pairs (Red/White, White/Red), `FlashIntervalMs = 1000`, `TestMode = false`
    - Throw `ConfigurationException` with the exact message patterns from the design error table on any validation failure
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 6.8_

  - [x]* 6.2 Write example-based unit tests for `ConfigLoader`
    - Cover all valid-configuration rows from the design (minimal config, full config, testMode variants, flashIntervalMs: 1)
    - Cover all error-case rows from the design (missing file, missing keys, invalid `after` formats, invalid ConsoleColor names, non-ascending order, invalid flashIntervalMs)
    - Each error case asserts `ConfigurationException` is thrown and the message contains the expected substring
    - _Requirements: 3.1–3.7, 6.8_

  - [x]* 6.3 Write property test P4 for `ConfigLoader` — invalid `after` strings always rejected
    - `// Feature: dotnet-uptime-tracker, Property 4: Config validation rejects invalid 'after' values`
    - Generator: arbitrary strings filtered to exclude valid HH:MM:SS patterns
    - Assert `ConfigLoader.Load` throws `ConfigurationException` for any such string in any `after` field
    - _Requirements: 3.6_

  - [x]* 6.4 Write property test P5 for `ConfigLoader` — invalid ConsoleColor names always rejected
    - `// Feature: dotnet-uptime-tracker, Property 5: Config validation rejects invalid ConsoleColor names`
    - Generator: arbitrary strings filtered to exclude valid `ConsoleColor` member names (case-insensitive)
    - Assert `ConfigLoader.Load` throws `ConfigurationException` for any such string in any color field
    - _Requirements: 3.7_

  - [x]* 6.5 Write property test P6 for `ConfigLoader` — non-ascending threshold order always rejected
    - `// Feature: dotnet-uptime-tracker, Property 6: Config validation enforces ascending threshold order`
    - Generator: random `TimeSpan` triples `(a, b, c)` where `a >= b` or `b >= c`
    - Assert `ConfigLoader.Load` throws `ConfigurationException` for any such triple
    - _Requirements: 3.3_

- [x] 7. Checkpoint — ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement `IBootTimeProvider` and its implementations
  - [x] 8.1 Implement `IBootTimeProvider`, `SystemBootTimeProvider`, and `TestBootTimeProvider`
    - Write `internal interface IBootTimeProvider` with `DateTime GetBootTime()`
    - Write `SystemBootTimeProvider` using `DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64)`
    - Write `TestBootTimeProvider` accepting a `DateTime startTime` constructor parameter and returning it from `GetBootTime()`
    - _Requirements: 1.1, 8.1, 8.2_

- [x] 9. Implement `IConsoleWriter` and the production `ConsoleWriter`
  - [x] 9.1 Implement `IConsoleWriter` interface and `ConsoleWriter`
    - Write `internal interface IConsoleWriter` with `SetCursorPosition`, `CursorTop`, `Write`, `WriteLine`, `WriteLine()`, `ForegroundColor`, `BackgroundColor`, `ResetColor` members
    - Write `internal sealed class ConsoleWriter : IConsoleWriter` that delegates every member directly to `System.Console`
    - _Requirements: 2.4, 7.2_

- [x] 10. Implement `UptimeRenderer`
  - [x] 10.1 Implement `UptimeRenderer` and its render loop
    - Write `internal sealed class UptimeRenderer` with constructor `(IBootTimeProvider, ThresholdConfiguration, IConsoleWriter, bool testMode = false)` and `public Task RunAsync(CancellationToken)`
    - Record the cursor row after the boot-time line is printed; each iteration: compute uptime → `ThresholdResolver.Resolve` → `SetCursorPosition(0, savedRow)` → apply colors → write `UptimeFormatter.Format(uptime)` → `ResetColor`
    - Use `Task.Delay(config.Overdue.FlashIntervalMs, ct)` when in overdue state; `Task.Delay(1000, ct)` otherwise
    - On cancellation: call `ResetColor`, write a final `WriteLine()`, and return
    - _Requirements: 2.1, 2.3, 2.4, 4.1–4.5, 5.1–5.5, 6.1–6.7, 7.1–7.3_

  - [x]* 10.2 Write example-based unit tests for `UptimeRenderer` using fakes
    - Implement `FakeConsoleWriter` (records all calls) and use `TestBootTimeProvider` with a fixed `DateTime`
    - Cover boot-time line tests: written exactly once, contains formatted time, testMode label present/absent
    - Cover in-place rendering: subsequent writes use `SetCursorPosition` to saved row, column 0
    - Cover color state tests for all five states (Default, Warn, Reboot, Overdue PairA, Overdue PairB)
    - Cover cancellation/cleanup: `ResetColor` called, final `WriteLine()` written, no further uptime writes
    - _Requirements: 2.3, 2.4, 4.1–4.5, 5.1–5.5, 6.1–6.7, 7.1–7.3, 8.2, 8.3_

  - [x]* 10.3 Write property test P7 for `UptimeRenderer` — render loop uses configured flash interval
    - `// Feature: dotnet-uptime-tracker, Property 7: Render loop uses configured flash interval`
    - Generator: random valid `flashIntervalMs` values in [1, 10000]
    - Inject a fake delay provider that records delay values; assert the delay equals `flashIntervalMs` when in overdue state
    - _Requirements: 6.4, 6.7_

- [x] 11. Checkpoint — ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Implement `Program.Main` and wire everything together
  - [x] 12.1 Implement the explicit `Program` class with `static async Task<int> Main`
    - Write `internal sealed class Program` with `public static async Task<int> Main(string[] args)` — no top-level statements
    - Capture `DateTime.Now` as program start time before any other work
    - Call `ConfigLoader.Load(executableDirectory)` inside a try/catch for `ConfigurationException`; on catch write to `Console.Error` and return 1
    - Select `IBootTimeProvider` based on `AppConfiguration.TestMode`
    - Save original console colors (`Console.ForegroundColor`, `Console.BackgroundColor`)
    - Register `Console.CancelKeyPress` to cancel a `CancellationTokenSource`
    - Print the boot-time line (with ` [TEST MODE]` suffix when `testMode = true`) using `Console.WriteLine`
    - Construct `UptimeRenderer` with the production `ConsoleWriter` and call `RunAsync(cancellationToken)`
    - Return 0 on normal exit
    - _Requirements: 1.1–1.3, 2.1–2.4, 3.4, 7.1–7.3, 8.1–8.5_

  - [x]* 12.2 Write integration-style `ProgramTests`
    - Test: config file does not exist → exit code 1, stderr contains file path
    - Test: valid config with `CancellationToken` cancelled immediately → exit code 0, stderr empty
    - _Requirements: 1.3, 3.4, 7.1_

- [x] 13. Final checkpoint — ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at logical boundaries
- Property tests (P1–P7) validate universal correctness properties; unit tests validate specific examples and edge cases
- The `FakeConsoleWriter` and `FakeBootTimeProvider` test helpers are shared across multiple test classes — consider placing them in a `TestHelpers` folder within `UptimeTracker.Tests`
