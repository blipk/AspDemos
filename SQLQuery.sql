--insert into Employees(ApplicationUserId, EmployeeNumber, SoftDeleteLevel, CreatedDate, UpdatedDate) select ApplicationUserId, EmployeeNumber, SoftDeleteLevel, CreatedDate, UpdatedDate from Employees;

UPDATE Employees SET SoftDeleteLevel = 0;
UPDATE AspNetUsers SET SoftDeleteLevel = 0;
SELECT * FROM Employees;
SELECT * FROM AspNetUsers;