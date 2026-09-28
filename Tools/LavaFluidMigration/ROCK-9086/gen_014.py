"""ROCK-9086 pass 11: rows production logged under Fluid verification 2026-09-25..28.
Reads scratchpad/vals/p14-<Table>-<Id>-<Col>.txt (exact prod values), applies literal (old, new) rules, and writes
Tools/LavaFluidMigration/ROCK-9086/014_apply_prodpass5.sql with per-row guarded REPLACEs (same shape as 005-013)."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = r"C:\Users\stephenl\source\repos\RockPlugins-16"
OUT = os.path.join(REPO, r"Tools\LavaFluidMigration\ROCK-9086\014_apply_prodpass5.sql")
SQ = chr(39)

R_JOB = [("job.[" + SQ + "Job Serial" + SQ + "]", "job[" + SQ + "Job Serial" + SQ + "]")]
R_PIPE_ITEM = [("item | | Attribute", "item | Attribute")]
R_ADDRESS = [("Address,'Home',", "Address:'Home',")]
R_VISITS = [("{% if lastActivityDate >= '08/2/2019' %}",
             "{% assign showActionsCutoff = '2019-08-02' | AsDateTime %}{% if lastActivityDate >= showActionsCutoff %}")]
R_LEADTIME = [("'MinLeadTime'| | DateAdd", "'MinLeadTime' | DateAdd")]
R_REGEX_IF = [("{% if url | RegExMatch:'^[/~].*$' %}",
               "{% assign urlIsRelative = url | RegExMatch:'^[/~].*$' %}{% if urlIsRelative %}")]
R_REFERENCE = [("recommended == true", "recommended == 'True'"),
               ("reasonToNotVolunteer == false", "reasonToNotVolunteer == 'False'"),
               ("isMinor == true", "isMinor == 'True'")]
R_QUOTE = [("Attribute:'LocationShortCode''}}", "Attribute:'LocationShortCode'}}")]
R_PARENS = [("{% if (Registraion == 'None') AND (AddRegCom != ' ') %}", "{% if Registraion == 'None' and AddRegCom != ' ' %}")]
R_SIZE = [("| Size asInteger ", "| Size "), ("| Size  asInteger ", "| Size ")]
R_SPOUSE = [("'Spouse':'NickName'", "'Spouse','NickName'")]
R_WEEK = [('{{row.WeekStart | Replace:" 12:00:00 AM",""}}', "{{row.WeekStart | Date:'M/d/yyyy'}}"),
          ('{{row.WeekEnd | Replace:" 12:00:00 AM",""}}', "{{row.WeekEnd | Date:'M/d/yyyy'}}")]
R_MUSTACHE_IF = [("{% if {{row.Status}} != 'Active' %}", "{% if row.Status != 'Active' %}"),
                 ("{% if {{row.OneXStory}} == 'True' %}", "{% if row.OneXStory == 'True' %}")]
R_WHEN = [("{%- when '19' and meetingType == '' -%}", "{%- when '19' -%}")]
# behaviour fixes the owner asked for (2026-09-28): both were broken on DotLiquid too
R_RENEWAL = [("isRenewal = Workflow | Attribute:'IsRenewal' ", "isRenewal = Workflow | Attribute:'IsRenewal','RawValue' "),
             ("isRenewal == True ", "isRenewal == 'True' ")]
R_MSEXPIRED = [("CurrentPerson | PersonInDataView:'1678'", "CurrentPerson.Id | IsInDataView:'1678'")]

# (table, id, column, fix-rule label, rules)
ROWS = [
    ("HtmlContent", 4591, "Content", "dot-bracket", R_JOB),
    ("HtmlContent", 5306, "Content", "double-pipe", R_PIPE_ITEM),
    ("WorkflowActionForm", 289, "Header", "comma-args", R_ADDRESS),
    ("WorkflowActionForm", 292, "Header", "comma-args", R_ADDRESS),
    ("WorkflowActionForm", 339, "Header", "comma-args", R_ADDRESS),
    ("WorkflowActionForm", 691, "Header", "comma-args", R_ADDRESS),
    ("WorkflowActionForm", 36, "Footer", "date-vs-string", R_VISITS),
    ("WorkflowActionForm", 565, "Header", "double-pipe", R_LEADTIME),
    ("WorkflowActionForm", 566, "Header", "double-pipe", R_LEADTIME),
    ("SystemCommunication", 9, "Body", "filter-in-if", R_REGEX_IF),
    ("AttributeValue", 418179915, "Value", "rawvalue-bool", R_REFERENCE),
    ("AttributeValue", 419773863, "Value", "rawvalue-bool", R_REFERENCE),
    ("AttributeValue", 119518117, "Value", "extra-quote", R_QUOTE),
    ("AttributeValue", 119518133, "Value", "extra-quote", R_QUOTE),
    ("AttributeValue", 181166313, "Value", "extra-quote", R_QUOTE),
    ("AttributeValue", 119518134, "Value", "parens-AND", R_PARENS),
    ("AttributeValue", 181933899, "Value", "parens-AND", R_PARENS),
    ("AttributeValue", 51107841, "Value", "size-junk", R_SIZE),
    ("AttributeValue", 3889252, "Value", "colon-args", R_SPOUSE),
    ("AttributeValue", 101737557, "Value", "date-tostring", R_WEEK),
    ("AttributeValue", 211932365, "Value", "mustache-in-if", R_MUSTACHE_IF),
    ("AttributeValue", 55093101, "Value", "when-and", R_WHEN),
    ("AttributeValue", 58019746, "Value", "when-and", R_WHEN),
    ("AttributeValue", 3752511, "Value", "renewal-rawvalue", R_RENEWAL),
    ("AttributeValue", 3752514, "Value", "renewal-rawvalue", R_RENEWAL),
    ("AttributeValue", 71943274, "Value", "renewal-rawvalue", R_RENEWAL),
    ("AttributeValue", 71943275, "Value", "renewal-rawvalue", R_RENEWAL),
    ("AttributeValue", 446668184, "Value", "renewal-rawvalue", R_RENEWAL),
    ("AttributeValue", 446668185, "Value", "renewal-rawvalue", R_RENEWAL),
    ("HtmlContent", 12330, "Content", "unknown-filter", R_MSEXPIRED),
]

# AttributeValue Id -> (AttributeId, EntityId) on production: ids are not guaranteed to be the same row on dev, so the
# update and the "absent" (-1) check both match all three
AV_KEYS = {}
for kv in open(os.path.join(HERE, "vals", "p14-av-keys.txt"), encoding="utf-8").read().strip().split(","):
    i_, a_, e_ = (int(x) for x in kv.split(":"))
    AV_KEYS[i_] = (a_, e_)

def q(s):
    assert all(ord(c) < 128 for c in s), s
    return "N'" + s.replace("'", "''") + "'"

def cnt(col, o):
    # DATALENGTH, not LEN: LEN drops trailing spaces and some rules end in one
    return "(DATALENGTH(%s) - DATALENGTH(REPLACE(%s, %s, N''))) / DATALENGTH(%s)" % (col, col, q(o), q(o))

L = ["""-- ROCK9086 pass 11: rows production logged under Fluid verification 2026-09-25..28 (generated by gen_014.py).
-- Every rewrite keeps the DotLiquid output (LavaProbe cases 27-31) except three behaviour fixes:
--   SystemCommunication 9: the assign-then-if fix WorkflowActionForm 531 already has (DotLiquid ignored the RegExMatch, so
--     it prefixed every URL).
--   renewal-rawvalue (MinistrySafe email subject/body, workflow types 108 and 421 and one more copy): the formatted Boolean
--     is 'Yes'/'No', which never equals True on DotLiquid and always does on Fluid; now RawValue == 'True', so renewals
--     get the renewal wording.
--   unknown-filter (HtmlContent 12330, MyVolunteerStatus): PersonInDataView is not a Lava filter, so "Expired" never showed
--     on DotLiquid and always on Fluid; now IsInDataView on data view 1678 "Ministry Safe Training is Expired".
--   dot-bracket     job.['Job Serial']            -> job['Job Serial']
--   double-pipe     x | | Filter                  -> x | Filter
--   comma-args      Address,'Home',...            -> Address:'Home',...   (DotLiquid reads the comma as the colon)
--   colon-args      Attribute:'Spouse':'NickName' -> Attribute:'Spouse','NickName'
--   date-vs-string  DateTime >= '08/2/2019'       -> compare with '2019-08-02' | AsDateTime (Fluid is false for the string)
--   filter-in-if    {% if url | RegExMatch %}     -> assign first
--   rawvalue-bool   RawValue == true / false      -> == 'True' / 'False' (Fluid: any non-empty string equals true)
--   extra-quote     'LocationShortCode''          -> 'LocationShortCode'
--   parens-AND      (a == x) AND (b != y)         -> a == x and b != y ('Registraion' is misspelt and stays false on both)
--   size-junk       | Size asInteger              -> | Size
--   date-tostring   DateTime | Replace:" 12:00:00 AM","" -> | Date:'M/d/yyyy' (Fluid prints the DateTime with an offset)
--   mustache-in-if  {% if {{row.X}} ... %}        -> {% if row.X ... %}
--   when-and        when '19' and meetingType ... -> when '19' (DotLiquid ignored the 'and')
-- Guarded REPLACE per row, @DryRun = 1 rolls back, the live run is all-or-nothing, backups to dbo._ROCK9086_LavaBackup,
-- rows absent from the target database log -1. HtmlContent rows are guarded to the current version. Clear the Rock cache after.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
DECLARE @avkeys TABLE (Id int, AttributeId int, EntityId int);
INSERT @avkeys VALUES """ + ", ".join("(%d, %d, %d)" % (k, v[0], v[1]) for k, v in sorted(AV_KEYS.items())) + """;
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());
"""]
verify = []
for tbl, i, col, label, rules in ROWS:
    p = os.path.join(HERE, "vals", "p14-%s-%d-%s.txt" % (tbl, i, col))
    t = open(p, encoding="utf-8", newline="").read()
    pairs = []
    for o, n in rules:
        c = t.count(o)
        if c:
            pairs.append((o, n, c))
    assert pairs, (tbl, i, "no rule matched")
    new = t
    for o, n, c in pairs:
        new = new.replace(o, n)
    for o, n, c in pairs:
        assert o not in new or o in n, (tbl, i, o)
    colq = "[%s]" % col
    # always back up the pre-pass value: some rows were already backed up by an earlier pass (BackedUpAt tells them apart)
    L.append("INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT '%s', x.[Id], '%s', x.%s FROM [%s] x WHERE x.[Id] = %d;"
             % (tbl, col, colq, tbl, i))
    expr = colq
    for o, n, c in pairs:
        expr = "REPLACE(%s, %s, %s)" % (expr, q(o), q(n))
    conds = ["[Id] = %d" % i] + ["%s = %d" % (cnt(colq, o), c) for o, n, c in pairs]
    if tbl == "AttributeValue":
        conds[0] += " AND [AttributeId] = %d AND [EntityId] = %d" % AV_KEYS[i]
    if tbl == "HtmlContent":
        conds.append("[Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId)")
    L.append("UPDATE [%s] SET %s = %s\nWHERE %s;" % (tbl, colq, expr, "\n  AND ".join(conds)))
    L.append("INSERT @log VALUES ('%s', %d, '%s', @@ROWCOUNT);\n" % (tbl, i, label))
    still = " + ".join("CASE WHEN CHARINDEX(%s COLLATE Latin1_General_CS_AS, %s COLLATE Latin1_General_CS_AS) > 0 THEN 1 ELSE 0 END"
                       % (q(o), colq) for o, n, c in pairs if o not in n)
    verify.append("SELECT '%s' Tbl, %d Id, %s StillOld FROM [%s] WHERE [Id] = %d" % (tbl, i, still or "0", tbl, i))
    for o, n, c in pairs:
        print("%-19s %9d %-6s x%d  %s  ->  %s" % (tbl, i, col, c, o[:70], n[:70]))

L.append("""UPDATE l SET [Rows] = -1 FROM @log l WHERE l.[Rows] = 0 AND NOT EXISTS (
    SELECT 1 FROM HtmlContent x WHERE l.Tbl = 'HtmlContent' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM WorkflowActionForm x WHERE l.Tbl = 'WorkflowActionForm' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM SystemCommunication x WHERE l.Tbl = 'SystemCommunication' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM AttributeValue x JOIN @avkeys k ON k.Id = x.Id AND k.AttributeId = x.AttributeId AND k.EntityId = x.EntityId
        WHERE l.Tbl = 'AttributeValue' AND x.Id = l.Id);
SELECT * FROM @log ORDER BY Tbl, Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1)) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1))
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 11: a guarded update did not match exactly one row; nothing committed.', 1; END
""")
L.append("\nUNION ALL\n".join(verify) + ";\n")
L.append("IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END\n")
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L))
print("rows:", len(ROWS), "->", OUT)
