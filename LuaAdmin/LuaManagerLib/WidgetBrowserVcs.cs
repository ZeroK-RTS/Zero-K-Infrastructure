using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace LuaManagerLib
{
    public class WidgetBrowserVcs : LuaManagerLib.WidgetBrowserAdmin
    {
        /*
         * return a list of the files for that widget which are not 
         * present in the file list
         */
        public WidgetBrowserVcs(string serverUrl)
            : base(serverUrl)
        {
        }

        private ArrayList getMissingFilesForWidget(LinkedList<FileInfo> filesCurWidget, ArrayList files)
        {
            IEnumerator ienumf = filesCurWidget.GetEnumerator();

            ArrayList missingFiles = new ArrayList();
            bool found = false;
            while (ienumf.MoveNext())
            {
                FileInfo finfo = (FileInfo)ienumf.Current;

                found = false;
                IEnumerator ienuml = files.GetEnumerator();
                while (ienuml.MoveNext())
                {
                    string localPath = (string)ienuml.Current;
                    if (localPath == Utils.normalizePathname(finfo.localPath))
                    {
                        found = true;
                        break;
                    }
                }

                if ( !found )
                {
                    missingFiles.Add(finfo.localPath);
                }
            }

            return missingFiles;
        }

        /*
         * returns all widgets which contain one more of the files
         */
        private WidgetList getWidgetsThatContainFiles(WidgetList luas, ArrayList files)
        {
            WidgetList widgets = new WidgetList();

            IEnumerator ienum = luas.Values.GetEnumerator();
            while( ienum.MoveNext() )
            {
                //iterate widgets
                WidgetInfo curWidget = (WidgetInfo)ienum.Current;

                LinkedList<FileInfo> filesCurWidget = this.getFileListByLuaId(curWidget.id);
                ArrayList mis = this.getMissingFilesForWidget(filesCurWidget, files);

                if (mis.Count < filesCurWidget.Count )
                {
                    //at least one file is included, add the widget
                    widgets.Add(curWidget.id, curWidget);
                }
            }

            return widgets;
        }

        /*
         * Adds a new Lua version. It finds the widget that matches the files and adds missing files from older version
         */
        public void addLuaVersionByFiles(decimal version, string changelog, string systemPathOfLuaUi, ArrayList fileList)
        {
            fileList = Utils.normalizePathnames(fileList);
            systemPathOfLuaUi = Utils.normalizePathname(systemPathOfLuaUi);
            WidgetList luas = this.getOverviewListWithInactive(); 

            WidgetList luasToUpdate = this.getWidgetsThatContainFiles(luas, fileList);

            IEnumerator widgetEnum = luasToUpdate.Values.GetEnumerator();
            while (widgetEnum.MoveNext())
            {
                WidgetInfo curWidget = (WidgetInfo)widgetEnum.Current;
                int luaId = this.addLuaVersion(version, curWidget.nameId );

                this.updateLuaVersion(luaId, changelog, 1);

                LinkedList<FileInfo> filesCurWidget = this.getFileListByLuaId(curWidget.id);

                IEnumerator fEnum = filesCurWidget.GetEnumerator();
                while (fEnum.MoveNext())
                {
                    FileInfo finfo = (FileInfo)fEnum.Current;
                    string localGamePath = Utils.normalizePathname(finfo.localPath);
                    string sourceFilename = Utils.normalizePathname(systemPathOfLuaUi + finfo.localPath);
                    this.addLuaFile(localGamePath, sourceFilename, luaId);     
                }
            }            
        }

        /*
         * Adds a new Lua version. The widget name and all the complete file list is needed
         */
        public void addLuaVersionAndFiles(decimal version, string widgetName, string changelog, ArrayList fileList)
        {
            fileList = Utils.normalizePathnames(fileList);
            WidgetName wn = this.getNameByWidgetName(widgetName);

            int luaId = this.addLuaVersion(version, wn.Id);
            this.updateLuaVersion(luaId, changelog, 1);

            for (int i = 0; i < fileList.Count; i++)
            {
                string sourceFilename = Utils.normalizePathname( (string)fileList[i] );
                string localGamePath = sourceFilename.Substring(sourceFilename.IndexOf("/LuaUI/"));
                this.addLuaFile(sourceFilename, localGamePath, luaId);
            }
        }
    }
}
