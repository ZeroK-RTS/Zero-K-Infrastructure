var Dir = new function () {
    this.toNativeSparator = function (path) {
        if (systemInfo.productType === "windows")
            return path.replace(/\//g, '\\');
        return path;
    }
};

function Component() {
    component.loaded.connect(this, Component.prototype.loaded );
}

Component.prototype.loaded = function()
{

    if ((systemInfo.productType === "windows") && (systemInfo.prettyProductName === "Windows XP")){

        installer.componentByName("info.zerok.lobby.xp").setValue("Default", true);
        installer.componentByName("info.zerok.engine.win").setValue("Default", true);

        installer.componentByName("info.zerok.lobby.seven").setValue("Virtual", true);
        installer.componentByName("info.zerok.lobby.linux").setValue("Virtual", true);
        installer.componentByName("info.zerok.engine.linux").setValue("Virtual", true);
    }

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
                    component.addElevatedOperation("Delete", "@TargetDir@\\dotnet\\dotNetFx40_Full_setup.exe");
            }
            //component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@DesktopDir@\\Zero-K.lnk");
            //component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@AllUsersStartMenuProgramsPath@\\Zero-K.lnk");

        }
    }
}
