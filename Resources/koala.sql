-- phpMyAdmin SQL Dump
-- version 4.5.4.1deb2ubuntu2.1
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Jul 05, 2019 at 10:08 AM
-- Server version: 10.0.38-MariaDB-0ubuntu0.16.04.1
-- PHP Version: 7.0.33-0ubuntu0.16.04.5

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `koala`
--

-- --------------------------------------------------------

--
-- Table structure for table `k_cmdlog`
--

CREATE TABLE IF NOT EXISTS `k_cmdlog` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `guild` bigint(20) NOT NULL,
  `channel` bigint(20) NOT NULL,
  `message` bigint(20) NOT NULL,
  `user` bigint(20) NOT NULL,
  `name` text NOT NULL,
  `content` text NOT NULL,
  `attachments` int(11) NOT NULL DEFAULT '0',
  `failure` text,
  `date_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=90 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `k_modlog`
--

CREATE TABLE IF NOT EXISTS `k_modlog` (
  `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `guild` bigint(20) UNSIGNED NOT NULL,
  `subject` bigint(20) UNSIGNED NOT NULL,
  `moderator` bigint(20) UNSIGNED NOT NULL,
  `message` bigint(20) UNSIGNED NOT NULL,
  `action` text NOT NULL,
  `reason` text NOT NULL,
  `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=24 DEFAULT CHARSET=utf8mb4;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
