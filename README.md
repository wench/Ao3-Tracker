# Ao3Track
Source code to Ao3Track

## Files and Directories
* Ao3TrackReader/ - Source for Ao3Track Reader Xamarin Apps (C# and Xaml)
* Ao3tracksync/ - Soruce for Ao3Track Cloud Server ASP.Net Web Api WebApp (C#)
* ao3_tracker/ - Source for Web Extension and Reader injected scripts (TypeScript)
* Ao3tracksync.sln - Visual Studio 2017 solution for Ao3TrackReader and Ao3tracksync

## Building

### Ao3TrackReader
ao3_tracker must be built before Ao3TrackReader can be built. Xamarin extensions for Visual Studio are required. Restore Nuget packages to build.

### Ao3tracksync
Restore Nuget packages to build.

### ao3_tracker
Node.js required. TypeScript >=2.1 required. Builds using Gulp 4. See instructions in directory for more info
