$pipe = 'np:\\.\\pipe\\LOCALDB#FDCB0F28\\tsql\\query'
$connString = "Data Source=$pipe;Initial Catalog=master;Integrated Security=True;" 

$connection = New-Object System.Data.SqlClient.SqlConnection $connString
try {
    $connection.Open()
    Write-Output "Connected to master"
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT name FROM sys.databases WHERE name = 'SengeleMinistries'"
    $reader = $cmd.ExecuteReader()
    $exists = $false
    while ($reader.Read()) { $exists = $true; Write-Output ("Found DB: " + $reader.GetString(0)) }
    $reader.Close()

    if ($exists) {
        Write-Output "Dropping database SengeleMinistries"
        $cmd.CommandText = "ALTER DATABASE [SengeleMinistries] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [SengeleMinistries];"
        $cmd.ExecuteNonQuery() | Out-Null
        Write-Output "Dropped"
    } else {
        Write-Output "Database not found"
    }
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    $connection.Close()
}
