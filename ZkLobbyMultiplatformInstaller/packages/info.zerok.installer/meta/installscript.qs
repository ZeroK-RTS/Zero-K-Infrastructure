/**************************************************************************
**
** Copyright (C) 2015 The Qt Company Ltd.
** Contact: http://www.qt.io/licensing/
**
** This file is part of the Qt Installer Framework.
**
** $QT_BEGIN_LICENSE:LGPL$
** Commercial License Usage
** Licensees holding valid commercial Qt licenses may use this file in
** accordance with the commercial license agreement provided with the
** Software or, alternatively, in accordance with the terms contained in
** a written agreement between you and The Qt Company. For licensing terms
** and conditions see http://qt.io/terms-conditions. For further
** information use the contact form at http://www.qt.io/contact-us.
**
** GNU Lesser General Public License Usage
** Alternatively, this file may be used under the terms of the GNU Lesser
** General Public License version 2.1 or version 3 as published by the Free
** Software Foundation and appearing in the file LICENSE.LGPLv21 and
** LICENSE.LGPLv3 included in the packaging of this file. Please review the
** following information to ensure the GNU Lesser General Public License
** requirements will be met: https://www.gnu.org/licenses/lgpl.html and
** http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html.
**
** As a special exception, The Qt Company gives you certain additional
** rights. These rights are described in The Qt Company LGPL Exception
** version 1.1, included in the file LGPL_EXCEPTION.txt in this package.
**
**
** $QT_END_LICENSE$
**
**************************************************************************/

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
