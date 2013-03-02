AsfMojo
=======

AsfMojo is an open source .NET ASF parsing library, providing support for parsing Windows Media Audio (WMA) and Windows Media Video (WMV) files.  It offers classes to create streams from packet data within a media file, gather file statistics and extract audio segments or frame accurate still frames. The library parses ASF objects as per the ASF specification revision 01.20.05

AsfMojo comes with a WPF UI application and a command line utility application that both make use of the AsfMojo core library.

The AsfMojo WPF UI app is a full fledged ASF Parser (similar to Windows Media AsfView 9) offering a simple but powerful interface into the structure of ASF files down to the packet level. Still frames and audio for every payload can be previewed (based on presentation time). The UI makes use of NAudio to calculate and display the waveform of audio data.

The AsfMojo command line utility offers a few streamlined options to access the AsfMojo library, such as updating its content properties(Author, Description etc.), creating thumb images from an offset within a media file or extracting a WAVE file from a time range within a media file.

AsfMojo core library is also available as NuGet package - install via the packet manager Install-Package AsfMojo to test and try it out!
