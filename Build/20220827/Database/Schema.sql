-- MySQL dump 10.13  Distrib 8.0.30, for Win64 (x86_64)
--
-- Host: localhost    Database: jtyd
-- ------------------------------------------------------
-- Server version	8.0.30

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `atc_articles`
--

DROP TABLE IF EXISTS `atc_articles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `atc_articles` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `websiteid` int unsigned NOT NULL,
  `url` varchar(512) NOT NULL,
  `actualurl` varchar(512) DEFAULT NULL,
  `title` varchar(255) DEFAULT NULL,
  `content` longtext,
  `contenthtml` longtext,
  `published` datetime DEFAULT NULL,
  `timestamp` datetime NOT NULL,
  `author` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `url_UNIQUE` (`url`)
) ENGINE=InnoDB AUTO_INCREMENT=108089 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `atc_crawllogs`
--

DROP TABLE IF EXISTS `atc_crawllogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `atc_crawllogs` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `websiteid` int unsigned NOT NULL,
  `crawlid` int unsigned NOT NULL,
  `lasthandled` varchar(512) DEFAULT NULL,
  `success` int unsigned NOT NULL,
  `fail` int unsigned NOT NULL,
  `status` varchar(20) NOT NULL,
  `notes` varchar(512) DEFAULT NULL,
  `crawled` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=81901 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `atc_crawls`
--

DROP TABLE IF EXISTS `atc_crawls`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `atc_crawls` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `success` int unsigned NOT NULL,
  `fail` int unsigned NOT NULL,
  `notes` varchar(512) DEFAULT NULL,
  `status` varchar(20) NOT NULL,
  `started` datetime NOT NULL,
  `completed` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `atc_websiterules`
--

DROP TABLE IF EXISTS `atc_websiterules`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `atc_websiterules` (
  `ruleid` varchar(36) NOT NULL,
  `type` varchar(20) NOT NULL,
  `websiteid` int unsigned NOT NULL,
  `pg_loadoption` varchar(20) NOT NULL,
  `pg_exp_urlrevise` varchar(255) DEFAULT NULL,
  `pg_exp_urlreplacement` varchar(255) DEFAULT NULL,
  `cnt_matchtype` varchar(20) NOT NULL,
  `cnt_exp_root` varchar(512) DEFAULT NULL,
  `cnt_exp_url` varchar(512) DEFAULT NULL,
  `cnt_exp_urlrevise` varchar(255) DEFAULT NULL,
  `cnt_exp_urlreplacement` varchar(255) DEFAULT NULL,
  `cnt_exp_title` varchar(512) DEFAULT NULL,
  `cnt_exp_date` varchar(512) DEFAULT NULL,
  `cnt_exp_content` varchar(512) DEFAULT NULL,
  `cnt_exp_author` varchar(512) DEFAULT NULL,
  PRIMARY KEY (`ruleid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `atc_websites`
--

DROP TABLE IF EXISTS `atc_websites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `atc_websites` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `rank` int unsigned DEFAULT NULL,
  `name` varchar(128) NOT NULL,
  `home` varchar(255) NOT NULL,
  `urlformat` varchar(255) DEFAULT NULL,
  `startindex` tinyint unsigned DEFAULT NULL,
  `validatedate` tinyint(1) NOT NULL,
  `notes` varchar(512) DEFAULT NULL,
  `registered` datetime NOT NULL,
  `enabled` tinyint(1) NOT NULL,
  `status` varchar(255) DEFAULT NULL,
  `sysnotes` varchar(512) DEFAULT NULL,
  `brokensince` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=8577 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-08-28 23:45:49
