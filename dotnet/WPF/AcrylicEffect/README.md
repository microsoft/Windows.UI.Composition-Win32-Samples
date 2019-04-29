# WPF Effects Sample

!!! TODO !!! fill more of this out

temporarily, steps needed are as follows: 

- Clone
- NuGet Updates
   - Right click project > Manage NuGet Packages
   - Make sure 'Include prerelease' is checked
   - From Source nuget.org, install the following:
       - Win2D.uwp
       - Microsoft.Windows.SDK.Contracts
       - Microsoft.VCRTForwarders package

- Add an app.manifest to the project. 
   - At the bottom, add the following: 
 `<file name="Microsoft.Graphics.Canvas.dll">
    <activatableClass
      name="Microsoft.Graphics.Canvas.CanvasDevice"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
   <activatableClass
      name="Microsoft.Graphics.Canvas.UI.Composition.CanvasComposition"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
    <activatableClass
      name="Microsoft.Graphics.Canvas.Effects.SaturationEffect"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
    <activatableClass
        name="Microsoft.Graphics.Canvas.Effects.BlendEffect"
        threadingModel="both"
        xmlns="urn:schemas-microsoft-com:winrt.v1"/>
    <activatableClass
      name="Microsoft.Graphics.Canvas.Effects.GaussianBlurEffect"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
    <activatableClass
      name="Microsoft.Graphics.Canvas.Effects.ColorSourceEffect"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
    <activatableClass
      name="Microsoft.Graphics.Canvas.Effects.CompositeEffect"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
    <activatableClass
      name="Microsoft.Graphics.Canvas.Effects.OpacityEffect"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
    <activatableClass
      name="Microsoft.Graphics.Canvas.CanvasBitmap"
      threadingModel="both"
      xmlns="urn:schemas-microsoft-com:winrt.v1"/>
  </file>`


- Add Win2D dll
   - Go to official Win2d Nuget site - https://www.nuget.org/packages/Win2D.uwp/
   - Download win2d, then show in folder
   - Change extension to .zip
   - Extract zip, find Microsoft.Graphics.Canvas.dll. Copy and add to this project's main folder/bin/x64/Debug

- Build and Run as Debug, x64

## Features



## Run the sample
