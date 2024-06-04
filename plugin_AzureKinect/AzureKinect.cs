// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Amethyst.Plugins.Contract;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace plugin_AzureKinect;

[Export(typeof(ITrackingDevice))]
[ExportMetadata("Name", "Azure Kinect")]
[ExportMetadata("Guid", "K2VRTEAM-AME2-APII-DVCE-DVCEKINECTAZ")]
[ExportMetadata("Publisher", "K2VR Team")]
[ExportMetadata("Version", "1.0.0.3")]
[ExportMetadata("Website", "https://github.com/KimihikoAkayasaki/plugin_AzureKinect")]
public class AzureKinect : ITrackingDevice
{
    private static readonly SortedDictionary<TrackedJointType, JointId> KinectJointTypeDictionary = new()
    {
        { TrackedJointType.JointHead, JointId.Head },
        { TrackedJointType.JointNeck, JointId.Neck },
        { TrackedJointType.JointSpineShoulder, JointId.Neck },
        { TrackedJointType.JointShoulderLeft, JointId.ShoulderLeft },
        { TrackedJointType.JointElbowLeft, JointId.ElbowLeft },
        { TrackedJointType.JointWristLeft, JointId.WristLeft },
        { TrackedJointType.JointHandLeft, JointId.HandLeft },
        { TrackedJointType.JointHandTipLeft, JointId.HandTipLeft },
        { TrackedJointType.JointThumbLeft, JointId.ThumbLeft },
        { TrackedJointType.JointShoulderRight, JointId.ShoulderRight },
        { TrackedJointType.JointElbowRight, JointId.ElbowRight },
        { TrackedJointType.JointWristRight, JointId.WristRight },
        { TrackedJointType.JointHandRight, JointId.HandRight },
        { TrackedJointType.JointHandTipRight, JointId.HandTipRight },
        { TrackedJointType.JointThumbRight, JointId.ThumbRight },
        { TrackedJointType.JointSpineMiddle, JointId.SpineChest },
        { TrackedJointType.JointSpineWaist, JointId.SpineNavel },
        { TrackedJointType.JointHipLeft, JointId.HipLeft },
        { TrackedJointType.JointKneeLeft, JointId.KneeLeft },
        { TrackedJointType.JointFootLeft, JointId.AnkleLeft },
        { TrackedJointType.JointFootTipLeft, JointId.FootLeft },
        { TrackedJointType.JointHipRight, JointId.HipRight },
        { TrackedJointType.JointKneeRight, JointId.KneeRight },
        { TrackedJointType.JointFootRight, JointId.AnkleRight },
        { TrackedJointType.JointFootTipRight, JointId.FootRight }
    };

    private static readonly SortedDictionary<JointConfidenceLevel, TrackedJointState> KinectJointStateDictionary = new()
    {
        { JointConfidenceLevel.None, TrackedJointState.StateNotTracked },
        { JointConfidenceLevel.Low, TrackedJointState.StateInferred },
        { JointConfidenceLevel.Medium, TrackedJointState.StateTracked },
        { JointConfidenceLevel.High, TrackedJointState.StateTracked }
    };

    [Import(typeof(IAmethystHost))] private IAmethystHost Host { get; set; }

    private Page InterfaceRoot { get; set; }
    private ComboBox TrackingTypeComboBox { get; set; }
    private ComboBox OrientationTypeComboBox { get; set; }

    private Device KinectDevice { get; set; } // The Kinect device
    private Tracker BodyTracker { get; set; } // The tracking component
    private bool PluginLoaded { get; set; }

    public bool IsPositionFilterBlockingEnabled => false;
    public bool IsPhysicsOverrideEnabled => false;
    public bool IsSelfUpdateEnabled => false;
    public bool IsFlipSupported => true;
    public bool IsAppOrientationSupported => true;
    public bool IsSettingsDaemonSupported => true;
    public object SettingsInterfaceRoot => InterfaceRoot;

    public bool IsInitialized { get; private set; }

    public bool IsSkeletonTracked { get; private set; }

    public int DeviceStatus => KinectDevice is not null ? 0 : 1;

    public ObservableCollection<TrackedJoint> TrackedJoints { get; } =
        // Prepend all supported joints to the joints list
        new(Enum.GetValues<TrackedJointType>().Where(x => x != TrackedJointType.JointManual)
            .Select(x => new TrackedJoint { Name = x.ToString(), Role = x }));

    public string DeviceStatusString => PluginLoaded
        ? DeviceStatus switch
        {
            0 => Host.RequestLocalizedString("/Plugins/AzureKinect/Statuses/Success"),
            1 => Host.RequestLocalizedString("/Plugins/AzureKinect/Statuses/NotAvailable"),
            _ => $"Undefined: {DeviceStatus}\nE_UNDEFINED\nSomething weird has happened, though we can't tell what."
        }
        : $"Undefined: {DeviceStatus}\nE_UNDEFINED\nSomething weird has happened, though we can't tell what.";

    public Uri ErrorDocsUri => new($"https://docs.k2vr.tech/{Host?.DocsLanguageCode ?? "en"}/one/troubleshooting/");

