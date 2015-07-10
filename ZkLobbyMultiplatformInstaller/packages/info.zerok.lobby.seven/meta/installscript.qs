var Dir = new function () {
    this.toNativeSparator = function (path) {
        if (systemInfo.productType === "windows")
            return path.replace(/\//g, '\\');
        return path;
    }
};

function Component() {
    installer.installationFinished.connect(this, Component.prototype.installationFinished );
    component.loaded.connect(this, Component.prototype.loaded );
}

Component.prototype.loaded = function()
{

    if ((systemInfo.productType === "windows") && (systemInfo.prettyProductName === "Windows 7")){

        installer.componentByName("info.zerok.lobby.seven").setValue("Default", true);
        installer.componentByName("info.zerok.engine.win").setValue("Default", true);

        installer.componentByName("info.zerok.lobby.xp").setValue("Virtual", true);
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

            if (systemInfo.prettyProductName === "Windows 7") {

                dotNet451 = installer.execute("reg", new Array("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\|NET Framework Setup\\NDP\\v4\Full\\Install") );
                if ( !dotNet451)
                    //QMessageBox["warning"]( "netError", ".NET", ".NET 4.5.1 Not Found" );

                    component.addElevatedOperation("Execute",
                                            "{0,1602,5100}",
                                            "@TargetDir@\\dotnet\\NDP451-KB2859818-Web.exe",
                                            "/norestart");
                    component.addElevatedOperation("Delete", "@TargetDir@\\dotnet\\NDP451-KB2859818-Web.exe");
            }
            //component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@DesktopDir@\\Zero-K.lnk");
            //component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@AllUsersStartMenuProgramsPath@\\Zero-K.lnk");
        }
    }
}

Component.prototype.installationFinished = function() {
    text = "";
    if(installer.isInstaller()) {

        text += "Zero-K installation done.";

    } else if(installer.isUninstaller()) {
        text += "Zero-K uninstallation done.";
    }
    text += "Click Finish to exit the Setup Wizard.";
    installer.setValue("FinishedText", text);
}
