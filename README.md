<h1 dir=auto>
<b>Kinect One (V2)</b>
<a style="color:#9966cc;" href="https://github.com/KinectToVR/Amethyst">Amethyst</a>
<text>device plugin</text>
</h1>

## **License**
This project is licensed under the GNU GPL v3 License 

## **Overview**
This repo is a pure implementation of the `ITrackingDevice` interface,  
providing Amethyst support for the Xbox One Kinect, using the 2.0 SDK.  
Both the handler and the plugin itself ([available here](https://github.com/KinectToVR/plugin_AzureKinect/tree/main/plugin_AzureKinect)) are written in C#

## **Downloads**
You're going to find built plugins in [repo Releases](https://github.com/KinectToVR/plugin_AzureKinect/releases/latest).

## **Build & Deploy**
Both build and deployment instructions [are available here](https://github.com/KinectToVR/plugin_AzureKinect/blob/main/.github/workflows/build.yml).
 - Ensure you have the Kinect SDK 2.0 (2.2) installed ([from here](https://www.microsoft.com/en-us/download/details.aspx?id=44561) or nuget)
 - Open in Visual Studio and publish using the prepared publish profile  
   (`plugin_AzureKinect` → `Publish` → `Publish` → `Open folder`)
 - Copy the published plugin to the `plugins` folder of your local Amethyst installation  
   or register by adding it to `$env:AppData\Amethyst\amethystpaths.k2path`
   ```jsonc
   {
    "external_plugins": [
        // Add the published plugin path here, this is an example:
        "F:\\source\\repos\\plugin_AzureKinect\\plugin_AzureKinect\\bin\\Release\\Publish"
    ]
   }
   ```

## **Wanna make one too? (K2API Devices Docs)**
[This repository](https://github.com/KinectToVR/Amethyst.Plugins.Templates) contains templates for plugin types supported by Amethyst.<br>
Install the templates by `dotnet new install Amethyst.Plugins.Templates::1.2.0`  
and use them in Visual Studio (recommended) or straight from the DotNet CLI.  
The project templates already contain most of the needed documentation,  
although please feel free to check out [the official wesite](https://docs.k2vr.tech/) for more docs sometime.

The build and publishment workflow is the same as in this repo (excluding vendor deps).  