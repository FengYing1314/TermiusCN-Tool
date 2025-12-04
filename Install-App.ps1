<#
.SYNOPSIS
    TermiusCN-Tool 自动安装脚本
#>

param(
    [switch]$SkipCertificate
)

$ErrorActionPreference = "Stop"

# ==========================================
# 1. 自动提权 (请求管理员权限)
# ==========================================
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$identity
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "正在请求管理员权限..." -ForegroundColor Yellow
    
    # 重建参数列表以传递给提权后的进程
    $arguments = $MyInvocation.BoundParameters.GetEnumerator() | ForEach-Object {
        if ($_.Value -is [switch]) {
            if ($_.Value) { "-$($_.Key)" }
        }
        else {
            "-$($_.Key) `"$($_.Value)`""
        }
    }
    $argList = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" $arguments"

    try {
        Start-Process powershell.exe -ArgumentList $argList -Verb RunAs
        exit
    }
    catch {
        Write-Host "无法自动提权，请手动右键以管理员身份运行。" -ForegroundColor Red
        Pause
        exit
    }
}

# ==========================================
# 2. 环境初始化
# ==========================================
$scriptDir = $PSScriptRoot
Set-Location $scriptDir

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "      TermiusCN-Tool 安装程序" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ==========================================
# 3. 查找文件
# ==========================================
# 3.1 查找证书
$cerFile = Get-ChildItem -Path $scriptDir -Filter "*.cer" | Select-Object -First 1

# 3.2 查找 MSIX (优先匹配当前系统架构)
$arch = $env:PROCESSOR_ARCHITECTURE
$msixFile = $null

if ($arch -match "ARM64") {
    $msixFile = Get-ChildItem -Path $scriptDir -Filter "*ARM64*.msix" | Select-Object -First 1
}
elseif ($arch -match "AMD64" -or $arch -match "IA64") {
    $msixFile = Get-ChildItem -Path $scriptDir -Filter "*x64*.msix" | Select-Object -First 1
}

# 兜底：如果没有匹配到特定架构，就找任意一个
if (-not $msixFile) {
    $msixFile = Get-ChildItem -Path $scriptDir -Filter "*.msix" | Select-Object -First 1
}

if (-not $msixFile) {
    Write-Host "错误: 未找到 .msix 安装包文件！" -ForegroundColor Red
    Pause
    exit 1
}

Write-Host "已选中安装包: $($msixFile.Name)" -ForegroundColor Green

# ==========================================
# 4. 选择操作
# ==========================================
Write-Host "请选择操作 / Select Action:" -ForegroundColor Cyan
Write-Host "  [1] 安装 (Install)" -ForegroundColor Green
Write-Host "  [2] 卸载 (Uninstall)" -ForegroundColor Red
Write-Host ""
$choice = Read-Host "请输入数字 (1/2)"

if ($choice -eq "2") {
    # ==========================================
    # 5. 卸载流程
    # ==========================================
    Write-Host ""
    Write-Host "正在执行卸载..." -ForegroundColor Yellow

    # 5.1 卸载应用
    Write-Host "正在查找并卸载应用..."
    try {
        $pkgs = Get-AppxPackage -AllUsers | Where-Object { $_.Name -like "*Termius*" }
        if ($pkgs) {
            foreach ($pkg in $pkgs) {
                Write-Host "发现应用: $($pkg.Name) ($($pkg.Version))"
                Remove-AppxPackage -Package $pkg.PackageFullName -AllUsers
                Write-Host "应用卸载成功。" -ForegroundColor Green
            }
        }
        else {
            Write-Host "未找到已安装的 Termius 相关应用。" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "应用卸载遇到错误: $_" -ForegroundColor Red
    }

    # 5.2 卸载证书
    if ($cerFile) {
        Write-Host "正在清理证书..."
        try {
            # 获取证书指纹
            $certObj = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cerFile.FullName)
            $thumbprint = $certObj.Thumbprint

            $locations = @("Cert:\LocalMachine\Root", "Cert:\LocalMachine\TrustedPeople")
            foreach ($loc in $locations) {
                $certPath = "$loc\$thumbprint"
                if (Test-Path $certPath) {
                    Remove-Item -Path $certPath -Force -ErrorAction SilentlyContinue
                    Write-Host "已从 $loc 移除证书。" -ForegroundColor Green
                }
            }
        }
        catch {
            Write-Host "证书清理遇到错误: $_" -ForegroundColor Red
        }
    }
    else {
        Write-Host "未找到 .cer 文件，无法精确清理证书 (跳过)。" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "       卸载完成! " -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
else {
    # ==========================================
    # 5. 安装流程 (默认)
    # ==========================================
    
    # 5.1 安装证书
    if (-not $SkipCertificate) {
        if ($cerFile) {
            Write-Host "正在安装证书..." -ForegroundColor Yellow
            try {
                # 关键：安装到 'Root' (受信任的根证书颁发机构)
                Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null
                
                # 额外：安装到 'TrustedPeople' (受信任人)，某些自签名应用需要
                Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
                
                Write-Host "证书安装成功！" -ForegroundColor Green
            }
            catch {
                Write-Host "证书安装失败: $_" -ForegroundColor Red
            }
        }
        else {
            Write-Host "警告: 未找到 .cer 证书文件，跳过证书安装。" -ForegroundColor Yellow
        }
    }

    # 5.2 安装应用
    Write-Host "正在安装应用..." -ForegroundColor Yellow

    try {
        # 使用 -ForceUpdateFromAnyVersion 允许覆盖安装/降级
        Add-AppxPackage -Path $msixFile.FullName -ForceUpdateFromAnyVersion -ForceApplicationShutdown
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "       安装成功! " -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
    }
    catch {
        Write-Host "安装失败: $_" -ForegroundColor Red
        Write-Host "常见原因：证书未安装，或系统版本过低。" -ForegroundColor Gray
    }
}

Write-Host "按任意键退出..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
