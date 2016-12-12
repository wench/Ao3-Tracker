param(
    [string]$src=(Get-Location),
    [string]$dest="..\Ao3TrackReader.UWP\Assets",
    [string]$suffix100 = ".scale-100",
    [string]$suffix200 = ".scale-200"
)

Push-Location $dest 

Get-ChildItem $src -File -Filter *.png | ForEach-Object {
    $filename = [System.IO.Path]::GetFileNameWithoutExtension($_.Name);
    $path100 = $filename + $suffix100 + $_.Extension;
    $path200 = $filename + $suffix200 + $_.Extension;
    $src100 = (Resolve-Path -Relative -Path ([System.IO.Path]::Combine($src,$_.Name)));
    $src200 = (Resolve-Path -Relative -Path ([System.IO.Path]::Combine($src,"200",$_.Name)));

    rm $path100 -ErrorAction SilentlyContinue
    cmd.exe /c mklink $path100 $src100
    rm $path200 -ErrorAction SilentlyContinue
    cmd.exe /c mklink $path200 $src200
}
Pop-Location