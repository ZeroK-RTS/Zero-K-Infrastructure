var Dir = new function () {
    this.toNativeSparator = function (path) {
        if (systemInfo.productType === "windows")
            return path.replace(/\//g, '\\');
        return path;
    }
};

function Component()
{
    // default constructor
}

Component.prototype.createOperations = function()
{
    // call default implementation to actually install README.txt!
    component.createOperations();

    if (installer.isInstaller()){
        if (systemInfo.productType === "windows"){

            if (systemInfo.prettyProductName === "Windows XP") {

                dotNet40 = installer.execute("reg", new Array("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\|NET Framework Setup\\NDP\\v4\Full\\Install") );
                if ( !dotNet40)
                    //QMessageBox["warning"]( "netError", ".NET", ".NET 4.0 Not Found" );

                    component.addElevatedOperation("Execute",
                                            "{0,1602,5100}",
                                            "@TargetDir@\\dotNetFx40_Full_setup.exe",
                                            "/norestart");
                    component.addElevatedOperation("Delete", "@TargetDir@\\dotNetFx40_Full_setup.exe");
            }
            component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@DesktopDir@\\Zero-K.lnk");
            component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@AllUsersStartMenuProgramsPath@\\Zero-K.lnk");

        }
    }
}
