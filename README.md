# Ao3Track
Source code to Ao3Track

## Files and Directories
* LICENSE - Apache License Version 2.0 terms and conditions
* README.md - This file
* Ao3TrackReader/ - Source for Ao3Track Reader Xamarin Apps (C# and Xaml)
* Ao3tracksync/ - Soruce for Ao3Track Cloud Server ASP.Net Web Api WebApp (C#)
* ao3_tracker/ - Source for Web Extension and Reader injected scripts (mostly TypeScript)
* Ao3tracksync.sln - Visual Studio 2017 solution for Ao3TrackReader and Ao3tracksync
* Ao3tracksync.Win81.sln - Visual Studio 2015 solution for Windows 8.1 versions of Ao3TrackReader 

## Building

### Ao3TrackReader
* ao3_tracker must be built before Ao3TrackReader can be built.
* Xamarin for Visual Studio is required for Droid and iOS project.

### ao3_tracker
Node.js required. Builds using Gulp 4.

#### Set up build environment:
`> cd ao3_tracker`

`> npm install -g gulp-cli typescript typings less`

`> npm install`

`> typings install`

#### Building 

##### All projects
`> gulp`

##### Ao3Track Reader Scripts and CSS
`> gulp reader`

##### Ao3Track Reader Scripts and CSS for UWP
`> gulp reader.uwp`

##### Ao3Track Reader Scripts and CSS for Droid
`> gulp reader.droid`

##### Ao3Track Reader Scripts and CSS for iOS
`> gulp reader.ios`

## Visual Studio Projects

### Ao3TrackReader
Shared Project containing main Ao3Track Reader code.

### Ao3TrackReader.Droid (VS 2017)
Xamarin Droid project containing for the Droid App. Need Xamarin and Android 7.1 SDK 

References
* Ao3TrackReader
* Ao3TrackReader.Helper
* Ao3TrackReader.Version

Prerequisites
* Ao3TrackReader.Version.Build

### Ao3TrackReader.Helper
Shared Project containing shared code and interfaces used by Ao3Track Reader as part of the bridge between the C# App code and the injected JavaScript 

### Ao3TrackReader.Helper.Messaging
Shared Project containing shared code and interfaces used but platforms that don't support injecting a native object into Javascript (iOS, Win81).

### Ao3TrackReader.Helper.UWP
Windows Runtime component for Windows 10 App containing the native class that is injected into Javascript

References
* Ao3TrackReader.Helper
* Ao3TrackReader.Version

### Ao3TrackReader.iOS (VS 2017)
Xamarin iOS project containing for the iOS App

References
* Ao3TrackReader
* Ao3TrackReader.Helper
* Ao3TrackReader.Helper.Messaging
* Ao3TrackReader.Version

Dependencies
* Ao3TrackReader.Version.Build

### Ao3TrackReader.Version
Shared project containing the version information used by Ao3TrackReader project

### Ao3TrackReader.Version.Build
.Net console application used to update version info stored in various project xml files. Files are updated using Post Build event when build output changed.

References
* Ao3TrackReader.Version

### Ao3TrackReader.UWP  (VS 2017)
Windows Universal Project for the Windows 10 App. Requires Windows 10 SDK 10.0.14393

References
* Ao3TrackReader
* Ao3TrackReader.Helper.UWP
* Ao3TrackReader.Version
* Ao3TrackReader.WinRT

Dependencies
* Ao3TrackReader.Version.Build

### Ao3TrackReader.Win81 (VS 2015)
Shared Project containing shared Windows 8.1 code

### Ao3TrackReader.Win81.Desktop (VS 2015)
Windows 8.1 project. Can only be built in Visual Studio 2015. 

References
* Ao3TrackReader
* Ao3TrackReader.Helper
* Ao3TrackReader.Helper.Messaging
* Ao3TrackReader.Version
* Ao3TrackReader.Win81
* Ao3TrackReader.WinRT

Dependencies
* Ao3TrackReader.Version.Build

### Ao3TrackReader.Win81.Phone (VS 2015)
Windows Phone 8.1 project. 

References
* Ao3TrackReader
* Ao3TrackReader.Helper
* Ao3TrackReader.Helper.Messaging
* Ao3TrackReader.Version
* Ao3TrackReader.Win81
* Ao3TrackReader.WinRT

Dependencies
* Ao3TrackReader.Version.Build

### Ao3TrackReader.WinRT
Shared Project containing shared Windows Runtime code. 

### Ao3tracksync (VS 2017)
ASP.Net Web Api WebApp for IIS. Included Web.Config sets the "Ao3TrackEntities" database datasource to use a SQL Server database at  ".\AO3TRACK" using windows authentication. 

## Note About Visual Studio 2015 and C#7
C#7 features will cause Intellisense errors but the projects will compile and build without error.

In order to enable Intellisense support for C#7 features in Visual Studio 2015 a Roslyn 2.0 Insider Build VSIX needs to be installed. This specific extension build is known to work [https://dotnet.myget.org/feed/roslyn/package/vsix/eb2680f2-4e63-44a8-adf6-2e667d9f689c/2.0.0.6110410](https://dotnet.myget.org/feed/roslyn/package/vsix/eb2680f2-4e63-44a8-adf6-2e667d9f689c/2.0.0.6110410). Other builds may not work and break Visual Studio. If you need to uninstall the VSIX, you must: 

 - Close all instances of VS 
 -	delete %LocalAppdata%\Microsoft\VisualStudio\14.0\ (Note, this will delete all your extensions, not just the Roslyn VSIX) 
 - run `devenv /updateconfiguration` from a developer command prompt 

