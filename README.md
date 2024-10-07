<div align="Center">
    <img
        src="https://github.com/FatRainbowPony/Dock-Window/blob/main/img/DockWindow.svg"
        alt="DockWindow" 
        width="200" 
        height="170">
</div>

# Dock Window
[![Nuget](https://img.shields.io/nuget/v/DockWindow)](https://www.nuget.org/packages/DockWindow)
[![Downloads](https://img.shields.io/nuget/dt/DockWindow)](https://www.nuget.org/packages/DockWindow) 

Dock window implementation of WPF base off [Using Application Desktop Toolbars](https://learn.microsoft.com/en-us/windows/win32/shell/application-desktop-toolbars)

 ## Features
- Docking to any edge and monitor 
- Supporting automatic hiding
- Cooperating with other desktop toolbars

## Usage
Install the NuGet package. Then create a WPF window and select the `DockWindow` class instead of the `Window` class. Needs to done  both in XAML and in code

### In XAML
```xml
<dw:DockWindow 
    x:Class="DockWindowDemo.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:dw="clr-namespace:DockWindow.Windows;assembly=DockWindow"
    mc:Ignorable="d"
    Title="MainWindow" 
    Background="Transparent"
    BorderBrush="Transparent"
    BorderThickness="0"
    AllowsTransparency="True"
    DockWidthOrHeight="150"
    AnimationBackground="White">
    <Grid>
    </Grid>
</dw:DockWindow>
```

### In code
```csharp
namespace DockWindowDemo
{
    public partial class MainWindow : DockWindow.Windows.DockWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

You can see [DockWindowDemo](https://github.com/FatRainbowPony/Dock-Window/tree/main/src/DockWindowDemo) for an example project

<img src="https://github.com/FatRainbowPony/Dock-Window/blob/main/img/DockWindowDemo.gif" width="600" height="338"/>