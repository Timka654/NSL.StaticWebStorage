$ver = (Get-Date).ToString("yyyy.MM.dd.HHmm")
$buildPath = "build/Release"

if (Test-Path $buildPath ) {
	remove-item $buildPath -Recurse -Force
}

./BuildRelease $ver