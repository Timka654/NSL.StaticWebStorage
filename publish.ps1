$currentLocation=Get-Location

# dotnet Project file path
$clientSolutionPath=($currentLocation).Path + "\NSL.StaticWebStorage\NSL.StaticWebStorage.csproj"
# dotnet Publish profile file path
$clientPublishProfileName=($currentLocation).Path + "\NSL.StaticWebStorage\Properties\PublishProfiles\Release.pubxml"

#remote ipaddr/port for connect to publisher.server project
$publisher_ip = "212.47.65.86"
$publisher_port = 6583

#cipher key for initialize connection to publisher.server project
$publisher_out_cipher_key = "!{b1HX11R**"
$publisher_in_cipher_key = "!{b1HX11R**"

#path to file with user information for authorize to deployed project on publisher.server, this file can located in "key_storage" with publisher.client executable folder, or relative/absolute path to current directory
$publisher_auth_key_file = "my_contabo_49e83e8e-282d-49ac-b7c4-60197090ee18.pubuk"

#dotnet publish output folder path
$publisher_release_dir = "publish"
#project id for identity on publisher.server
$publisher_project_id = "3435a0d1-45ef-4f0c-bc2b-f332fd505576"

# dotnet project build
dotnet publish $clientSolutionPath -c Release -o "$publisher_release_dir" /p:PublishProfile="$clientPublishProfileName"

# Evaluate success/failure
if($LASTEXITCODE -eq 0)
{
	Write-Host "build success" -ForegroundColor Green
		
	deployclient publish /project_id:$publisher_project_id /directory:$publisher_release_dir /auth_key_path:$publisher_auth_key_file /ip:$publisher_ip /port:$publisher_port /cipher_out_key:$publisher_out_cipher_key /cipher_in_key:$publisher_in_cipher_key /has_compression:true
		
	Write-Host "Finished" -ForegroundColor Green
}
else
{
	Write-Host "build failed" -ForegroundColor Red
	[System.Console]::ReadKey()
}