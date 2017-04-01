# Ao3Track
Source code to Ao3Track

## Files and Directories
* Ao3TrackReader/ - Source for Ao3Track Reader Xamarin Apps (C# and Xaml)
* Ao3tracksync/ - Soruce for Ao3Track Cloud Server ASP.Net Web Api WebApp (C#)
* ao3_tracker/ - Source for Web Extension and Reader injected scripts (TypeScript)
* Ao3tracksync.sln - Visual Studio 2017 solution for Ao3TrackReader and Ao3tracksync
* Ao3tracksync.Win81.sln - Visual Studio 2015 solution for Windows 8 version of Ao3TrackReader 

## Building

### Ao3TrackReader
ao3_tracker must be built before Ao3TrackReader can be built. Xamarin extensions for Visual Studio are required. Restore Nuget packages to build.

### Ao3TrackReader.Win81
Can only be built in Visual Studio 2015. C#7 features will cause Intellisense errors but the projects will compile and build without error.

In order to enable Intellisense support for C#7 features in Visual Studio 2015 a Rosyln 2.0 Insider Build VSIX needs to be installed. This specific extension build is known to work [https://dotnet.myget.org/feed/roslyn/package/vsix/eb2680f2-4e63-44a8-adf6-2e667d9f689c/2.0.0.6110410](https://dotnet.myget.org/feed/roslyn/package/vsix/eb2680f2-4e63-44a8-adf6-2e667d9f689c/2.0.0.6110410). Other builds may not work and break Visual Studio. If you need to uninstall the VSIX, you must: 

    - Close all instances of VS 
    -	delete %LocalAppdata%\Microsoft\VisualStudio\14.0\ (Note, this will delete all your extensions, not just the Roslyn VSIX) 
    - run `devenv /updateconfiguration` from a developer command prompt 

### Ao3tracksync
Restore Nuget packages to build.

### ao3_tracker
Node.js required. TypeScript >=2.1 required. Builds using Gulp 4. See instructions in directory for more info

