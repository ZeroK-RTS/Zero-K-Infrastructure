# MySQL-Front 3.2  (Build 14.3)

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES latin1 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='SYSTEM' */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE */;
/*!40101 SET SQL_MODE='' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES */;
/*!40103 SET SQL_NOTES='ON' */;


# Host: vsync.de    Database: LuaManager
# ------------------------------------------------------
# Server version 5.0.51a-24+lenny2-log

DROP DATABASE IF EXISTS `LuaManager`;
CREATE DATABASE `LuaManager` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `LuaManager`;

#
# Table structure for table Categories
#

CREATE TABLE `Categories` (
  `Id` int(11) NOT NULL auto_increment,
  `Name` varchar(40) NOT NULL,
  `OwnerId` int(11) unsigned NOT NULL,
  PRIMARY KEY  (`Id`),
  UNIQUE KEY `NameKey` (`Name`)
) ENGINE=MyISAM AUTO_INCREMENT=13 DEFAULT CHARSET=latin1;


#
# Table structure for table CommentsWidget
#

CREATE TABLE `CommentsWidget` (
  `UserId` int(11) NOT NULL,
  `NameId` int(11) NOT NULL,
  `Comment` text NOT NULL,
  `Entry` timestamp NOT NULL default CURRENT_TIMESTAMP,
  PRIMARY KEY  (`UserId`,`NameId`,`Entry`),
  KEY `NameIdKey` (`NameId`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;


#
# Table structure for table Files
#

CREATE TABLE `Files` (
  `Id` int(11) NOT NULL auto_increment,
  `LuaId` int(11) NOT NULL default '0',
  `Url` varchar(1000) default NULL,
  `MD5` varchar(32) NOT NULL default '',
  `LocalPath` varchar(200) NOT NULL default '',
  PRIMARY KEY  (`Id`),
  UNIQUE KEY `IdPath` (`LuaId`,`LocalPath`)
) ENGINE=MyISAM AUTO_INCREMENT=1354 DEFAULT CHARSET=latin1;


#
# Table structure for table Images
#

CREATE TABLE `Images` (
  `Id` int(11) NOT NULL auto_increment,
  `NameId` int(11) NOT NULL default '0',
  `Url` varchar(1000) NOT NULL default '',
  PRIMARY KEY  (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=139 DEFAULT CHARSET=latin1;


#
# Table structure for table LUAs
#

CREATE TABLE `LUAs` (
  `Id` int(11) NOT NULL auto_increment,
  `Version` decimal(10,3) unsigned NOT NULL default '0.000',
  `NameId` int(11) NOT NULL default '0',
  `Entry` timestamp NOT NULL default CURRENT_TIMESTAMP,
  `Active` tinyint(3) NOT NULL default '0',
  `Changelog` text,
  PRIMARY KEY  (`Id`),
  UNIQUE KEY `NameVersion` (`Version`,`NameId`)
) ENGINE=MyISAM AUTO_INCREMENT=355 DEFAULT CHARSET=latin1;


#
# Table structure for table Log
#

CREATE TABLE `Log` (
  `Id` int(11) NOT NULL auto_increment,
  `Entry` timestamp NULL default CURRENT_TIMESTAMP,
  `Action` int(11) default NULL,
  PRIMARY KEY  (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=3499771 DEFAULT CHARSET=latin1;


#
# Table structure for table ModWidgets
#

CREATE TABLE `ModWidgets` (
  `Id` int(11) NOT NULL auto_increment,
  `HeaderName` varchar(100) default NULL COMMENT 'The widget''s name as it appears in widget''s header info',
  `ModId` int(11) NOT NULL,
  `Description` text,
  PRIMARY KEY  (`Id`),
  KEY `ModIdKey` (`ModId`)
) ENGINE=MyISAM AUTO_INCREMENT=28 DEFAULT CHARSET=latin1;


#
# Table structure for table Mods
#

CREATE TABLE `Mods` (
  `Id` int(11) NOT NULL auto_increment,
  `Abbreviation` varchar(40) NOT NULL,
  `OwnerId` int(11) NOT NULL,
  `OrderConfigFilename` varchar(200) NOT NULL,
  PRIMARY KEY  (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=8 DEFAULT CHARSET=latin1;


#
# Table structure for table Names
#

CREATE TABLE `Names` (
  `Id` int(11) NOT NULL auto_increment,
  `Name` varchar(100) NOT NULL default '',
  `Author` varchar(100) default NULL,
  `Description` text,
  `Mods` varchar(100) default 'All',
  `OwnerId` int(11) NOT NULL default '0',
  `DownloadCount` int(11) unsigned NOT NULL default '0',
  `Hidden` tinyint(1) unsigned NOT NULL default '0',
  `CategoryId` int(11) unsigned default NULL,
  PRIMARY KEY  (`Id`,`Name`(1)),
  UNIQUE KEY `Name` (`Name`)
) ENGINE=MyISAM AUTO_INCREMENT=159 DEFAULT CHARSET=latin1;


#
# Table structure for table ProfileActivates
#

CREATE TABLE `ProfileActivates` (
  `Id` int(11) NOT NULL auto_increment,
  `UserId` int(11) unsigned NOT NULL,
  `ModId` int(11) unsigned NOT NULL,
  `WidgetName` varchar(100) NOT NULL,
  PRIMARY KEY  (`Id`),
  UNIQUE KEY `NameUserMod` (`ModId`,`UserId`,`WidgetName`)
) ENGINE=MyISAM AUTO_INCREMENT=3563 DEFAULT CHARSET=latin1;


#
# Table structure for table ProfileInstalls
#

CREATE TABLE `ProfileInstalls` (
  `Id` int(11) NOT NULL auto_increment,
  `UserId` int(11) unsigned NOT NULL,
  `NameId` int(11) unsigned NOT NULL,
  PRIMARY KEY  (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=541 DEFAULT CHARSET=latin1;


#
# Table structure for table RatingsWidget
#

CREATE TABLE `RatingsWidget` (
  `NameId` int(11) NOT NULL,
  `Rating` float(2,1) unsigned NOT NULL,
  `UserId` int(11) NOT NULL,
  `Entry` timestamp NOT NULL default CURRENT_TIMESTAMP on update CURRENT_TIMESTAMP,
  PRIMARY KEY  (`NameId`,`UserId`),
  KEY `NameIdKey` (`NameId`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;


#
# Table structure for table SpringUsers
#

CREATE TABLE `SpringUsers` (
  `Id` int(11) NOT NULL auto_increment,
  `Username` varchar(200) NOT NULL,
  `Entry` timestamp NOT NULL default CURRENT_TIMESTAMP,
  PRIMARY KEY  (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=15 DEFAULT CHARSET=latin1;


#
# Table structure for table Users
#

CREATE TABLE `Users` (
  `Id` int(11) NOT NULL auto_increment,
  `Name` varchar(30) NOT NULL default '',
  `PasswdMd5` varchar(64) NOT NULL default '',
  `Admin` int(1) unsigned NOT NULL default '0',
  PRIMARY KEY  (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=14 DEFAULT CHARSET=latin1;


/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
