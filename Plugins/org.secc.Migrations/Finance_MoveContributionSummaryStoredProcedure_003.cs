using Rock.Plugin;


namespace org.secc.Migrations
{
    [MigrationNumber(32, "1.12.7")]
    public class Finance_MoveContributionSummaryStoredProcedure_003 : Migration
    {
        public override void Up()
        {
			Sql(@"
ALTER PROCEDURE [dbo].[_org_secc_Commitment_GetTotalsByPersonId]
(
 @PersonAliasID INT,
 @EndDate DATETIME
)
as

DECLARE @PersonId INT 
DECLARE @BirthDate DATETIME
DECLARE @GivingId NVARCHAR(50)
DECLARE @FamilyID INT
DECLARE @CommitmentPledgeAmount DECIMAL(18,2)
DECLARE @CommitmentGiftAmount DECIMAL (18,2)
DECLARE @MoveStartDate DATE = '2025-03-31'

SELECT TOP 1 
	@PersonId = PersonId,
	@Birthdate = Birthdate,
	@GivingId = GivingId,
	@FamilyID = PrimaryFamilyId
FROM Person p
INNER JOIN PersonAlias pa on p.Id = pa.PersonId
WHERE pa.Id = @PersonAliasId

CREATE TABLE #tmpEarlyGiftTranasactionIds
( 
	FinancialTransactionId INT
)

INSERT INTO #tmpEarlyGiftTranasactionIds
SELECT Id 
FROM FinancialTransaction ft
WHERE ft.TransactionCode in 
(
	'STOCK1','STOCK2','STOCK3','STOCK4','STOCK5','58519297665','59068380364','57273615371',
	'57521955446','58220081991','58517270827','58517270831','57521955485','QCD2','57889760328',
	'58517270811','58220262314','58220584902','58519245935','59068380379','57889760320','58220081987',
	'58517270823','59067674584','59067674597','58517270836','57889760324','59067674604','59067674588',
	'57521955450','57521955454','59068380331','58220082016','58220081983','58517270840','55587939014',
	'57273615367','57889760340','57889760344','58220082000','59068291787','59067674620','53925029454',
	'59067674628','54254307671','58220081995','59067674616','58220082004','59067674608','59067674612',
	'58517270819','57889760348','57889765214','58519245951','59067674592','57273615376','58220082012',
	'59067674632','57889760335','QCD1','59068304800','58517270815','58220584195','58220082008','59067674624',
	'QCD3','59333337823','59333337815','59386236532','59333337811','59334895632','59333337791','59333337799',
	'59335429416','59333337807','59335202789','59333337803','59335239761','59333337795','59335485819',
	'59335427377','59333337787','59335918036','59333337767','59333337771','59333337763','59333337783',
	'59334895609','59333502357','59333499682','59333337779','59333337775','59335711223','59333499722',
	'59333337692','59333337744','59333337708','59333337704','59333337740','59333337759','59333337752',
	'59335711261','59333337748','59333337700','59333337728','59333337716','59333337720','59333337724',
	'59335536526','59333337732','59333337696','59333337712','59335918024','STOCK6','QCD4','QCD5',
	'59264105875','59207562970','59258225816','59258225832','59287965126','59499728206','58395762776',
	'QCD6'
)

CREATE TABLE #tmpAccounts
(
	AccountId INT
)

INSERT INTO #tmpAccounts
SELECT Id
FROM FinancialAccount
WHERE ParentAccountId = 896
UNION ALL
SELECT 896


CREATE TABLE #tmpFamilyAdultPersonIds
(
	PersonId INT
)


IF @BirthDate >= DATEADD(YEAR, -19, CAST(GETDATE() AS DATE)) OR LEFT(@GivingId, 1) <> 'G'
BEGIN
	INSERT INTO #tmpFamilyAdultPersonIds
	SELECT @PersonId
END
ELSE IF LEFT(@GivingId, 1) = 'G'
BEGIN
	INSERT INTO #tmpFamilyAdultPersonIds
	SELECT 
		gm.PersonId
	FROM GroupMember gm 
	WHERE GroupId = @FamilyID
		AND GroupRoleId = 3
		AND GroupMemberStatus = 1
END

	SELECT 
		@CommitmentPledgeAmount = SUM(Amount)
	FROM _org_secc_Commitment c
	WHERE c.PersonId in (SELECT PersonId FROM #tmpFamilyAdultPersonIds)
		and c.IsDeleted = 0

	SELECT  @CommitmentGiftAmount = SUM(Amount)
	FROM
	(

	SELECT SUM(td.Amount) Amount
	FROM FinancialTransactionDetail td
	INNER JOIN FinancialTransaction t on td.TransactionId = t.Id
	INNER JOIN PersonAlias pa on t.AuthorizedPersonAliasId = pa.Id
	INNER JOIN Person p on pa.PersonId = p.Id
	WHERE AccountId in (SELECT AccountId FROM #tmpAccounts)
		AND t.TransactionTypeValueId = 53
		AND t.TransactionDateTime >= @MoveStartDate
		and t.TransactionDateTime < @EndDate
		and p.Id in (Select PersonId from #tmpFamilyAdultPersonIds)
	UNION ALL
	SELECT SUM(td.Amount) Amount
	FROM FinancialTransactionDetail td
	INNER JOIN FinancialTransaction t on td.TransactionId = t.id
	INNER JOIN #tmpEarlyGiftTranasactionIds egt on t.Id = egt.FinancialTransactionId
	INNER JOIN PersonAlias pa on t.AuthorizedPersonAliasId = pa.Id
	INNER JOIN Person p on pa.PersonId = p.Id
	WHERE t.TransactionTypeValueId = 53
		and td.AccountId in (select AccountId FROM #tmpAccounts)
		and p.Id in (SELECT PersonId FROM #tmpFamilyAdultPersonIds)
	UNION ALL
	SELECT SUM(sft.Amount) Amount
	FROM _org_secc_CommitmentSplitFundTransaction sft
	INNER JOIN #tmpFamilyAdultPersonIds p on sft.PersonId = p.PersonId
	INNER JOIN FinancialTransaction t on sft.TransactionId = t.Id
	WHERE t.TransactionDateTime > @MoveStartDate
		AND t.TransactionDateTime < @EndDate
		and p.PersonId in (SELECT PersonId FROM #tmpFamilyAdultPersonIds )
	) A

SELECT @PersonId AS PersonId,
	ISNULL(@CommitmentPledgeAmount, 0.00) as AmountPledged,
	ISNULL(@CommitmentGiftAmount, 0.00) as AmountGiven,
	@EndDate as StatusDate
	
DROP TABLE #tmpFamilyAdultPersonIds
DROP TABLE #tmpEarlyGiftTranasactionIds
DROP TABLE #tmpAccounts

            ");
        }
        public override void Down()
        {

        }


    }
}
