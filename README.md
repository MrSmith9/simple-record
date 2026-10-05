# Simple Record

A Windows desktop screen recorder, built step by step in Visual Studio using
C# and WPF (.NET 8).

This README covers everything built so far: the core recording engine
(Part 1), recording-source selection (Part 4), settings for save location
and recording quality (Part 2), an app icon (part of Part 6), and a
startup update check (Part 8). See the full roadmap at the bottom for
what's planned next.

> **Note on naming:** the app's display name (what you see in its window
> title and About screen) is "Simple Record". The project folder, file
> names, and `.exe` filename are `simple-record-by-mrsmith9`. Inside the
> C# code itself, things are organized under the name `SimpleRecord`
> instead - that's because C# code names aren't allowed to contain
> hyphens, so `simple-record-by-mrsmith9` couldn't be used there directly.
> You won't normally need to worry about this split; it's mentioned here
> only so it doesn't look like a mistake if you spot both names while
> browsing the code.

## What's built so far

- Choose what to record: your **whole screen** (primary monitor), a
  **specific open window**, or a **custom area** you drag out yourself.
  Only the primary monitor is supported for now - picking a window or
  area on a second monitor isn't available yet.
- Start, pause, resume, and stop controls - all keyboard accessible.
- A visible recording indicator (pulsing red dot + text) and a live timer.
  No sound is used anywhere - every status change is also shown as text.
- Asks for confirmation before every recording starts, naming exactly
  what's about to be recorded.
- Saves recordings to a folder you choose (Videos\Simple Record
  by default), lets you pick a fixed recording size (720p up to 8K), lets
  you choose your own picture as the main window's background, and lets
  you turn microphone audio on or off - see "Settings" below.
- An About window with the copyright notice.
- A custom app icon, shown in the title bar, taskbar, and on the `.exe`
  file itself.
- A way to publish a shareable `.exe` (see section 5 below).
- A startup check for newer versions, shown as a visual banner with a
  download link - see "Checking for updates" below.
- When recording a **Custom Area**, a thin red border now stays on screen
  the whole time, drawn exactly around the area being recorded, so it's
  always obvious which part of your screen is captured - see "Choosing
  what to record" below.

Not yet included (coming in later parts - see the roadmap below):
system/speaker audio, file format options beyond MP4, global keyboard
shortcuts, further theme polish, and a polished installer.

## Choosing what to record

Three buttons above the Start button let you choose the source before you
start recording (they're locked while a recording is in progress):

- **Whole Screen** - records the whole primary monitor. This is the default.
- **A Window** - opens a list of your currently open windows; pick one
  (arrow keys + Enter, or click + Select) and only that window is recorded.
- **Custom Area** - hides the app and shows a dimmed overlay over your
  screen; click and drag to draw a rectangle (or use arrow keys / Shift +
  arrow keys with no mouse at all), then confirm. Only that area is recorded,
  and the saved video is sized to exactly match it.

The currently selected button is highlighted, and the line underneath
always says exactly what will be recorded.

**While a Custom Area recording is running**, a thin red line stays drawn
around that exact area for as long as the recording lasts (including while
paused), so you always know what's inside the frame. You can still click
and type normally through it - it doesn't block your mouse or keyboard in
any way - and it never shows up in the saved video itself, only on your
own screen while you're recording. (Behind the scenes, this uses a Windows
feature called `SetWindowDisplayAffinity`, the same one apps like password
managers use to hide a window from screen shares - it needs Windows 10
version 2004/May 2020 Update or newer, which almost every PC has by now;
on a much older Windows 10, the red line would end up in the recording
too, which isn't dangerous, just not ideal.)

## Settings

Click **Settings** (top of the window, next to About) to open:

- **Save recordings to** - shows the current folder, with a **Browse...**
  button that opens a normal Windows folder picker. The default is
  `Videos\Simple Record`, same as before.
- **Recording quality (output size)** - choose the exact pixel size the
  video is saved at:
  - **Automatic (Recommended)** - saves at exactly the size of whatever
    you're recording (your screen's real resolution, the window's real
    size, or the area you dragged out). This is what the app always did
    before this setting existed.
  - **720p HD** (1280 x 720), **1080p Full HD** (1920 x 1080), **2K /
    1440p** (2560 x 1440), **4K UHD** (3840 x 2160), **8K UHD** (7680 x
    4320) - forces the saved video to exactly that size.

  Two things worth knowing: choosing a size **bigger** than what you're
  actually recording does not add real detail - a 1080p screen "recorded"
  at 4K just gets stretched up, it doesn't become sharper. And if the
  shape (aspect ratio) of what you're recording doesn't match the chosen
  size - for example a narrow window forced into a wide 16:9 size - black
  bars are added on the sides rather than the picture looking squashed.
