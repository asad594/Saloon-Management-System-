USE urooba;
GO

-- Add column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserActivityLog') AND name = 'LogMessage')
BEGIN
    ALTER TABLE UserActivityLog ADD LogMessage VARCHAR(255);
END
GO

-- Create or replace the trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_LogSuccessMessage')
    DROP TRIGGER trg_LogSuccessMessage;
GO

CREATE TRIGGER trg_LogSuccessMessage
ON UserActivityLog
AFTER INSERT
AS
BEGIN
    UPDATE u
    SET u.LogMessage = CASE 
        WHEN i.ActionType = 'LOGIN' THEN 'Login Successful'
        WHEN i.ActionType = 'LOGOUT' THEN 'Logout Successful'
        ELSE 'Activity: ' + i.ActionType
    END
    FROM UserActivityLog u
    JOIN inserted i ON u.LogId = i.LogId;
END;
GO
