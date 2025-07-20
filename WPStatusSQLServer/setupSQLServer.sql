-- Datenbank muss bereits erstellt sein. 
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[WPstatus](
	[statid] [int] IDENTITY(1,1) NOT NULL,
	[statdate] [datetime2](7) NULL,
	[status] [varchar](50) NULL,
	[mailSent] [datetime2](7) NULL,
	[statdatehalfday]  AS (dateadd(hour,(((datepart(hour,[statdate])+(4))/(12))*(12)+(8))-(12),CONVERT([datetime2],datefromparts(datepart(year,[statdate]),datepart(month,[statdate]),datepart(day,[statdate]))))),
 CONSTRAINT [PK_WPstatus_statid] PRIMARY KEY CLUSTERED 
(
	[statid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[WPstatus] ADD  DEFAULT (getdate()) FOR [statdate]
GO

CREATE   PROCEDURE [dbo].[spStoreWPstatus]
@currentState varchar(50),
@haschanged BIT OUT,
@statid INT OUT
AS
DECLARE @laststate varchar(50)
SELECT @laststate=(SELECT top 1 status from wpstatus order BY statid desc)
insert INTO WPstatus (status) VALUES (@currentState) 
SELECT @statid=SCOPE_IDENTITY()
IF @laststate<>@currentState 
  SET @haschanged=1
ELSE
  SET @haschanged=0

GO



CREATE   PROCEDURE [dbo].[spWPmailHasBeenSent]
@statid int
AS 

UPDATE wpstatus SET mailsent=getdate() where statid=@statid

GO


CREATE FUNCTION [dbo].[fnWPstatusJSON] ()
RETURNS varchar(max)
AS
BEGIN
  RETURN (SELECT TOP 11 status,format(statdate,'dd.MM.yyyy HH:mm') AS statusdate, format(mailsent,'dd.MM.yyyy HH:mm') AS mailsent FROM WPstatus order BY statid DESC FOR json AUTO)
  END
GO
