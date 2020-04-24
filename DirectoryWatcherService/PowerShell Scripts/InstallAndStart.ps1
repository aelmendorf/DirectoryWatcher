param([switch]$Elevated)
function Check-Admin {
    $currentUser = New-Object Security.Principal.WindowsPrincipal $([Security.Principal.WindowsIdentity]::GetCurrent())
    $currentUser.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
}
if ((Check-Admin) -eq $false)  {
    if ($elevated)
    {
    # could not elevate, quit
    }
    else {
        Start-Process powershell.exe -Verb RunAs -ArgumentList ('-noprofile -noexit -file "{0}" -elevated' -f ($myinvocation.MyCommand.Definition))
    }
    exit
}
$serviceName="DirectoryWatcher"
if(Get-Service $serviceName -ErrorAction SilentlyContinue){
    $serviceToRemove=Get-WmiObject -Class Win32_Service -Filter "name='$serviceName'"
    $serviceToRemove.delete()
}else{
    "Service does not exist"
}

#$secPassword=ConvertTo-SecureString "Drizzle123!" -AsPlainText -Force
#$myCredentials=New-Object System.Management.Automation.PSCredential(
$myCredentials=Get-Credential
#"C:\DirectoryWatcherSoftware\DirectoryWatcherService.exe -k netsvcs"
$binaryPath="C:\DirectoryWatcherSoftware\DirectoryWatcherService.exe"
New-Service -Name $serviceName -BinaryPathName $binaryPath -DisplayName $serviceName -StartupType Automatic -Credential $myCredentials
Start-Service -Name "DirectoryWatcher"