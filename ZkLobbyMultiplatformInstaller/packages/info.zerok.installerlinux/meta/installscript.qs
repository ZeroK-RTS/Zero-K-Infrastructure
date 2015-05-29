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

function Component()
{
    // default constructor
}

Component.prototype.createOperations = function()
{
    // call default implementation to actually install README.txt!
    component.createOperations();

    if (installer.isInstaller()){
        
        if (systemInfo.productType === "ubuntu"){
            
            //var result = QMessageBox.question("quit.question", "Installer", "Ubuntu",
            //                          QMessageBox.Yes | QMessageBox.No);
            
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
                component.addElevatedOperation("Execute", "apt-get", "install", "p7zip", "-y");
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
    }
}
