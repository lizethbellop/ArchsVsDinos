CREATE DATABASE ArchsVsDinos;
GO

USE ArchsVsDinos;
GO

CREATE TABLE Music (
	idMusic INT PRIMARY KEY IDENTITY (1,1),
	songName VARCHAR(45) NOT NULL,
	filePath VARCHAR(150) NOT NULL
);
GO

CREATE TABLE CardCharacter (
	idCardCharacter INT PRIMARY KEY IDENTITY (1,1),
	name VARCHAR(45) NOT NULL,
	type VARCHAR(20) NOT NULL,
	armyType VARCHAR(20) NOT NULL,
	power INT,
	imagePath VARCHAR(150) NOT NULL
);
GO

CREATE TABLE CardBody (
	idCardBody INT PRIMARY KEY IDENTITY (1,1),
	name VARCHAR(45) NOT NULL,
	power INT,
	imagePath VARCHAR(150) NOT NULL
);
GO

CREATE TABLE Configuration (
	idConfiguration INT PRIMARY KEY IDENTITY (1,1),
	musicVolume INT NOT NULL,
	soundVolume INT NOT NULL
);
GO

CREATE TABLE Player (
	idPlayer INT PRIMARY KEY IDENTITY (1,1),
	facebook VARCHAR(45),
	instagram VARCHAR(45),
	x VARCHAR(45),
	totalWins INT NOT NULL DEFAULT 0,
	totalLosses INT NOT NULL DEFAULT 0,
	totalPoints INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE UserAccount (
	idUser INT PRIMARY KEY IDENTITY (1,1),
	name VARCHAR(50) NOT NULL,
	email VARCHAR(254) NOT NULL,
	password VARCHAR(63) NOT NULL,
	username VARCHAR(45) NOT NULL,
	nickname VARCHAR(45) NOT NULL,
	idConfiguration INT NOT NULL,
	idPlayer INT NOT NULL,
	FOREIGN KEY (idConfiguration) REFERENCES Configuration(idConfiguration),
	FOREIGN KEY (idPlayer) REFERENCES Player(idPlayer)
);
GO

CREATE TABLE Friendship (
	idFriendship INT PRIMARY KEY IDENTITY (1,1),
	status VARCHAR(45) NOT NULL,
	idUserFriend INT NOT NULL,
	idUser INT NOT NULL,
	FOREIGN KEY (idUser) REFERENCES UserAccount(idUser),
	FOREIGN KEY (idUserFriend) REFERENCES UserAccount(idUser)
);
GO

CREATE TABLE FriendRequest (
	idFriendRequest INT PRIMARY KEY IDENTITY (1,1),
	date DATETIME NOT NULL,
	status VARCHAR(45) NOT NULL,
	idReceiverUser INT NOT NULL,
	idUser INT NOT NULL,
	FOREIGN KEY (idUser) REFERENCES UserAccount(idUser),
	FOREIGN KEY (idReceiverUser) REFERENCES UserAccount(idUser)
);
GO

CREATE TABLE StrikeKind (
	idStrikeKind INT PRIMARY KEY IDENTITY (1,1),
	name VARCHAR(45) NOT NULL,
	description VARCHAR(100) NOT NULL
);
GO

CREATE TABLE Strike (
	idStrike INT PRIMARY KEY IDENTITY (1,1),
	startDate DATE NOT NULL,
	endDate DATE NOT NULL,
	idStrikeKind INT NOT NULL,
	FOREIGN KEY (idStrikeKind) REFERENCES StrikeKind(idStrikeKind)
);
GO

CREATE TABLE UserHasStrike (
	idStrike INT NOT NULL,
	idUser INT NOT NULL,
	PRIMARY KEY (idStrike, idUser),
	FOREIGN KEY (idStrike) REFERENCES Strike(idStrike),
	FOREIGN KEY (idUser) REFERENCES UserAccount(idUser)
);
GO

CREATE TABLE GeneralMatch (
	idGeneralMatch INT PRIMARY KEY IDENTITY(1,1),
	date DATETIME NOT NULL,
	gameTime TIME NOT NULL
);
GO

CREATE TABLE MatchParticipants (
	idMatchParticipant INT PRIMARY KEY IDENTITY(1,1),
	idGeneralMatch INT NOT NULL,
	idPlayer INT NOT NULL,
	points INT NOT NULL,
	isWinner BIT NOT NULL,
	isDefeated BIT NOT NULL,
	FOREIGN KEY (idGeneralMatch) REFERENCES GeneralMatch(idGeneralMatch),
	FOREIGN KEY (idPlayer) REFERENCES Player(idPlayer)
);
GO



