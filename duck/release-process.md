
## Context
I've decided not to automate the release process for now. It wouldn't save much on complexity, 
and the speedbump should help me be intentional about release notes, versioning, if I really want the binary 
synced with the nuget package, etc.


## Steps

1. Tag the repository
```powershell
git tag "0.1.0-alpha"
git push --tags
```
2. Package exes (be sure to set the version)
```powershell
dotnet publish -r win-x64   -p:PublishSingleFile=true --self-contained true /p:Version="0.1.0-alpha"
dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true /p:Version="0.1.0-alpha"
dotnet publish -r osx-x64   -p:PublishSingleFile=true --self-contained true /p:Version="0.1.0-alpha"
```
2. Copy them to a new release at https://github.com/farlee2121/Notedown/releases/new
   1. set the title and release notes

Done


## Future

I can publish as a .net tool just like I publish a nuget package. I just need to add
```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>notedown</ToolCommandName>
``` 

Then
```powershell
dotnet pack /p:Version="0.1.0-alpha"
```

I can also publish from the cli, though a manual upload gives me an opportunity to review the contents. 
```powershell
dotnet nuget push <path-to.nupkg> -k <api-key>
```

All of this can pretty easily translate to an automated process, either as a local script or github action