    public void OnLoad()
    {
        TrackingTypeComboBox = new ComboBox
        {
            Items =
            {
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Tracking/0"),
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Tracking/1"),
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Tracking/2"),
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Tracking/3"),
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Tracking/4")
            },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = Host.PluginSettings.GetSetting("Tracking", 4)
        };

        OrientationTypeComboBox = new ComboBox
        {
            Items =
            {
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Orientation/0"),
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Orientation/1"),
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Orientation/2"),
                Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Orientation/3")
            },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = Host.PluginSettings.GetSetting("Orientation", 0)
        };

        TrackingTypeComboBox.SelectionChanged += (sender, e) =>
        {
            if (sender is ComboBox { SelectedIndex: < 0 } box)
                box.SelectedItem = e.RemovedItems[0];

            // Save the new value to own plugin settings
            Host.PluginSettings.SetSetting("Tracking", TrackingTypeComboBox.SelectedIndex);
        };

        OrientationTypeComboBox.SelectionChanged += (sender, e) =>
        {
            if (sender is ComboBox { SelectedIndex: < 0 } box)
                box.SelectedItem = e.RemovedItems[0];

            // Save the new value to own plugin settings
            Host.PluginSettings.SetSetting("Orientation", TrackingTypeComboBox.SelectedIndex);
        };

        var trackingTextBlock = new TextBlock
        {
            Margin = new Thickness { Top = 2 },
            Text = Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Tracking/Select")
        };
        var orientationTextBlock = new TextBlock
        {
            Margin = new Thickness { Top = 2 },
            Text = Host.RequestLocalizedString("/Plugins/AzureKinect/Interface/Options/Orientation/Select")
        };

        InterfaceRoot = new Page
        {
            Content = new Grid
            {
                Children =
                {
                    trackingTextBlock, orientationTextBlock,
                    TrackingTypeComboBox, OrientationTypeComboBox
                },
                ColumnSpacing = 5.0,
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto
                    }
                },
                RowSpacing = 3.0,
                RowDefinitions =
                {
                    new RowDefinition
                    {
                        Height = new GridLength(1, GridUnitType.Star)
                    },
                    new RowDefinition
                    {
                        Height = new GridLength(1, GridUnitType.Star)
                    }
                }
            }
        };

        Grid.SetColumn(trackingTextBlock, 0);
        Grid.SetColumn(orientationTextBlock, 0);

        Grid.SetColumn(TrackingTypeComboBox, 1);
        Grid.SetColumn(OrientationTypeComboBox, 1);

        Grid.SetRow(trackingTextBlock, 0);
        Grid.SetRow(orientationTextBlock, 1);

        Grid.SetRow(TrackingTypeComboBox, 0);
        Grid.SetRow(OrientationTypeComboBox, 1);

        PluginLoaded = true;
    }

    public void Initialize()
    {
        IsInitialized = InitKinect();
        Host.Log($"Tried to initialize the Kinect sensor with status: {DeviceStatusString}");
    }

    public void Shutdown()
    {
        try
        {
            BodyTracker?.Shutdown();
            KinectDevice?.StopCameras();

            BodyTracker?.Dispose();
            KinectDevice?.Dispose();
        }
        catch (Exception e)
        {
            Host?.Log(e, LogSeverity.Error);
        }

        // Mark as not initialized
        IsInitialized = false;
    }

    public void Update()
    {
        try
        {
            if (KinectDevice is null || BodyTracker is null) return;

            // Queue latest frame from the sensor
            using var sensorCapture = KinectDevice.GetCapture();
            BodyTracker.EnqueueCapture(sensorCapture);

            // Try getting latest tracker frame
            using var lastFrame = BodyTracker.PopResult(TimeSpan.Zero);
            if (lastFrame is null) return;

            if (lastFrame.NumberOfBodies < 1)
            {
                IsSkeletonTracked = false;
                return; // Give up now...
            }

            // There's at least 1 body tracked
            IsSkeletonTracked = true;

            var skeleton = lastFrame.GetBodySkeleton(0);

            // Copy positions, orientations and states from the sensor
            // We should be able to address our joints by [] because
            // they're prepended via Enum.GetValues<TrackedJointType>
            foreach (var (appJoint, kinectJoint) in KinectJointTypeDictionary)
            {
                var tracker = TrackedJoints[(int)appJoint];
                (tracker.Position, tracker.Orientation, tracker.TrackingState) = (
                    skeleton.GetJoint(kinectJoint).Position,
                    skeleton.GetJoint(kinectJoint).Quaternion,
                    KinectJointStateDictionary[skeleton.GetJoint(kinectJoint).ConfidenceLevel]);
            }
        }
        catch (Exception e)
        {
            Host?.Log(e, LogSeverity.Error);
        }
    }

    public void SignalJoint(int jointId)
    {
        // ignored
    }

    private bool InitKinect()
    {
        try
        {
            // Try opening the no.1 device
            KinectDevice = Device.Open();

            if (KinectDevice is null) return false;

            // Try initializing the camera stream
            KinectDevice.StartCameras(new DeviceConfiguration
            {
                CameraFPS = FPS.FPS30,
                ColorResolution = ColorResolution.Off,
                DepthMode = DepthMode.NFOV_Unbinned,
                WiredSyncMode = WiredSyncMode.Standalone
            });

            // Initialize the tracking service
            BodyTracker = Tracker.Create(KinectDevice.GetCalibration(), new TrackerConfiguration
            {
                ProcessingMode = (TrackerProcessingMode)Host.PluginSettings.GetSetting("Tracking", 4),
                SensorOrientation = (SensorOrientation)Host.PluginSettings.GetSetting("Orientation", 0)
            });

            return false;
        }
        catch (Exception e)
        {
            Host.Log($"Failed to open the Kinect sensor! Message: {e.Message}");
            return false;
        }
    }
}