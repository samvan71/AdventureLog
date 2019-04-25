/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script.	
 Use SQLCMD syntax to include a file in the pre-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the pre-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
IF NOT EXISTS (SELECT * from sys.credentials WHERE name = 'AdventureLogUser')
	CREATE CREDENTIAL [AdventureLogUser]
		WITH IDENTITY = N'AdventureLogUser',
		SECRET = N'Mc5jXq!tJJIZg1v9nozY'
