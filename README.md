# winhwmetrics

winhwmetrics is a C# windows program for aggregating windows metrics and sensor readings through an RESTful API.
Currently serves data from HWiNFO and WMI.

## Running from source code
Execute the following command:
```bash
dotnet run
```

Server will be available at http://localhost:60000.

## Releasing
Run the following command:
```bash
dotnet publish -r win10-x64 -c Release /p:PublishSingleFile=true --self-contained
```

The server can be started by executing the generated .exe file found here: `<project_path>/bin/Release/net6.0/win10-x64/publish/winhwmetrics.exe`
