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

Component.prototype.installerLoaded = function()
{
    if (installer.isInstaller()){
        QMessageBox["warning"]( "sssError", "sss", "ssss" );
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
        } else */
        if (systemInfo.productType === "ubuntu"){

            installer.componentByName("info.zerok.installer").setValue("Default", "false");
            installer.componentByName("info.zerok.installerxp").setValue("Default", "false");
            installer.componentByName("info.zerok.installerlinux").setValue("Default", "true");
        }
    }
}
