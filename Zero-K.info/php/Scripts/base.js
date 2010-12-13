function clearinput(a) { document.getElementById(a).value = ""; }

function download()
{
	var os = /win/gi
	if(os.test(navigator.platform))
	{
		window.location = "http://zero-k.info/lobby/setup.exe"
	}
	else
	{
		window.location = "download.php"
	}
}