$revision = git rev-parse HEAD;
$tag = git tag -l --points-at HEAD;

$cscode = @"
namespace Ao3TrackReader.Version
{
    static partial class Version
    {
        public const string GitRevision = "$revision";
        public const string GitTag = "$tag";
    }
}
"@
$existing = Get-Content -Path "$PSScriptRoot\GitRevision.cs"
if ($existing -cne $cscode) {
	Set-Content -Path "$PSScriptRoot\GitRevision.cs" -Value $cscode
}
