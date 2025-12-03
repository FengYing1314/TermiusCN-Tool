#Requires -RunAsAdministrator
<#
.SYNOPSIS
    TermiusCN-Tool 安装脚本
.DESCRIPTION
    此脚本会自动安装证书并安装应用程序
.NOTES
    请以管理员身份运行此脚本
#>

param(
    [switch]$SkipCertificate
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TermiusCN-Tool 安装程序" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 获取脚本所在目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# 查找证书文件
$cerFile = Get-ChildItem -Path $scriptDir -Filter "*.cer" | Select-Object -First 1
# 查找 MSIX 文件
$msixFile = Get-ChildItem -Path $scriptDir -Filter "*.msix" | Select-Object -First 1

if (-not $msixFile) {
    Write-Host "错误: 未找到 .msix 安装包文件" -ForegroundColor Red
    Write-Host "请确保 .msix 文件与此脚本在同一目录" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "找到安装包: $($msixFile.Name)" -ForegroundColor Green

# 安装证书
if (-not $SkipCertificate) {
    if ($cerFile) {
        Write-Host ""
        Write-Host "正在安装证书..." -ForegroundColor Yellow
        try {
            Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
            Write-Host "证书安装成功!" -ForegroundColor Green
        }
        catch {
            Write-Host "证书安装失败: $_" -ForegroundColor Red
            Write-Host "尝试继续安装应用..." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "未找到证书文件 (.cer)，跳过证书安装" -ForegroundColor Yellow
        Write-Host "如果安装失败，请先开启 Windows 开发者模式" -ForegroundColor Yellow
    }
}

# 安装应用
Write-Host ""
Write-Host "正在安装应用..." -ForegroundColor Yellow
try {
    Add-AppxPackage -Path $msixFile.FullName
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  安装成功!" -ForegroundColor Green
    Write-Host "  请在开始菜单中查找 TermiusCN-Tool" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "安装失败: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "解决方法:" -ForegroundColor Yellow
    Write-Host "1. 确保以管理员身份运行此脚本" -ForegroundColor White
    Write-Host "2. 或者开启 Windows 开发者模式:" -ForegroundColor White
    Write-Host "   设置 -> 系统 -> 开发者选项 -> 开启开发人员模式" -ForegroundColor Gray
    Write-Host "3. 然后重新运行此脚本" -ForegroundColor White
}

Write-Host ""
pause
