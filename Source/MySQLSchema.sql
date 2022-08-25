# DROP TABLE IF EXISTS atc_articles;﻿
CREATE TABLE atc_articles (
	id INT AUTO_INCREMENT,
	websiteid int4 NOT NULL,
	url varchar(512) NOT NULL, -- Store URL in ASCII as Unique doesn't accept text longer than 255 chars, so make sure the URL is encoded properly.
	actualurl varchar(512) NULL,
	title varchar(255) NULL,
	content text NULL,
	contenthtml text NULL,
	published DATETIME NULL,
	timestamp DATETIME NOT NULL,
	author varchar(50) NULL,
	CONSTRAINT atc_articles_pkey PRIMARY KEY (id),
    UNIQUE INDEX `url_UNIQUE` (`url` ASC) VISIBLE
);

# DROP TABLE IF EXISTS atc_crawllogs;
CREATE TABLE atc_crawllogs (
	id INT AUTO_INCREMENT,
	websiteid int4 NOT NULL,
	crawlid int4 NOT NULL,
	lasthandled varchar(512) NULL,
	success int4 NOT NULL,
	fail int4 NOT NULL,
	status varchar(255) NOT NULL,
	notes varchar(512) NULL,
	crawled DATETIME NULL,
	CONSTRAINT atc_crawllogs_pkey PRIMARY KEY (id)
);

# DROP TABLE IF EXISTS atc_crawls;
CREATE TABLE atc_crawls (
	id INT AUTO_INCREMENT,
	success int4 NOT NULL,
	fail int4 NOT NULL,
	notes varchar(512) NULL,
	status varchar(255) NOT NULL,
	started DATETIME NOT NULL,
	completed DATETIME NULL,
	CONSTRAINT atc_crawls_pkey PRIMARY KEY (id)
);

# DROP TABLE IF EXISTS atc_websiterules;
CREATE TABLE atc_websiterules (
	ruleid VARCHAR(36) NOT NULL,
	type varchar(20) NOT NULL,
	websiteid int4 NOT NULL,
	pg_loadoption varchar(20) NOT NULL,
	pg_exp_urlrevise varchar(200) NULL,
	pg_exp_urlreplacement varchar(200) NULL,
	cnt_matchtype varchar(20) NOT NULL,
	cnt_exp_root varchar(500) NULL,
	cnt_exp_url varchar(500) NULL,
	cnt_exp_urlrevise varchar(200) NULL,
	cnt_exp_urlreplacement varchar(200) NULL,
	cnt_exp_title varchar(500) NULL,
	cnt_exp_date varchar(500) NULL,
	cnt_exp_content varchar(500) NULL,
	cnt_exp_author varchar(500) NULL,
	CONSTRAINT atc_websiterules_pkey PRIMARY KEY (ruleid)
);

# DROP TABLE IF EXISTS atc_websites;
CREATE TABLE atc_websites (
	id INT AUTO_INCREMENT,
	`rank` int4 NULL,
	name varchar(100) NOT NULL,
	home varchar(255) NOT NULL,
	urlformat varchar(255) NULL,
	startindex int2 NULL,
#	listpath varchar(512) NULL,
	validatedate bool NOT NULL,
	notes varchar(512) NULL,
	registered DATETIME NOT NULL,
	enabled bool NOT NULL,
	status varchar(255) NULL,
	sysnotes varchar(512) NULL,
#	listmatchtype varchar(255) NOT NULL,
#	dataurl varchar(500) NULL,
	CONSTRAINT atc_websites_pkey PRIMARY KEY (id)
);
