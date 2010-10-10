<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="SubversionHowTo.aspx.cs" Inherits="ModelBase.SubversionHowTo" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
Subversion - or "SVN" is sort of "shared folder" that remembers all changes done to files and even all deleted files.

How to use it:
<ul>
<li>Download and install tortoise svn from <a href='http://tortoisesvn.net/downloads'>http://tortoisesvn.net/downloads</a></li>
<li>Create empty folder anywhere on your computer</li>
<li>Right click on it and pick "SVN checkout"</li>
<li>Enter "<asp:Label ID="lbSvn" runat="server">svn://springrts.com/modelbase/YourLogin</asp:Label>" as URL of repository and click ok</li>
<li>From now on you can work in this folder as normal - put your model sources there, each to its own sub-folder. Folder name will be used as model name by the system</li>
<li>When you want to upload data to the model base, simply right click your folder and select 
    &quot;SVN commit&quot;. In dialog, you can review changes you want to send to model base. 
    You will be asked for login name and password - use login name and password from 
    your model base account.</li>
<li>Later, you can update your model sources there and do commit again, to upload new model version - you can also easilly go back to older version using other subversion options.</li>
<li>Optionally you can add screenshots to your model (preferably to subfolder called "screenshots"). First screenshot will be auto-used as its icon, or you can upload specific file - called "icon.png" to model folder. In the same way you can upload custom license.txt</li>
</ul>


</asp:Content>
