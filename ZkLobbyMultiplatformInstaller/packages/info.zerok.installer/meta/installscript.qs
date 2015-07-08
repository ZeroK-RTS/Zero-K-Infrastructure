var ComponentSelectionPage = null;

var Dir = new function () {
    this.toNativeSparator = function (path) {
        if (systemInfo.productType === "windows")
            return path.replace(/\//g, '\\');
        return path;
    }
};

function Component() {

    component.loaded.connect(this, Component.prototype.installerLoaded);

    installer.installationFinished.connect(this, Component.prototype.installationFinished );

    ComponentSelectionPage = gui.pageById(QInstaller.ComponentSelection);
    installer.setDefaultPageVisible(QInstaller.ComponentSelection, false);



}

Component.prototype.isDefault = function() {
    // select the component by default
    return true;
}

Component.prototype.installerLoaded = function()
{
    if (installer.isInstaller()){
        if (systemInfo.productType === "windows"){

            if (systemInfo.prettyProductName === "Windows XP") {

                installer.componentByName("info.zerok.installer").setValue("Default", "false");
                installer.componentByName("info.zerok.installerxp").setValue("Default", "true");
                installer.componentByName("info.zerok.installerlinux").setValue("Default", "false");

            } else if (systemInfo.prettyProductName === "Windows 7") {

                installer.componentByName("info.zerok.installer").setValue("Default", "true");
                installer.componentByName("info.zerok.installerxp").setValue("Default", "false");
                installer.componentByName("info.zerok.installerlinux").setValue("Default", "false");

            }
        } else if(systemInfo.productType === "ubuntu"){

            installer.componentByName("info.zerok.installer").setValue("Default", "false");
            installer.componentByName("info.zerok.installerxp").setValue("Default", "false");
            installer.componentByName("info.zerok.installerlinux").setValue("Default", "true");
        }
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
                    component.addElevatedOperation("Delete", "@TargetDir@\\NDP451-KB2859818-Web.exe");
            }
            component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@DesktopDir@\\Zero-K.lnk");
            component.addOperation("CreateShortcut", "@TargetDir@\\Zero-k.exe", "@AllUsersStartMenuProgramsPath@\\Zero-K.lnk");
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
