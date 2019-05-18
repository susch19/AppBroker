$KeyPress = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

dotnet restore -r  linux-arm
if($KeyPress = 'D')
{
    dotnet publish -r linux-arm -c Debug
}
else
{
    dotnet publish -r linux-arm -c Release
}