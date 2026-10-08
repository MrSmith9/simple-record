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

- A compact toolbar-style window (instead of a big full-size window) -
  its own title bar with a **☰ menu** (Settings, About, Exit) and a
  close button, a row of icon buttons to choose what to record and
  start recording, and a live status row that replaces it while
  recording. Click-and-drag the title bar to move the window, the same
  as any other window. See "The redesigned window" below.
- Choose between a **Dark** (default) or **Light** color theme for every
  window in the app - see "Settings" below.
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
  by default), lets you pick a fixed recording size (720p up to 8K), and
  lets you turn microphone audio on or off - see "Settings" below.
- An About window with the copyright notice.
- A custom app icon, shown in the title bar, taskbar, and on the `.exe`
  file itself.
- A way to publish a shareable `.exe` (see section 5 below), and a
  proper Windows installer (`setup.exe`) that installs it with a Start
  Menu shortcut and an uninstaller - see "Make a shareable installer
  (setup.exe)" below.
- A startup check for newer versions, shown as a visual banner with a
  download link - see "Checking for updates" below.
- When recording a **Custom Area**, a thin red border now stays on screen
  the whole time, drawn exactly around the area being recorded, so it's
  always obvious which part of your screen is captured - see "Choosing
  what to record" below.

Not yet included (coming in later parts - see the roadmap below):
system/speaker audio, file format options beyond MP4, global keyboard
shortcuts, and a high-contrast theme.

## The redesigned window

As of version 0.8.0, Simple Record is a small toolbar-style window
instead of a big centered one - inspired by a screen-capture tool called
CaptureWiz that Kyle shared a picture of, though not an exact copy of it.
A few things work differently because of this:

- **No normal Windows title bar.** The bar at the top (showing the app
  icon and "Simple Record") is drawn by the app itself. To move the
  window, click and hold anywhere on that bar (except the two buttons on
  the right) and drag, the same as dragging a normal title bar.
  (Version 0.9.0 removed the multi-color gradient stripe that used to
  sit above this bar, and the optional background-image feature, in
  favor of the plain Dark/Light theme described in "Settings" below.)
- **The ☰ button** (top-right) opens a small menu with **Settings**,
  **About**, and **Exit** - these used to be their own buttons in the
  header; they're grouped into this menu now to keep the window small.
  Arrow keys move through the menu, Enter opens the selected item, Esc
  closes it.
- **The ✕ button** (top-right corner) closes the app, the same as the
  normal Windows close button would.
- **Alt+F4 still closes the app too** - wired by hand, since a window
  without the normal Windows chrome can't always be assumed to get this
  automatically.

## Choosing what to record

