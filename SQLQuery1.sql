CREATE DATABASE filesdirs;
//Создание новой БД "filesdirs" на локальном сервере MSSQLEXPRESS

CREATE TABLE files (id INT IDENTITY(1,1), filname VARCHAR(260), hashsum VARCHAR(32), result VARCHAR(50));
//Создание новой рабочей таблицы "files" в БД "filesdirs" ..