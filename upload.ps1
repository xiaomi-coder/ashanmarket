param (
    [string]$HostName = "5.182.26.100",
    [string]$UserName = "root",
    [string]$Password = "5PMzEWGQl6O&od!r",
    [string]$LocalFileZip = "e:\xiaomi\ashanmarket\saas-pos.zip",
    [string]$LocalFileDeploy = "C:\Users\iSSGamer\.gemini\antigravity\brain\3763f2e2-12e7-4d89-919a-3aa0865492a8\deploy.sh",
    [string]$RemotePath = "/root/"
)

# Use PSCredential
$secpasswd = ConvertTo-SecureString $Password -AsPlainText -Force
$mycreds = New-Object System.Management.Automation.PSCredential ($UserName, $secpasswd)

# Try to use .NET WebClient with FTP if SFTP is not available natively in powershell without modules
Write-Host "Please use WinSCP or FileZilla manually as native powershell requires external modules for SFTP."