- **Background image** - choose a picture of your own (JPG, PNG, BMP, or
  GIF) to show behind the main window, with a **Choose Image...** button
  that opens a normal Windows picture picker, and a **Remove** button to
  go back to the plain dark background. By default (and if you never set
  one) the plain dark background is used - there is no forced default
  picture anymore. Whatever you choose is copied into the app's own
  settings folder, so it keeps working even if you later move, rename, or
  delete the original picture file.
- **Microphone audio** - a checkbox to turn your microphone on or off for
  the next recording. Off (unchecked) by default, meaning the saved video
  has no sound at all, same as before this setting existed. When checked,
  your **default** microphone (whichever one is set as default in
  Windows) is recorded into the video's sound track - there's no picker
  yet to choose a specific microphone if you have more than one. System/
  speaker audio (recording sounds the PC itself is playing) isn't
  available yet - only the microphone.

Click **Save** to keep your changes (they're remembered for next time you
open the app) or **Cancel** to discard them. Everything in this window is
keyboard accessible (Tab between fields, Alt+underlined-letter for a
shortcut, Enter to Save, Escape to Cancel).

Settings, including your chosen background image, are stored under
`%AppData%\Simple Record\` - you never need to open or edit this
yourself, but it's there if you're curious or want to back it up.

## 1. Prerequisites

- **Visual Studio 2022** with the **".NET desktop development"** workload.
  - To check: open Visual Studio Installer > Modify (next to your VS 2022
    install) > make sure ".NET desktop development" is ticked.
- **.NET 8 SDK**. Recent Visual Studio 2022 installs (17.8+) include this
  automatically. If Visual Studio says it can't find `net8.0-windows`,
  install the SDK from https://dotnet.microsoft.com/download/dotnet/8.0.
- **Windows 10 (version 1903 or later) or Windows 11**.

## 2. Open the project

1. Open Visual Studio.
2. **File > Open > Project/Solution**.
3. Go to your `Documents\Windows app\simple-record-by-mrsmith9` folder and
   open `simple-record-by-mrsmith9.sln`.
   - If Visual Studio has any trouble with the `.sln` file, you can
     instead open `src\simple-record-by-mrsmith9\simple-record-by-mrsmith9.csproj`
     directly the same way - that works just as well.
4. Visual Studio will restore the NuGet package (`ScreenRecorderLib`)
   automatically. If it doesn't, right-click the solution in
   **Solution Explorer** and choose **Restore NuGet Packages**.

## 3. Check the build settings

At the top of Visual Studio, next to the green "Start" button, make sure:

- The configuration dropdown shows **Debug**.
- The platform dropdown shows **x64** (not "Any CPU"). This matters
  because the recording library uses native Windows components that need
  a specific platform.

## 4. Run it

Press **F5** (or click the green **Start** button). The app should open
as "Simple Record".

**Try this to test it:**

1. Press **Alt+S** (or click **Start Recording**).
2. A confirmation window appears - press **Enter** (or click **Start
   Recording** again) to confirm.
3. The red dot should start pulsing, the text should say "Recording",
   and the timer should start counting up.
4. Press **Alt+P** to pause - the dot turns blue/accent-colored and the
   text says "Paused". Press **Alt+P** again (now labeled "Resume") to
   continue.
5. Press **Alt+T** (or click **Stop Recording**).
6. After a moment, the status bar at the bottom will show the saved file
   path. Open your **Videos\Simple Record** folder to find it.

If Visual Studio shows a build error the first time, that's normal when
wiring up a new library for the first time - copy the exact error text
and we'll fix it together.

## 5. Make a shareable .exe (no installer yet)

This packages the app into its own folder with everything it needs to
run on another Windows PC - without needing Visual Studio, and without
needing .NET installed separately on that PC. It still produces an
ordinary `.exe` you (or a friend) double-click to run.

We're skipping a full installer (Start Menu shortcut, uninstaller, etc.)
for now, since this app is staying free/personal - every installer tool
we checked (Inno Setup, WiX) has recently added commercial-use licensing
wrinkles that don't apply to a free personal app, but weren't worth
adding complexity for right now. See "If you want a proper installer
later" below if that ever changes.

**Steps:**

1. In Visual Studio, open a terminal: **View → Terminal** (or **Tools →
   Command Line → Developer PowerShell**).
2. Make sure you're in the project folder. If not, run:
   ```
   cd "C:\Users\ksmit\Documents\Windows app\simple-record-by-mrsmith9\src\simple-record-by-mrsmith9"
   ```
3. Run:
   ```
   dotnet publish -c Release -r win-x64 -p:Platform=x64 --self-contained true
   ```
   - `-c Release` builds the optimized, final version (not the debug one).
   - `-r win-x64` targets 64-bit Windows (what almost every modern PC uses).
   - `-p:Platform=x64` explicitly tells the build "use the x64
     configuration." The project file already says x64 by default, but
     the `dotnet publish` command-line tool has a known quirk where it
     doesn't always honor that - passing it explicitly here avoids the
     "does not work correctly on 'AnyCPU' platform" error.
   - `--self-contained true` bundles the .NET runtime itself into the
     output, so the PC running it doesn't need .NET installed separately.

   **Important:** don't add `-p:PublishSingleFile=true` to this command.
   ScreenRecorderLib mixes native and managed code in one file (called a
   "mixed-mode" assembly), and .NET's single-file packaging doesn't
   support that - the publish would fail. The folder approach below
   avoids that problem entirely.

4. When it finishes, your shareable build is here:
   ```
   src\simple-record-by-mrsmith9\bin\x64\Release\net8.0-windows\win-x64\publish\
   ```
   (The extra `x64\` segment appears because the project now always
   builds as x64 instead of "Any CPU" - that's expected.)
   This whole folder (not just the `.exe` on its own) is what you share -
   the `.exe` needs the other files sitting next to it. Easiest way to
   share it: right-click the `publish` folder → **Send to → Compressed
   (zipped) folder**, then send that `.zip`.
5. To run it: open the `publish` folder (or the unzipped copy on another
   PC) and double-click `simple-record-by-mrsmith9.exe`.

Expect the file size to be noticeably bigger than before (roughly
150-200 MB) - that's the bundled .NET runtime, not a mistake.

**The first time it runs on a different PC**, Windows may show a blue
"Windows protected your PC" SmartScreen warning, because the `.exe`
isn't digitally signed (a code-signing certificate costs money and isn't
needed for personal use). Click **More info → Run anyway** to continue.
That's expected, not a sign something's wrong.

### If you want a proper installer later

If you ever want a polished setup wizard (Start Menu shortcut, an entry
in "Add or Remove Programs", an uninstaller), two free tools can do
that - just ask and we'll set one up:

- **Inno Setup** - simpler to write, produces a `.exe` installer. Its
  license file permits free commercial use, but its website now also
  asks commercial users to voluntarily buy a license.
- **WiX Toolset** - produces a real `.msi` (some workplaces specifically
  require this format). Recently added an "Open Source Maintenance Fee"
  that can apply if an app built with it earns revenue.

Neither of those licensing wrinkles affects a free personal app like
this one - they'd only matter if you later decided to sell the app.

## 6. Checking for updates

Every time the app starts, it quietly checks GitHub for a newer version in
the background (this never delays the window opening, and never shows an
error if you're offline - it just does nothing in that case). If a newer
version exists, a notice banner appears at the top of the window with a
**Get It** button (opens the download page in your browser) and a
**Dismiss** button. No sound is used - it's purely a visual notice, and
it's fully reachable by keyboard (Tab to it, Enter/Space to activate a
button).

This is already set up and live, pointed at your real GitHub repository:
**https://github.com/MrSmith9/simple-record**. If you ever rename the
GitHub repository again, just say so and we'll update
`src\simple-record-by-mrsmith9\Services\UpdateChecker.cs` to match - GitHub
also keeps the old URL working as a redirect after a rename, so nothing
breaks in the meantime either way.

### How to publish a new version

Each time you want the app to notice a new version is out:

1. Bump the version number in `simple-record-by-mrsmith9.csproj`, e.g.
   `<Version>0.5.0</Version>` -> `<Version>0.6.0</Version>`.
2. Publish the new build (see section 5 above) and zip the `publish`
   folder.
3. On GitHub, go to your repository -> **Releases** -> **Draft a new
   release**.
4. For the tag, type a **"v" followed by the exact same version number**,
   e.g. `v0.6.0` - this must match, since that's what the app compares
   against. Fill in a title/notes if you like.
5. Attach the zipped `publish` folder to the release as a file, then
   publish it.

The next time the app starts (on any PC running an older version), it
will see the new version is newer and show the banner.

## 3rd-party software and licensing

See `THIRD_PARTY_NOTICES.md` in this folder for the full explanation of
what ScreenRecorderLib is, why it's safe to use in a closed-source app,
and what was deliberately avoided (FFmpeg linking, OBS source code) and why.

Short version: everything used here (ScreenRecorderLib, and Windows' own
Media Foundation underneath it) is free to use in a personal or
commercial, closed-source Windows app, with no royalties and no
obligation to publish your own source code.

## Project structure

```
simple-record-by-mrsmith9/
  simple-record-by-mrsmith9.sln
  README.md
  THIRD_PARTY_NOTICES.md
  src/
    simple-record-by-mrsmith9/
      simple-record-by-mrsmith9.csproj
      App.xaml / App.xaml.cs            - app startup
      MainWindow.xaml / .xaml.cs        - main screen (buttons, timer, indicator)
      Views/
        ConfirmStartDialog.xaml(.cs)    - "start recording?" confirmation
        AboutWindow.xaml(.cs)           - About + copyright
        WindowSelectDialog.xaml(.cs)    - "choose a window" list
        RegionSelectWindow.xaml(.cs)    - "drag out a custom area" overlay
        RegionBoundaryOverlay.xaml(.cs) - red border shown during a Custom Area recording
        SettingsWindow.xaml(.cs)        - save location, quality, background, microphone
      Services/
        RecordingService.cs             - wraps the recording engine
        UpdateChecker.cs                - checks GitHub Releases for a newer version
        SettingsService.cs              - loads/saves settings.json in %AppData%
      Models/
        RecordingState.cs               - Idle / Recording / Paused
        RecordingSourceMode.cs          - FullScreen / Window / Region
        RecordingSourceSelection.cs     - the current "what to record" choice
        WindowInfo.cs                   - a simple, app-level window reference
        AppSettings.cs                  - save folder, quality, background, microphone choices
        VideoResolutionPreset.cs        - Automatic / 720p / 1080p / 2K / 4K / 8K
      Resources/
        Styles.xaml                     - colors, fonts, button styles
      Assets/
        AppIcon.ico                     - app icon (title bar/taskbar/.exe)
