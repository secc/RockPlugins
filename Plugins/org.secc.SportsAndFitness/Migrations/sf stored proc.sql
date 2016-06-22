USE [RockDBStaging]
GO
/****** Object:  StoredProcedure [dbo].[_org_secc_spSportsAndFitnessMigrate]   Script Date: 6/18/2016 5:33:06 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create PROC [dbo].[_org_secc_spSportsAndFitnessMigrate]

AS

declare @newGroupId int = 315318
declare @activeGroupId int = 295567
declare @expiredGroupId int = 312961
declare @redflagGroupId int = 304444

declare @oldGroupFitnessId int = 295563
declare @newGroupFitnessId int = 315319

declare @expirationAttributeID int = 3868
declare @sessionsAttributeId int = 3869
declare @sessionsPersonAttributeId int = 1954


--Move non-expired card members
insert into GroupMember
(PersonId, GroupRoleId, GroupMemberStatus, GroupId, CreatedDateTime, IsSystem, [GUID])
select PersonId, GroupRoleId, 1, @newGroupID, GetDate(), 0, NEWID()
 from GroupMember as GM
 where GroupId = @activeGroupId

 --insert values for expiration date
insert into AttributeValue
(EntityId, [Value], IsSystem, AttributeId,	[Guid])
 select   GM.Id, DATEADD(yyyy, DATEDIFF(yyyy, OGM.CreatedDateTime, GETDATE()), OGM.CreatedDateTime) , 0, @expirationAttributeId, NEWID()
 from GroupMember as GM
 join GroupMember as OGM on GM.PersonID = OGM.PersonID and OGM.GroupID = @activeGroupId
 where GM.GroupId = @newGroupId

 --add year to expired cards that should not be expired
 update AttributeValue
 set Value = DATEADD(yyyy,1,Convert(DATE, value))
where Convert ( DATE, value) < GETDATE()
and attributeid = @expirationAttributeID

--add group members from the expired card group
--set their status as 0 (inactive)
insert into GroupMember
(PersonId, GroupRoleId, GroupMemberStatus, GroupId, CreatedDateTime, IsSystem, [GUID])
select PersonId, GroupRoleId, 0,@newGroupID, GetDate(), 0, NEWID()
 from GroupMember as GM
 where GroupId = @expiredGroupId

 --insert expired dates for expired cards
insert into AttributeValue
(EntityId, [Value], IsSystem, AttributeId,	[Guid], CreatedDateTime)
 select   GM.Id, OGM.CreatedDateTime , 0, @expirationAttributeId, NEWID(), GETDATE()
 from GroupMember as GM
 join GroupMember as OGM on GM.PersonID = OGM.PersonID and OGM.GroupID = @expiredGroupId
 where GM.GroupId = @newGroupId and GM.GroupMemberStatus = 0

 --switch expired card members from inactive to active
 --active will allow them to come up on checkin
 --their expiration date will allow us to check if they are expired
 update GroupMember
 set GroupMemberStatus = 1
 where GroupId = @newGroupId

 --move over the red flagged members with a note
 insert into GroupMember
(PersonId, GroupRoleId, GroupMemberStatus, GroupId, CreatedDateTime, IsSystem, [GUID], Note)
select PersonId, GroupRoleId, 1,@newGroupID, GetDate(), 0, NEWID(), 'DO NOT CHECK-IN!'
 from GroupMember as GM
 where GroupId = @redflagGroupId

 --move over the group fitness members
 insert into GroupMember
(PersonId, GroupRoleId, GroupMemberStatus, GroupId, CreatedDateTime, IsSystem, [GUID])
select PersonId, GroupRoleId, 1, @newGroupFitnessId, GetDate(), 0, NEWID()
 from GroupMember as GM
 where GroupId = @oldGroupFitnessId


 --Insert values for group fitness Sessions
insert into AttributeValue
(EntityId, [Value], IsSystem, AttributeId,	[Guid], CreatedDateTime)
 select   GM.Id, AV.Value, 0, @sessionsAttributeId, NEWID(), GetDate()
 from GroupMember as GM
 join AttributeValue as AV on AV.EntityId = GM.PersonId and AV.AttributeId = @sessionsPersonAttributeId
 where GM.GroupId = @newGroupFitnessId