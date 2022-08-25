# DROP TABLE IF EXISTS atc_articles;﻿
CREATE TABLE atc_articles (
	id int unsigned auto_increment,
	websiteid int unsigned NOT NULL,
	url varchar(512) NOT NULL, -- Store URL in ASCII as Unique doesn't accept text longer than 255 chars, so make sure the URL is encoded properly.
	actualurl varchar(512) NULL,
	title varchar(255) NULL,
	content longtext NULL,
	contenthtml longtext NULL,
	published datetime NULL,
	timestamp datetime NOT NULL,
	author varchar(64) NULL,
	PRIMARY KEY (`id`),
	UNIQUE KEY `url_UNIQUE` (`url`)
);

# DROP TABLE IF EXISTS atc_crawllogs;
CREATE TABLE atc_crawllogs (
	id int unsigned auto_increment,
	websiteid int unsigned NOT NULL,
	crawlid int unsigned NOT NULL,
	lasthandled varchar(512) NULL,
	success int unsigned NOT NULL,
	fail int unsigned NOT NULL,
	status varchar(20) NOT NULL,
	notes varchar(512) NULL,
	crawled datetime NULL,
	PRIMARY KEY (`id`)
);

# DROP TABLE IF EXISTS atc_crawls;
CREATE TABLE atc_crawls (
	id int unsigned auto_increment,
	success int unsigned NOT NULL,
	fail int unsigned NOT NULL,
	notes varchar(512) NULL,
	status varchar(20) NOT NULL,
	started datetime NOT NULL,
	completed datetime NULL,
	PRIMARY KEY (`id`)
);

# DROP TABLE IF EXISTS atc_websiterules;
CREATE TABLE atc_websiterules (
	ruleid varchar(36) NOT NULL,
	type varchar(20) NOT NULL,
	websiteid int unsigned NOT NULL,
	pg_loadoption varchar(20) NOT NULL,
	pg_exp_urlrevise varchar(255) NULL,
	pg_exp_urlreplacement varchar(255) NULL,
	cnt_matchtype varchar(20) NOT NULL,
	cnt_exp_root varchar(512) NULL,
	cnt_exp_url varchar(512) NULL,
	cnt_exp_urlrevise varchar(255) NULL,
	cnt_exp_urlreplacement varchar(255) NULL,
	cnt_exp_title varchar(512) NULL,
	cnt_exp_date varchar(512) NULL,
	cnt_exp_content varchar(512) NULL,
	cnt_exp_author varchar(512) NULL,
	PRIMARY KEY (`ruleid`)
);

# DROP TABLE IF EXISTS atc_websites;
CREATE TABLE atc_websites (
	id int unsigned auto_increment,
	`rank` int unsigned NULL,
	name varchar(128) NOT NULL,
	home varchar(255) NOT NULL,
	urlformat varchar(255) NULL,
	startindex tinyint unsigned NULL,
#	listpath varchar(512) NULL,
	validatedate bool NOT NULL,
	notes varchar(512) NULL,
	registered datetime NOT NULL,
	enabled bool NOT NULL,
	status varchar(255) NULL,
	sysnotes varchar(512) NULL,
#	listmatchtype varchar(255) NOT NULL,
#	dataurl varchar(512) NULL,
	PRIMARY KEY (`id`)
);
