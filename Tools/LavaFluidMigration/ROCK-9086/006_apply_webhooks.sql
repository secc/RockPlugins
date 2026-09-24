-- ROCK9086 pass 3: Lava/Subsplash webhook templates (DefinedType 236 "Lava Webhook", 277 "Subsplash Webhook") found by
-- crawling https://sedev.secc.org/api/subsplash/webhook/* with the engine on FluidVerification (2026-09-24).
-- Same shape as 002/005: guarded REPLACE per row, @DryRun = 1 rolls back, backups to dbo._ROCK9086_LavaBackup.
-- Clear the Rock cache afterwards (DefinedValue attribute values are cached).
--
-- Class                                Rows  Change
-- Replace:'"','\"'  (json-escape)       20   -> Replace:'"',bsq  plus {%- capture bsq -%}\"{%- endcapture -%} prepended (Fluid decodes the literal;
--                                            the filter was a no-op and the JSON the app receives was invalid)
-- Replace:'\','\\'  (backslash)          4   -> Replace:bs,bsbs plus two captures prepended (Fluid reads \' as an escaped quote -> parse error)
-- "{{colors:brand}}"                     1   -> "" (Fluid parse error; DotLiquid always emitted "")
-- Split:'|'- %}     (bad trim)           1   -> Split:'|' -%} (Fluid: "End of tag '%}' was expected")
-- All rewrites rendered identically on Fluid and RockLiquid with the LavaProbe harness (cases A4/B2/C2, and bsq from PR #304).
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
DECLARE @je nvarchar(50) = N'Replace:''"'',''\"''', @jeNew nvarchar(50) = N'Replace:''"'',bsq ';
DECLARE @bs nvarchar(50) = N'Replace:''\'',''\\''', @bsNew nvarchar(50) = N'Replace:bs,bsbs';
DECLARE @bsqDecl nvarchar(100) = N'{%- capture bsq -%}\"{%- endcapture -%}';
DECLARE @bsDecl nvarchar(200) = N'{%- capture bs -%}\{%- endcapture -%}{%- capture bsbs -%}\\{%- endcapture -%}';
DECLARE @nl nvarchar(2) = NCHAR(13) + NCHAR(10);
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());

-- backups (AttributeValue.Value of the defined value's Template attribute)
INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue)
SELECT 'AttributeValue', [Id], 'Value', [Value] FROM AttributeValue av
WHERE av.[Id] IN (46672566, 99299825, 65039673, 3887881, 3889165, 79972266, 79074126, 34736223, 15714173, 3852785, 56250147, 56248378, 56249663,
                  122133011, 123192263, 161473677, 122146389, 123217869, 123217886, 123721033, 78651559)
  AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'AttributeValue' AND b.Id = av.[Id]);

-- json-escape: 20 rows (13 Lava Webhook + 7 Subsplash Webhook). Guard: old form present, no bsq capture yet.
DECLARE @jeIds TABLE (Id int, DvId int);
INSERT @jeIds VALUES (46672566, 22281), (65039673, 27357), (3887881, 12941), (3889165, 12942), (79972266, 35240), (79074126, 33690), (34736223, 19885),
                     (15714173, 17055), (3852785, 12770), (56250147, 24909), (56248378, 24907), (56249663, 24908),
                     (122133011, 50467), (123192263, 50486), (161473677, 54028), (122146389, 50468), (123217869, 50487), (123217886, 50488), (123721033, 50684);
DECLARE @id int, @dv int;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Id, DvId FROM @jeIds;
OPEN c; FETCH NEXT FROM c INTO @id, @dv;
WHILE @@FETCH_STATUS = 0
BEGIN
    UPDATE AttributeValue SET [Value] = @bsqDecl + @nl + REPLACE([Value], @je, @jeNew)
    WHERE [Id] = @id AND CHARINDEX(@je, [Value]) > 0 AND CHARINDEX(N'capture bsq', [Value]) = 0;
    INSERT @log VALUES ('AttributeValue', @dv, 'json-escape', @@ROWCOUNT);
    FETCH NEXT FROM c INTO @id, @dv;
END
CLOSE c; DEALLOCATE c;

-- backslash: 4 Subsplash Webhook rows (sekids). Runs after json-escape so the bsq capture is already the first line; the bs captures go in front of it.
DECLARE @bsIds TABLE (Id int, DvId int);
INSERT @bsIds VALUES (122133011, 50467), (123192263, 50486), (161473677, 54028), (123217869, 50487);
DECLARE c2 CURSOR LOCAL FAST_FORWARD FOR SELECT Id, DvId FROM @bsIds;
OPEN c2; FETCH NEXT FROM c2 INTO @id, @dv;
WHILE @@FETCH_STATUS = 0
BEGIN
    UPDATE AttributeValue SET [Value] = @bsDecl + @nl + REPLACE([Value], @bs, @bsNew)
    WHERE [Id] = @id AND CHARINDEX(@bs, [Value]) > 0 AND CHARINDEX(N'capture bs ', [Value]) = 0;
    INSERT @log VALUES ('AttributeValue', @dv, 'backslash-replace', @@ROWCOUNT);
    FETCH NEXT FROM c2 INTO @id, @dv;
END
CLOSE c2; DEALLOCATE c2;

-- colors-brand: /sermondiscussion (DV 32999)
UPDATE AttributeValue SET [Value] = REPLACE([Value], N'"brandableElements":"{{colors:brand}}"', N'"brandableElements":""')
WHERE [Id] = 78651559 AND CHARINDEX(N'"brandableElements":"{{colors:brand}}"', [Value]) > 0;
INSERT @log VALUES ('AttributeValue', 32999, 'colors-brand', @@ROWCOUNT);

-- bad trim: /campusdata (DV 43357)
UPDATE AttributeValue SET [Value] = REPLACE([Value], N'| Split:''|''- %}', N'| Split:''|'' -%}')
WHERE [Id] = 99299825 AND CHARINDEX(N'| Split:''|''- %}', [Value]) > 0;
INSERT @log VALUES ('AttributeValue', 43357, 'bad-trim', @@ROWCOUNT);

SELECT * FROM @log ORDER BY FixRule, Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] <> 1) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';

-- verify inside the transaction
SELECT av.Id AttrValueId, dv.Id DvId, dv.Value Route,
       CHARINDEX(@je, av.Value) OldJsonEsc, CHARINDEX(@bs, av.Value) OldBackslash, CHARINDEX(N'{{colors:', av.Value) OldColors, CHARINDEX(N'- %}', av.Value) OldBadTrim,
       CHARINDEX(N'capture bsq', av.Value) HasBsq, CHARINDEX(N'capture bs ', av.Value) HasBs
FROM AttributeValue av JOIN DefinedValue dv ON dv.Id = av.EntityId
WHERE av.Id IN (46672566, 99299825, 65039673, 3887881, 3889165, 79972266, 79074126, 34736223, 15714173, 3852785, 56250147, 56248378, 56249663,
                122133011, 123192263, 161473677, 122146389, 123217869, 123217886, 123721033, 78651559)
ORDER BY dv.DefinedTypeId, dv.Value;

IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END
