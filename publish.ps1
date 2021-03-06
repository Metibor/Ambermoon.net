$ErrorActionPreference = 'Stop';

if ($isWindows) {
  Write-Host Publish Windows executables
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -r win-x86 --no-restore --nologo --no-self-contained
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -r win-x64 --no-restore --nologo --no-self-contained
  Write-Host Pack zips for Windows
  cmd /c copy /b "Ambermoon.net\bin\Any CPU\Release\netcoreapp3.1\win-x64\publish\Ambermoon.net.exe"+"versions.dat" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows.zip "Ambermoon.net\Ambermoon.net.exe"
  cmd /c copy /b "Ambermoon.net\bin\Any CPU\Release\netcoreapp3.1\win-x86\publish\Ambermoon.net.exe"+"versions.dat" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows32Bit.zip "Ambermoon.net\Ambermoon.net.exe"
  Write-Host Publish Windows standalone executables
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -r win-x86 --no-restore --nologo --self-contained
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -r win-x64 --no-restore --nologo --self-contained
  Write-Host Pack standalone zips for Windows
  cmd /c copy /b "Ambermoon.net\bin\Any CPU\Release\netcoreapp3.1\win-x64\publish\Ambermoon.net.exe"+"versions.dat" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows-Standalone.zip "Ambermoon.net\Ambermoon.net.exe"
  cmd /c copy /b "Ambermoon.net\bin\Any CPU\Release\netcoreapp3.1\win-x86\publish\Ambermoon.net.exe"+"versions.dat" "Ambermoon.net\Ambermoon.net.exe"
  7z a Ambermoon.net-Windows32Bit-Standalone.zip "Ambermoon.net\Ambermoon.net.exe"
} else {
  Write-Host Publish Linux executable
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -r linux-x64 --no-restore --no-self-contained
  dotnet publish -c Release "./Ambermoon.ConcatFiles/Ambermoon.ConcatFiles.csproj" -r linux-x64 --no-restore
  Write-Host Pack tar for Linux
  Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList '"./versions.dat"','"./Ambermoon.net/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.net"'
  7z a Ambermoon.net-Linux.tar "./Ambermoon.net/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.net"
  7z a Ambermoon.net-Linux.tar.gz Ambermoon.net-Linux.tar
  rm Ambermoon.net-Linux.tar
  Write-Host Publish Linux standalone executable
  dotnet publish -c Release "./Ambermoon.net/Ambermoon.net.csproj" -p:PublishSingleFile=true -r linux-x64 --no-restore --self-contained
  Write-Host Pack standalone tar for Linux
  Start-Process -FilePath "./Ambermoon.ConcatFiles/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.ConcatFiles" -Wait -WorkingDirectory . -ArgumentList '"./versions.dat"','"./Ambermoon.net/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.net"'
  7z a Ambermoon.net-Linux-Standalone.tar "./Ambermoon.net/bin/Any CPU/Release/netcoreapp3.1/linux-x64/publish/Ambermoon.net"
  7z a Ambermoon.net-Linux-Standalone.tar.gz Ambermoon.net-Linux-Standalone.tar
  rm Ambermoon.net-Linux-Standalone.tar
}