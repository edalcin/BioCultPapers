# Remove only untracked build artifacts carefully
Write-Host 'Removendo bin/Release raiz se existir...'
if (Test-Path 'H:\git\BioCultPapers\bin') {
    Remove-Item -Path 'H:\git\BioCultPapers\bin' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Removido: bin raiz'
}

if (Test-Path 'H:\git\BioCultPapers\publish') {
    Remove-Item -Path 'H:\git\BioCultPapers\publish' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Removido: publish raiz'
}

if (Test-Path 'H:\git\BioCultPapers\artifacts') {
    Remove-Item -Path 'H:\git\BioCultPapers\artifacts' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Removido: artifacts'
}

if (Test-Path 'H:\git\BioCultPapers\test-portable') {
    Remove-Item -Path 'H:\git\BioCultPapers\test-portable' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Removido: test-portable'
}

Write-Host 'Removendo net8.0-windows (mantendo publish)...'
if (Test-Path 'H:\git\BioCultPapers\src\EtnoPapers.UI\bin\Release\net8.0-windows') {
    Remove-Item -Path 'H:\git\BioCultPapers\src\EtnoPapers.UI\bin\Release\net8.0-windows' -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Removido: net8.0-windows'
}

Write-Host ''
Write-Host 'Verificando publish que deve ser mantido:'
if (Test-Path 'H:\git\BioCultPapers\src\EtnoPapers.UI\bin\Release\publish') {
    Get-ChildItem -Path 'H:\git\BioCultPapers\src\EtnoPapers.UI\bin\Release\publish' | Select-Object Name
} else {
    Write-Host 'AVISO: publish nao existe!'
}

Write-Host 'Limpeza concluida com sucesso!'