A row of icon buttons lets you choose the source before you start
recording (they're locked while a recording is in progress):

- **Whole Screen** - records the whole primary monitor. This is the default.
- **A Window** - opens a list of your currently open windows; pick one
  (arrow keys + Enter, or click + Select) and only that window is recorded.
- **Custom Area** - hides the app and shows a dimmed overlay over your
  screen; click and drag to draw a rectangle (or use arrow keys / Shift +
  arrow keys with no mouse at all), then confirm. Only that area is recorded,
  and the saved video is sized to exactly match it.
- **Record** - the fourth icon button in that same row; starts recording
  using whichever source is currently selected (same as the old "Start
  Recording" button).

The currently selected source button gets a highlighted background and
border, and the line underneath the row always says exactly what will be
recorded. Once recording starts, this whole row is replaced by a status
row (the pulsing dot, "Recording"/"Paused" text, the timer, and
Pause/Stop icon buttons) - it switches back once you stop.

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

## Bookmarks, clips, and screenshots

New in version 0.11.0.

- **Bookmark** - a button that appears in the recording-status row
  (alongside Pause/Stop), or the **F8** key - marks "something important
  just happened here" while you're recording or paused. It doesn't save
  anything by itself; it just remembers the moment, so you can decide
  what to do with it once the recording finishes. You can set as many
  bookmarks as you like during one recording.
- **Screenshot** - a button in either toolbar row, or the **F9** key -
  saves an instant picture (`.png`) of your whole primary screen right
  away, into the same folder as your recordings. This works at any
  time, whether or not a recording is running, and always captures the
  whole screen regardless of what source (window/area) is currently
  selected for recording.
- Both **F8** and **F9** only work while the Simple Record window itself
  has focus (not system-wide "global" shortcuts that work from inside
  another app/game) - this was a deliberate choice to keep the first
  version of this feature simpler and lower-risk; true global hotkeys
  are still tracked as a possible future addition (see the Roadmap).
  Both actions are also always reachable as ordinary toolbar buttons
  (Tab + Enter/Space), not just via the keys.

**After a recording that had at least one bookmark finishes**, a
**Bookmarks** window opens automatically, listing each bookmark with its
timestamp and two buttons:

- **Export Clip** - saves a short trimmed `.mp4` (a few seconds before
  and after the bookmarked moment) next to your full recording, named
  `<recording name>_bookmark1_clip.mp4`, `..._bookmark2_clip.mp4`, and so
  on. This uses a Windows-provided video-editing feature and re-encodes
  the trimmed section, so it takes a few seconds to build, and its
  quality is capped at 1080p regardless of what quality the original
  recording was made at.
- **Export GIF** - saves a short animated `.gif` (same few-seconds
  window) built from the same periodic snapshot pictures the whole-
  recording GIF feature uses (see "How the GIF export works" below) -
  named `<recording name>_bookmark1.gif`, and so on.

You can export as many or as few bookmarks as you like, in either or
both formats, and closing the Bookmarks window at any point - even
without exporting anything - is completely safe; your full recording is
already saved either way, and nothing is lost by just closing the
window.

**A few known limits of this first version**, worth knowing about:
- The clip/GIF window around a bookmark is fixed at 3 seconds before and
  3 seconds after (not adjustable yet).
- Matching a bookmark's exact moment to the right snapshot pictures for
  "Export GIF" is an approximation (based on when each snapshot picture
  was saved to disk), not a frame-perfect match - it can drift slightly
  if the recording was paused and resumed before the bookmark.
- "Export Clip" needs the Windows feature mentioned above, which
  required a project-level change (the exact Windows SDK version the
  app is built against) - if clip export doesn't work at all for you,
  this is the most likely thing to check first; GIF export doesn't
  depend on this change and should be unaffected either way.

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
- **Appearance** - choose **Dark** (the default) or **Light** as the
  color theme for every window in the app. Click **Save** and the main
  window reopens right away with the new colors - no restart needed,
  unless a recording is in progress, in which case it shows the next
  time you open Simple Record instead (see "How the theme switch works"
  below).
- **Microphone audio** - a checkbox to turn your microphone on or off for
  the next recording. Off (unchecked) by default, meaning the saved video
  has no sound at all, same as before this setting existed. When checked,
  your **default** microphone (whichever one is set as default in
  Windows) is recorded into the video's sound track - there's no picker
  yet to choose a specific microphone if you have more than one. System/
  speaker audio (recording sounds the PC itself is playing) isn't
  available yet - only the microphone.
- **Also save an animated GIF** - a checkbox to also save a `.gif` copy
  of every recording, alongside the normal `.mp4` video, with the same
  file name. Off (unchecked) by default. A GIF has no sound and far
  fewer colors than video, and gets large quickly for anything longer
  than a short clip, so this is an extra file for sharing short demos
  easily (for example, pasting straight into Discord or GitHub, which
  both play GIFs automatically without needing a video player) - it is
  never a replacement for the video. See "How the GIF export works"
  below for how it's built.
- **Updates** - shows the version you're currently running, plus a
  **Check for Updates** button that checks GitHub right now instead of
  waiting for the automatic check that happens when the app starts (see
  "Startup update check" below). It always gives a clear answer: a new
  version found (with a **Get It** button to open the download page), "you're
  using the latest version," or "couldn't check right now" if there's no
  internet connection or GitHub can't be reached.

Click **Save** to keep your changes (they're remembered for next time you
open the app) or **Cancel** to discard them. Everything in this window is
keyboard accessible (Tab between fields, Alt+underlined-letter for a
shortcut, Enter to Save, Escape to Cancel).

Settings, including your chosen theme, are stored under
`%AppData%\Simple Record\` - you never need to open or edit this
yourself, but it's there if you're curious or want to back it up.

### How the theme switch works

Every window picks its colors once, when it's first opened - WPF doesn't
repaint an already-open window live when a color setting changes. So
when you save a new theme choice, Simple Record closes the main window
and immediately reopens a fresh one in its place - that's why it
flickers closed and reappears right after you click Save. Settings and
About pick up the current theme automatically too, since they're
freshly opened each time you use them - this also required reloading
the app's shared button/label styles at the same time as the theme
colors (not just the colors on their own), otherwise a window that had
already been opened once under the old theme could keep showing some of
the old theme's colors even after being reopened.

The one exception: if a recording is in progress when you change the
theme, the main window **can't** be safely closed and reopened mid
recording, so in that case your choice is saved but won't show until you
close and reopen the app normally afterward. The status bar message
tells you which of the two happened.

### How the GIF export works

When "Also save an animated GIF" is checked, the app does **not** create
the GIF by converting the finished `.mp4` afterward - instead, while the
recording is running, it also asks ScreenRecorderLib to save a still
picture (a `.png`) of the screen every 200 milliseconds (5 pictures per
second) into a temporary folder next to the video. As soon as the
recording stops and the video file is finished, those still pictures are
stitched together into one looping `.gif` with the same name as the
video, and the temporary folder is deleted.

This approach was chosen over decoding the finished video because it
avoids needing a video-decoding library at all (keeping the "no
FFmpeg/GPL dependencies" rule below), at the cost of the GIF being a
slightly different, lower (5fps) frame rate than the video itself - which
is normal and expected for GIFs, and keeps the file size reasonable.

If building the GIF fails for any reason (for example, running out of
disk space while writing the temporary pictures), the status bar says so
clearly, but **the video recording itself is never affected** - the video
is always saved normally whether or not the GIF succeeds.

## 1. Prerequisites

- **Visual Studio 2022** with the **".NET desktop development"** workload.
  - To check: open Visual Studio Installer > Modify (next to your VS 2022
    install) > make sure ".NET desktop development" is ticked.
- **.NET 8 SDK**. Recent Visual Studio 2022 installs (17.8+) include this
  automatically. If Visual Studio says it can't find `net8.0-windows10.0.19041.0`,
  install the SDK from https://dotnet.microsoft.com/download/dotnet/8.0.
- **Windows 10 SDK, version 10.0.19041.0 (2004/May 2020 Update) or later installed in Visual Studio**.
  As of version 0.11.0, the project targets this specific Windows SDK
  version (not just "Windows" generically) so it can use a couple of
  Windows-provided features directly (see the .csproj's own comment on
  `TargetFramework`). Visual Studio normally has this already via the
  ".NET desktop development" workload; if it's missing, Visual Studio
  will offer to install it the first time you open or build the project
  after this change - just say yes to that prompt.
- **Windows 10 (version 1809 or later) or Windows 11** to *run* the built
  app - this is a separate, lower number from the SDK version above
  (that one is what you build *against*; this one is what you can run
  the finished app *on*).

## 2. Open the project

1. Open Visual Studio.
2. **File > Open > Project/Solution**.
3. Go to your `Documents\Windows app\simple-record-by-mrsmith9` folder and
   open `simple-record-by-mrsmith9.sln`.
   - If Visual Studio has any trouble with the `.sln` file, you can
     instead open `src\simple-record-by-mrsmith9\simple-record-by-mrsmith9.csproj`
     directly the same way - that works just as well.
4. Visual Studio will restore the NuGet packages (`ScreenRecorderLib`,
   `Openize.Animated-GIF`, `System.Drawing.Common`) automatically. If it
   doesn't, right-click the solution in **Solution Explorer** and choose
   **Restore NuGet Packages**.

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

As of version 0.8.0 (the redesigned compact window - see "The redesigned
window" below), the icon buttons don't have Alt-key shortcuts of their
own anymore - **Tab** to move between them and **Enter** or **Space** to
press the focused one still works exactly as before, it's just no
longer also reachable by a specific Alt+letter combination. Clicking
with the mouse works the same as always.

1. Click (or Tab to and press Enter/Space on) the **Record** icon button.
2. A confirmation window appears - press **Enter** (or click **Start
   Recording** again) to confirm.
3. The red dot should start pulsing, the text should say "Recording",
   and the timer should start counting up.
4. Click **Pause** - the dot turns blue/accent-colored and the text says
   "Paused". Click it again (now labeled "Resume") to continue.
5. Click **Stop**.
6. After a moment, the status bar at the bottom will show the saved file
   path. Open your **Videos\Simple Record** folder to find it.

If Visual Studio shows a build error the first time, that's normal when
wiring up a new library for the first time - copy the exact error text
and we'll fix it together.

## 5. Make a shareable installer (setup.exe)

This is a two-step process: first you **publish** the app (bundles
everything it needs into one folder), then you **package** that folder
into a single `setup.exe` installer that someone else can download and
run to properly install Simple Record - Start Menu shortcut, an entry in
"Add or Remove Programs", and a working uninstaller included.

If you only want the plain folder-of-files version (no installer, just
a `.exe` you unzip and double-click), step 5a alone is enough - skip 5b.
But most people downloading your app will expect a normal installer, so
5b is recommended.

### 5a. Publish the app

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
   src\simple-record-by-mrsmith9\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\
   ```
   (The extra `x64\` segment appears because the project now always
   builds as x64 instead of "Any CPU" - that's expected. The
   `net8.0-windows10.0.19041.0\` segment changed name in version 0.11.0 -
   see "Prerequisites" above for why - if you're on an older version's
   instructions, it would say plain `net8.0-windows\` instead.)
   This whole folder is what step 5b packages into `setup.exe` - don't
   move, rename, or delete anything inside it before running step 5b.

   (If you only want the plain, no-installer folder version instead:
   this whole folder, not just the `.exe` on its own, is what you'd
   share - the `.exe` needs the other files sitting next to it. Easiest
   way to share it that way: right-click the `publish` folder → **Send
   to → Compressed (zipped) folder**, then send that `.zip`. Running it
   is just opening the folder and double-clicking
   `simple-record-by-mrsmith9.exe` - no install step at all.)

Expect the `publish` folder to be noticeably bigger than a normal
program (roughly 150-200 MB) - that's the bundled .NET runtime sitting
inside it, not a mistake. `setup.exe` itself (after step 5b) will be
smaller than that, because it compresses everything.

### 5b. Package it into setup.exe

This step uses a free, widely-used tool called **Inno Setup** to turn
the `publish` folder from step 5a into one `setup.exe` file. This only
needs to be done once per version you want to share - you've already
got the script that does it (`Installer\SimpleRecord.iss`), so you
won't need to write anything yourself here.

1. **Install Inno Setup** (only needed the first time): download it
   free from https://jrsoftware.org/isdl.php and run its own installer
   with the default options.
2. Make sure step 5a above has been run and finished without errors -
   `setup.exe` is built from that `publish` folder, so it needs to
   exist first.
3. In File Explorer, go to your project's `Installer` folder:
   ```
   C:\Users\ksmit\Documents\Windows app\simple-record-by-mrsmith9\Installer
   ```
4. Double-click `SimpleRecord.iss`. This opens it in the **Inno Setup
   Compiler** window (Inno Setup associates `.iss` files with itself
   automatically when installed).
5. Click the green **Compile** (▶) button near the top-left, or press
   **F9**.
6. When it finishes (a few seconds), you'll see
   `Installer\Output\setup.exe`. That one file is the complete
   installer - it's what you attach to a GitHub Release (see section 6
   below) or send to someone directly.

**To test it:** double-click `setup.exe`. It should show a normal
Windows setup wizard - a license-free welcome page, an install location
page (defaults to Program Files), a checkbox for an optional desktop
shortcut, then an install progress bar, then a "Finished" page with an
option to launch the app right away. After installing, check that
**Simple Record** shows up in your Start Menu, and that it's listed in
**Settings → Apps → Installed apps** (where it can be uninstalled
normally, like any other Windows program).

**If you change the app's version number** (in
`simple-record-by-mrsmith9.csproj`), open `SimpleRecord.iss` in a text
editor first and update the `MyAppVersion` line near the top to match,
before re-publishing (5a) and re-compiling (5b) - otherwise the
installer will report the old version number to Windows even though it
contains the new build.

**The first time setup.exe runs on a different PC**, Windows may show a
blue "Windows protected your PC" SmartScreen warning, because it isn't
digitally signed (a code-signing certificate costs money and isn't
needed for personal use). Click **More info → Run anyway** to continue.
That's expected, not a sign something's wrong - the same thing happened
with the plain `.exe` before, and is normal for any unsigned app shared
this way.

One licensing note, for completeness: Inno Setup's license permits free
use, including for commercial apps, but its website separately invites
commercial users to voluntarily pay for a license. That doesn't apply
here either way, since this is a free personal app - just mentioning it
so nothing looks hidden. (**WiX Toolset** is the other common free
option, and produces a real `.msi` file instead of a `.exe` - some
workplaces specifically require that format - but it recently added an
"Open Source Maintenance Fee" that can apply if an app built with it
earns revenue. Neither wrinkle affects this app; say so if you ever want
to switch to WiX instead.)

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
   `<Version>0.5.0</Version>` -> `<Version>0.6.0</Version>` - and also
   update `MyAppVersion` near the top of `Installer\SimpleRecord.iss` to
   the same number, so the installer reports the correct version too.
2. Publish the new build and package it into `setup.exe` (see section 5
   above, steps 5a and 5b).
3. On GitHub, go to your repository -> **Releases** -> **Draft a new
   release**.
4. For the tag, type a **"v" followed by the exact same version number**,
   e.g. `v0.6.0` - this must match, since that's what the app compares
   against. Fill in a title/notes if you like.
5. Attach `Installer\Output\setup.exe` to the release as a file, then
   publish it.

The next time the app starts (on any PC running an older version), it
will see the new version is newer and show the banner.

## 3rd-party software and licensing

See `THIRD_PARTY_NOTICES.md` in this folder for the full explanation of
what ScreenRecorderLib and Openize.Animated-GIF are, why they're safe to
use in a closed-source app, and what was deliberately avoided (FFmpeg
linking, OBS source code, and a GPLv3 GIF library) and why.

Short version: everything used here (ScreenRecorderLib, Openize.Animated-
GIF, and Windows' own Media Foundation underneath it) is free to use in a
personal or commercial, closed-source Windows app, with no royalties and
no obligation to publish your own source code.

## Project structure

```
simple-record-by-mrsmith9/
  simple-record-by-mrsmith9.sln
  README.md
  THIRD_PARTY_NOTICES.md
  Installer/
    SimpleRecord.iss                 - Inno Setup script that builds setup.exe
    Output/                          - setup.exe appears here after compiling
                                        (created automatically, not checked in)
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
        SettingsWindow.xaml(.cs)        - save location, quality, appearance, microphone, GIF export
        BookmarksWindow.xaml(.cs)       - lists a recording's bookmarks, Export Clip/GIF per one
      Services/
        RecordingService.cs             - wraps the recording engine
        GifExporter.cs                  - builds a .gif from a recording's PNG snapshots
        ClipExporter.cs                 - builds a short trimmed .mp4 clip around a bookmark
        ScreenshotService.cs            - saves an instant .png of the primary screen
        UpdateChecker.cs                - checks GitHub Releases for a newer version
        SettingsService.cs              - loads/saves settings.json in %AppData%
        ThemeManager.cs                 - switches between the Dark/Light color dictionaries
      Models/
        RecordingState.cs               - Idle / Recording / Paused
        RecordingSourceMode.cs          - FullScreen / Window / Region
        RecordingSourceSelection.cs     - the current "what to record" choice
        WindowInfo.cs                   - a simple, app-level window reference
        AppSettings.cs                  - save folder, quality, theme, microphone, GIF export choices
        VideoResolutionPreset.cs        - Automatic / 720p / 1080p / 2K / 4K / 8K
        AppTheme.cs                     - Dark / Light
        Bookmark.cs                     - one marked moment (elapsed time) during a recording
        RecordingResult.cs              - data handed to the Bookmarks window once recording finishes
      Resources/
        Styles.xaml                     - fonts, button/card/field styles (not colors - see below)
        Theme.Dark.xaml                 - the Dark theme's color palette
        Theme.Light.xaml                - the Light theme's color palette
      Assets/
        AppIcon.ico                     - app icon (title bar/taskbar/.exe)
```

Note: there is no longer a background-image feature or a bundled
background image file - every window just uses the chosen theme's
plain background color (see "Settings" above).

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
5. **Part 5 (partial)**: Global keyboard shortcuts and a small always-on-top
   recording indicator. ~~In-app keyboard shortcuts for Bookmark (F8) and
   Screenshot (F9)~~ - **done**, see "Bookmarks, clips, and screenshots"
   above - but these only work while Simple Record's own window has
   focus. True **global** shortcuts (start/stop/bookmark/screenshot from
   outside the app window, e.g. while a game is focused) and the
   always-on-top recording indicator are still open.
6. **Part 6 (partial)**: Visual polish. ~~Custom app icon~~ - **done**.
   ~~Theme refinement~~ - **done**. ~~A Dark/Light theme toggle~~ -
   **done**, see item 14 below. A high-contrast theme is still open.
7. ~~Part 7: Packaging - a shareable `.exe` via `dotnet publish`, and a
   proper `setup.exe` installer (Start Menu shortcut, uninstaller, "Add
   or Remove Programs" entry) via Inno Setup~~ - **done** - see section
   5 above.
8. ~~Part 8: Startup update check (GitHub Releases)~~ - **done** and
   fully live, pointed at your GitHub repo - see section 6 above.
9. ~~Part 9: Renamed the app to "Simple Record" (display name) and its
   project files to `simple-record-by-mrsmith9`~~ - **done**.
10. ~~Part 10: Replaced the forced default background image with a
    user-chosen one, set from Settings~~ - **done**.
11. ~~Part 11: Microphone on/off toggle (part of Part 3), and a visible
    red border around Custom Area recordings while they run~~ - **done**.
12. ~~Part 12: a `setup.exe` installer via Inno Setup~~ - **done**.
13. ~~Part 13: redesigned the main window as a compact toolbar (icon
    buttons, custom title bar), inspired by a reference app called
    CaptureWiz~~ - **done**, confirmed working by Kyle (2026-10-07),
    aside from the two issues fixed in item 14 below.
14. ~~Part 14: removed the multi-color gradient stripe above the title
    bar and the background-image feature (both flagged by Kyle after
    testing item 13), and added a Dark/Light theme choice in Settings~~ -
    **done**, not yet confirmed working by Kyle.
15. ~~Part 15: fixed the Light theme's Settings text being unreadable
    (cached style colors), then fixed a startup crash that fix
    introduced, and added a manual "Check for Updates" button/section in
    Settings~~ - **done**, not yet confirmed working by Kyle.
16. ~~Part 16: "Also save an animated GIF" setting - captures periodic
    still pictures during recording and stitches them into a looping
    `.gif` once the video finishes, using the Apache-2.0-licensed
    Openize.Animated-GIF library~~ - **done**, not yet confirmed working
    by Kyle. See "How the GIF export works" above.
17. ~~Part 17: Bookmarks (mark a moment during recording, with a Bookmarks
    window afterward to export a short trimmed clip and/or GIF per
    bookmark) + in-app keyboard shortcuts for Bookmark (F8) and
    Screenshot (F9)~~ - **done**, not yet confirmed working by Kyle. See
    "Bookmarks, clips, and screenshots" above. This is the first feature
    that needed changing the project's TargetFramework (see the .csproj's
    comment) - worth double-checking the whole app still builds and runs
    normally, not just the new feature, since that kind of change is
    riskier than most.
