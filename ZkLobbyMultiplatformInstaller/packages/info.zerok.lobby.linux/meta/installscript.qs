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

    if (systemInfo.kernelType === "linux"){

        installer.componentByName("info.zerok.lobby.linux").setValue("Default", true);
        installer.componentByName("info.zerok.engine.linux").setValue("Default", true);

        installer.componentByName("info.zerok.lobby.seven").setValue("Virtual", true);
        installer.componentByName("info.zerok.lobby.xp").setValue("Virtual", true);
        installer.componentByName("info.zerok.engine.win").setValue("Virtual", true);
    }

}

Component.prototype.createOperations = function()
{
    // call default implementation to actually install README.txt!
    component.createOperations();

    if (installer.isInstaller()){

        if (systemInfo.productType === "ubuntu"){


            mono = installer.execute( "/bin/which", new Array( "mono" ) )[0];
            if (!mono) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "mono-complete", "-y");
                //QMessageBox["warning"]( "monoError", "No mono!", "You need mono. Please install mono-complete using the System Package Management tools." );
            }

            xprintidle = installer.execute( "/bin/which", new Array( "xprintidle" ) )[0];
            if (!xprintidle) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "xprintidle", "-y");
                //QMessageBox["warning"]( "xprintidleError", "No xprintidle!", "You need xprintidle. Please install xprintidle using the System Package Management tools." );
            }

            p7zip = installer.execute( "/bin/which", new Array( "p7zip" ) )[0];
            if (!p7zip) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "p7zip-full", "-y");
                //QMessageBox["warning"]( "p7zipError", "No p7zip!", "You need xprintidle. Please install p7zip using the System Package Management tools." );
            }

            wget = installer.execute( "/bin/which", new Array( "wget" ) )[0];
            if (!wget) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "wget", "-y");
                //QMessageBox["warning"]( "wgetError", "No wget!", "You need wget. Please install wget using the System Package Management tools." );
            }

            libsdl2 = installer.execute( "/bin/which", new Array( "sdl2-config" ) )[0];
            if (!libsdl2) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "libsdl-2-2.0-0", "-y");
                //QMessageBox["warning"]( "libsdl2Error", "No libsdl!", "You need libsdl2-2.0-0. Please install libsdl2-2.0-0 using the System Package Management tools." );
            }
        }
        else{
            QMessageBox["warning"]( "DistroError", "No supported!", "Your Distribution of GNU/Linux is not suported at the moment, not solving dependencies please report this name: " + systemInfo.productType );
        }
    }
}
