# Garante que o servico do MySQL (instalacao nativa no Windows) esta de pe.
# Uso: .\scripts\verificar-mysql.ps1 [-Servico MySQL80]
param([string]$Servico = "MySQL80")


# MySQL roda nativo no Windows (Servico $Servico), nao em Docker.
# O Windows ja garante que ele sobe sozinho no boot (Startup Type: Automatic).
Write-Host "Verificando o servico do MySQL..." -ForegroundColor Cyan
$mysqlService = Get-Service -Name $Servico -ErrorAction SilentlyContinue
if ($null -eq $mysqlService) {
    Write-Host "Servico $Servico nao encontrado. Verifique o nome do servico com 'Get-Service *mysql*'." -ForegroundColor Red
    exit 1
}
if ($mysqlService.Status -ne "Running") {
    Write-Host "Iniciando o servico MySQL..." -ForegroundColor Yellow
    Start-Service -Name $Servico
}

