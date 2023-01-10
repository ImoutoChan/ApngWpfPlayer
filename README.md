# APNG Player Library

[![NuGet](https://img.shields.io/nuget/v/ImoutoRebirth.ApngWpfPlayer.svg?style=flat-square)](https://www.nuget.org/packages/ImoutoRebirth.ApngWpfPlayer)

WPF User Control that can show [APNG/PNG](https://wiki.mozilla.org/APNG_Specification) animated files and play them

## Usage

```xaml
xmlns:apngPlayer="clr-namespace:ImoutoRebirth.Navigator.ApngWpfPlayer.ApngPlayer;assembly=ImoutoRebirth.Navigator.ApngWpfPlayer"
...
<apngPlayer:ApngPlayer Source="{Binding Path}" />
```

## Installation

Install as [NuGet package](https://www.nuget.org/packages/ImoutoRebirth.ApngWpfPlayer):

```powershell
Install-Package ImoutoRebirth.ApngWpfPlayer
```
or 
```xml
<PackageReference Include="ImoutoRebirth.ApngWpfPlayer" Version="1.1.1" />
```
