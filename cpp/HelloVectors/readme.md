# Introduction to using vector animation in a Win32 client app

![app gif](https://gph.is/g/Z5vVYo4)

Sample Demonstrates the following:

- Win32 window creation using C++/winrt
- Scenario 1: creating a simple ShapeVisual
- Scenario 2: creating a simple path using CompositionPath, Direct2D and ShapeVisual
- Scenario 3: creating a morph animation using ShapeVisual, two CompositionPath's and an animated CompositionPathGeometry 
- Scenario 4: playing vector animation output from LottieViewer tool
	Please Note: Follow these 2 steps to make scenario 4 work, if you get a compilation error 
		* Intall the prerelease Microsoft.UI.Xaml package 190131001-prerelease
		* Edit: …\HelloVectors\packages\Microsoft.UI.Xaml.2.1.190131001-prerelease\build\native\Microsoft.UI.Xaml.targets and replace <ItemGroup Condition="'$(TargetPlatformIdentifier)' == 'UAP'"> with <ItemGroup>