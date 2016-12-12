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

    New-Item -ItemType SymbolicLink -Path $path100 -Value (Resolve-Path -Relative -Path ([System.IO.Path]::Combine($src,$_.Name)))
    New-Item -ItemType SymbolicLink -Path $path200 -Value (Resolve-Path -Relative -Path ([System.IO.Path]::Combine($src,"200",$_.Name)))
}
Pop-Location