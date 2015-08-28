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

        var d = "Type=Application\nComment=Open source RTS game with physical projectiles, smart units and powerful UI, running on the Spring engine\nKeywords=zero-k;Zero-K;rts;spring;springrts;\nIcon=zero-k\nTerminal=false\nHomepage=https://zero-k.info\nCategories=Game;\n";
        var desktopIcon = "Name=Zero-K\nGenericName=Zero-K\nExec=mono @TargetDir@/Zero-K\n" + d;

        component.addOperation("CreateDesktopEntry", "Zero-K.desktop", desktopIcon);
        component.addOperation("InstallIcons", "@TargetDir@/share/icons");

        if (systemInfo.productType === "ubuntu" || systemInfo.productType === "debian"){

            mono = installer.execute( "/bin/which", new Array( "mono" ) )[0];
            if (!mono) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "mono-complete", "-y");
            }

            xprintidle = installer.execute( "/bin/which", new Array( "xprintidle" ) )[0];
            if (!xprintidle) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "xprintidle", "-y");
            }

            p7zip = installer.execute( "/bin/which", new Array( "p7zip" ) )[0];
            if (!p7zip) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "p7zip-full", "-y");
            }

            wget = installer.execute( "/bin/which", new Array( "wget" ) )[0];
            if (!wget) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "wget", "-y");
            }

            libsdl2 = installer.execute( "/bin/which", new Array( "sdl2-config" ) )[0];
            if (!libsdl2) {
                component.addElevatedOperation("Execute", "apt-get", "update");
                component.addElevatedOperation("Execute", "apt-get", "install", "libsdl-2-2.0-0", "-y");
            }
        }
        
        else if (systemInfo.productType === "opensuse" || systemInfo.productType === "suse"){

            mono = installer.execute( "/bin/which", new Array( "mono" ) )[0];
            if (!mono) {
                component.addElevatedOperation("Execute", "zypper", "refresh");
                component.addElevatedOperation("Execute", "zypper", "-n", "-q", "install", "mono-complete");
            }
            
            // xprintidle broken in opensuse :(
            /*
            xprintidle = installer.execute( "/bin/which", new Array( "xprintidle" ) )[0];
            if (!xprintidle) {
                component.addElevatedOperation("Execute", "zypper", "refresh");
                component.addElevatedOperation("Execute", "zypper", "-n", "-q", "install", "xprintidle");
            }
            */
            
            p7zip = installer.execute( "/bin/which", new Array( "p7zip" ) )[0];
            if (!p7zip) {
                component.addElevatedOperation("Execute", "zypper", "refresh");
                component.addElevatedOperation("Execute", "zypper", "-n", "-q", "install", "p7zip");
            }

            wget = installer.execute( "/bin/which", new Array( "wget" ) )[0];
            if (!wget) {
                component.addElevatedOperation("Execute", "zypper", "refresh");
                component.addElevatedOperation("Execute", "zypper", "-n", "-q", "install", "wget");
            }

            libsdl2 = installer.execute( "/bin/which", new Array( "sdl2-config" ) )[0];
            if (!libsdl2) {
                component.addElevatedOperation("Execute", "zypper", "refresh");
                component.addElevatedOperation("Execute", "zypper", "-n", "-q", "install", "libSDL2-2_0-0");
            }
        }
        
        else if (systemInfo.productType === "fedora" || systemInfo.productType === "redhat"){

            mono = installer.execute( "/bin/which", new Array( "mono" ) )[0];
            if (!mono) {
                component.addElevatedOperation("Execute", "dnf", "check-update");
                component.addElevatedOperation("Execute", "dnf", "-y", "install", "mono-core");
            }
            // xprintidle not available on fedora
            /*
            xprintidle = installer.execute( "/bin/which", new Array( "xprintidle" ) )[0];
            if (!xprintidle) {
                component.addElevatedOperation("Execute", "dnf", "check-update");
                component.addElevatedOperation("Execute", "dnf", "-y", "install", "xprintidle");
            }
            */
            p7zip = installer.execute( "/bin/which", new Array( "p7zip" ) )[0];
            if (!p7zip) {
                component.addElevatedOperation("Execute", "dnf", "check-update");
                component.addElevatedOperation("Execute", "dnf", "-y", "install", "p7zip");
            }

            wget = installer.execute( "/bin/which", new Array( "wget" ) )[0];
            if (!wget) {
                component.addElevatedOperation("Execute", "dnf", "check-update");
                component.addElevatedOperation("Execute", "dnf", "-y", "install", "wget");
            }

            libsdl2 = installer.execute( "/bin/which", new Array( "sdl2-config" ) )[0];
            if (!libsdl2) {
                component.addElevatedOperation("Execute", "dnf", "check-update");
                component.addElevatedOperation("Execute", "dnf", "-y", "install", "SDL2");
            }
        }
        
        else{
            QMessageBox["warning"]( "DistroError", "No supported!", "Your Distribution of GNU/Linux is not suported at the moment, not solving dependencies please report this name: " + systemInfo.productType );
        }
    }
}
