# Remote for Windows Phone

[Official Website](https://wp8remote.com/)

Remote was a Windows Phone app that could remotely control iTunes and other media players via Apple's proprietary DACP protocol.
Since Windows Phone is long gone at this point, I thought it might be interesting to release this code for anyone curious about how it works.

This repository was converted and combined from two old SVN repositories.
I've tried to preserve as much of the history as possible, with a few minor changes (like retroactively converting CRLF line endings to LF, adding .gitignore files, etc.).
I've also included tags for each release, but since the two repositories were only merged at the very end, checking out an old tag might not include all of the necessary code.

Some of the features you'll find in this code:

* A `DACPServer` class to represent a DACP server (that really should have been called called DACPClient)
* Bonjour (DNS-SD/mDNS) implementation
* A simple HTTP server to help complete the iTunes pairing process (WP8 only)
* Various third-party libraries copy/pasted in (since NuGet used to be... not great)
* ***No tests whatsoever!!!*** 😱

This app was originally built for Windows Phone 7 and then later enhanced for Windows Phone 8.
There are separate directories for the WP7 and WP8 versions of the app, but the WP8 directories are mostly empty since those projects just link in files from the WP7 projects (with a few exceptions for WP8-specific functionality).

Unfortunately, Windows Phone 10 (er, *Windows 10 Mobile*) broke the Bonjour library since the OS started listening on UDP port 5353, making that port unavailable for older apps.
The only way to fix it would have been rewriting the app to target Windows 10.
I made some attempts at this, but I eventually lost interest as it became clear the mobile OS wasn't going to be supported very well by Microsoft.

You can find a few remnants of these attempts in CommonLibraries/UniversalLibraries, where there are UWP versions of the HTTP server and Bonjour libraries.
There is also a partial rewrite of the DACP library in CommonLibraries (this time with a more accurately-named `DacpClient` class) from when I was considering bringing the app to Windows 8.
