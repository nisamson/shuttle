param([int]$MinTpe = 250, [string]$database = "ShuttleDev")

#foreach ($kind in ('C', "LW,RW", "D", "F", "F,D", "G")) {
foreach ($kind in ("G")) {
    $label = $kind
    if ($kind -eq "C") {
        $label = "centers"
    } elseif ($kind -eq "LW,RW") {
        $label = "wings"
    } elseif ($kind -eq "D") {
        $label = "defensemen"
    } elseif ($kind -eq "F") {
        $label = "forwards"
    } elseif ($kind -eq "G") {
        $label = "goalies"
    } else {
        $label = "skaters"
    }
    
    $tmpFile = New-TemporaryFile
    $skaterDir = Join-Path $PSScriptRoot "centroids-$label"
    $skaterFile = "$skaterDir/skaters.csv"
    New-Item -Path $skaterDir -ItemType Directory
    dotnet run --project $PSScriptRoot/../Shuttle.Analysis -- download-player-information --database $database -n L1 -p $kind -f csv -o $tmpFile
    Import-Csv -Path $tmpFile | Where-Object { $_.TotalTpe -ge $MinTpe } | Export-Csv -Path $skaterFile -NoTypeInformation
    dotnet run --project $PSScriptRoot/../Shuttle.Analysis -- analyze -i $skaterFile --flow kmeans-centroids --arg k=2 -o $skaterDir
    foreach ($file in Get-ChildItem -Path $skaterDir -Filter "*.csv") {
        (Get-Content -Path ($file.FullName)).Replace("goalieAttributes.", "").Replace("skaterAttributes.", "") | Set-Content -Path $file.FullName
    }
}

foreach ($file in Get-ChildItem -Path "split-5" -Filter "*.csv") {
    (Get-Content -Path ($file.FullName)).Replace("skaterAttributes.", "") | Set-Content -Path $file.FullName
}