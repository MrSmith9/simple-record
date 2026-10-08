# Third-Party Notices

Simple Record uses the following open-source software.

## ScreenRecorderLib

- Project: https://github.com/sskodje/ScreenRecorderLib
- License: MIT License
- Used for: capturing the screen and encoding video (H.264 / MP4), using
  Windows' own built-in Media Foundation and Desktop Duplication components.

The MIT License is one of the most permissive open-source licenses. In
plain terms, it lets you:

- Use the library in a closed-source, commercial, or personal app for free.
- Keep your own app's source code private - you do NOT have to open-source
  "Simple Record" because it uses an MIT-licensed library.
- Modify the library if you ever need to.

The only real condition is that the license notice below must stay
available somewhere in the project (this file, plus a credit in the
app's About window, covers that).

```
MIT License

Copyright (c) Sverre Kristoffer Skodje and contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

**Before your first public release**, open the `LICENSE` file in the
ScreenRecorderLib GitHub repository and copy the exact text here, just to
be sure it matches word for word.

## Openize.Animated-GIF

- Project: https://github.com/openize-com/openize-animated-gif-net
- License: Apache License 2.0
- Used for: building the optional animated .gif file (the "Also save an
  animated GIF" setting), from the still-image snapshots ScreenRecorderLib
  captures while a recording runs.

The Apache License 2.0 is another permissive open-source license, similar
in spirit to MIT. In plain terms, it lets you:

- Use the library in a closed-source, commercial, or personal app for free.
- Keep your own app's source code private - you do NOT have to open-source
  "Simple Record" because it uses an Apache-2.0-licensed library.
- Modify the library if you ever need to.

As with ScreenRecorderLib, the only real condition is that the license
notice stays available somewhere in the project (this file covers that).

```
Copyright 2023 Openize

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

**Before your first public release**, open the `LICENSE` file in the
Openize.Animated-GIF GitHub repository and copy the exact text here, just
to be sure it matches word for word.

## Windows Media Foundation / Desktop Duplication API / Windows.Media.Editing

These are built into Windows itself (part of Windows 10/11). ScreenRecorderLib
uses Media Foundation and Desktop Duplication internally to capture the
screen and encode video. As of version 0.11.0, `Services/ClipExporter.cs`
also uses Windows.Media.Editing directly (a different built-in Windows
feature, for trimming a short "clip" out of a recording around a
bookmarked moment - see that file's own comment for details). None of
these are redistributed or need a separate license to use - they're
already on every Windows 10/11 PC.

## What we deliberately avoided

- **FFmpeg** - FFmpeg itself is free, but depending on how it's built and
  linked into an app, it can carry LGPL or GPL obligations (for example,
  needing to let users replace the FFmpeg component, or in some cases
  needing to open-source code that links against it). We don't need
  FFmpeg at all, because Windows' own Media Foundation already does the
  video encoding - so this never becomes a concern for this app.
- **OBS Studio source code** - OBS itself is a great program, but its
  source code is licensed under the GPL. Reusing GPL-licensed code inside
  a closed-source app like this one would require open-sourcing this app
  too. We're not using any OBS code.
- **The "AnimatedGif" NuGet package (by mrousavy)** - this was the first
  GIF-encoding library considered for the "Also save an animated GIF"
  setting, but its LICENSE file is GPLv3, which has the same open-sourcing
  requirement as OBS above. Openize.Animated-GIF (Apache 2.0, see above)
  does the same job without that requirement, so that's what's used
  instead.
