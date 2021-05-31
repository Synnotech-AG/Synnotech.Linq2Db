CREATE TABLE Employees(
    Id INT IDENTITY(1, 1) CONSTRAINT PK_Employees PRIMARY KEY CLUSTERED,
    [Name] NVARCHAR(50) NOT NULL,
    Age INT NOT NULL
);

INSERT INTO Employees ([Name], Age)
VALUES ('John Doe', 42);

INSERT INTO Employees ([Name], Age)
VALUES ('Jane Kingsley', 29);

INSERT INTO Employees ([Name], Age)
VALUES ('Audrey McGinnis', 39);