```

Note: there is no longer a bundled background image file. A chosen
background image is copied into `%AppData%\Simple Record\Background\`
instead (see "Settings" above), not stored inside the project.

Note: inside the C# code itself (the `namespace` lines at the top of each
`.cs`/`.xaml` file), everything is organized under `SimpleRecord` rather
than `simple-record-by-mrsmith9`, because C# doesn't allow hyphens in
names. This only matters if you're reading the code directly - it doesn't
affect how the app looks, runs, or is organized on disk.

Everything to do with the recording engine lives in `RecordingService.cs`.
The window code (`MainWindow.xaml.cs`) only ever calls
`Start()` / `Pause()` / `Resume()` / `Stop()` on it - it never talks to
ScreenRecorderLib directly. This keeps things organized as more features
get added, and means later parts (audio, area selection, quality
settings) mostly involve editing one file.

## Roadmap - what's next

1. ~~Part 1: Core recording engine (whole screen, start/pause/resume/stop,
   indicator, timer, permission step)~~ - **done**
2. ~~Part 2: Settings - choose the save folder and recording quality
   (720p-8K), remembered between runs.~~ - **done**. File format
   (currently always MP4/H.264) is not yet a choice.
3. **Part 3 (partial)**: Audio. ~~Microphone on/off~~ - **done**, see
   "Settings" above. System/speaker audio, and picking a specific
   microphone when you have more than one, are still open.
4. ~~Part 4: Choose what to record - a specific open window, or a
   custom rectangular area, not just the whole screen.~~ - **done**
   (primary monitor only for window/area selection, for now)
5. **Part 5**: Global keyboard shortcuts (start/stop from outside the
   app window) and a small always-on-top recording indicator so it's
   obvious recording is active even if the main window is minimized.
6. **Part 6 (partial)**: Visual polish. ~~Custom app icon~~ - **done**.
   Theme refinement and a possible high-contrast/light-dark toggle are
   still open.
7. ~~Part 7: Packaging - a shareable `.exe` via `dotnet publish`~~ -
   **done** (a polished installer with Start Menu/uninstaller is still
   optional and not yet built - see section 5 above).
8. ~~Part 8: Startup update check (GitHub Releases)~~ - **done** and
   fully live, pointed at your GitHub repo - see section 6 above.
9. ~~Part 9: Renamed the app to "Simple Record" (display name) and its
   project files to `simple-record-by-mrsmith9`~~ - **done**.
10. ~~Part 10: Replaced the forced default background image with a
    user-chosen one, set from Settings~~ - **done**.
11. ~~Part 11: Microphone on/off toggle (part of Part 3), and a visible
    red border around Custom Area recordings while they run~~ - **done**.
