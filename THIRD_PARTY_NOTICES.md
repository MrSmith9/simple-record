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

## Windows Media Foundation / Desktop Duplication API

These are built into Windows itself (part of Windows 10/11). ScreenRecorderLib
uses them internally to capture the screen and encode video. You don't
redistribute them and you don't need a separate license to use them -
they're already on every Windows 10/11 PC.

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
