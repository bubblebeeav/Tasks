CREATE DATABASE filesdirs;
//�������� ����� �� "filesdirs" �� ��������� ������� MSSQLEXPRESS

CREATE TABLE files (id INT IDENTITY(1,1), filname VARCHAR(260), hashsum VARCHAR(32), result VARCHAR(50));
//�������� ����� ������� ������� "files" � �� "filesdirs" ..