"""ROCK-9086 pass 13: rows production logged under Fluid verification 2026-09-30 .. 2026-10-07.
Reads vals/p18-<Table>-<Id>[-<Col>].txt (exact prod values), applies literal (old, new) rules, and writes
018_apply_prodpass7.sql with per-row guarded REPLACEs (same shape as 016)."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
V = os.environ.get("ROCK9086_VALS", os.path.join(HERE, "vals"))  # prod dumps are not committed; point ROCK9086_VALS at them
OUT = os.path.join(HERE, "018_apply_prodpass7.sql")

# Event Request - Internal (type 72) summary view and emails: the same filter-in-parentheses if that 016 fixed in
# the forms. HasItOccurredBefore is a Boolean, formatted Yes/No, so compare the raw value
R_OCCURRED = [("{% if (Workflow | Attribute:'HasItOccurredBefore') == 'True' %}",
               "{% assign occurredBefore = Workflow | Attribute:'HasItOccurredBefore','RawValue' %}{% if occurredBefore == 'True' %}")]
# Sports & Fitness training workflow, Run Lava action: parentheses in if (LavaProbe 38 SF-*: same result)
R_SF = [("{% if (submitorPersonName != participantPersonName) and (minorStatus != 'Yes') %}",
         "{% if submitorPersonName != participantPersonName and minorStatus != 'Yes' %}")]
# RSVP Weekend Prayer follow-up emails: filter inside if, always true on DotLiquid (LavaProbe 38 CR-*)
R_REOPENED = [("{% if Workflow | Attribute:'CardReopened' == 'Yes' %}",
               "{% assign cardReopened = Workflow | Attribute:'CardReopened' %}{% if cardReopened == 'Yes' %}")]
# 2018 MIX camp pages: the HTML editor encoded <= (LavaProbe 38 HC-*, Q10/Q11: same result)
R_LTE_CLOSED = [("{% if registrationClosed &lt;= currentDate %}", "{% if registrationClosed <= currentDate %}")]
R_LTE_RELEASE = [("{% if releaseRegistration &lt;= currentDate %}", "{% if releaseRegistration <= currentDate %}")]
# Pastoral Care Nursing Home List (Dynamic Data block 9054, page 534): an integer 0 == '' is TRUE on Fluid
# (LavaProbe P1/P3), so a 0 visit count rendered blank; Communion is a bit, and bool == 'false' is TRUE on Fluid
# (LavaProbe Q6), so the check icon flipped. Compare numbers to null only; take the bit through AsBoolean (Q1-Q5).
R_PASTORAL = [("{% if row.Age == '' or row.Age == null %}", "{% if row.Age == null %}"),
              ("{% if row.Visits == '' or row.Visits == null %}", "{% if row.Visits == null %}"),
              # the branches swap so a NULL bit still shows the times icon on both engines (Q3 SAME; Q14 `== false` DIFF)
              ("{% if row.Communion == '' or row.Communion == 'false' or row.Communion == 'False' or row.Communion == null %}\n"
               "                                <i class=\"fa fa-times\"></i>\n"
               "                            {% else %}\n"
               "                                <i class=\"fa fa-check\"></i>\n",
               "{% assign communion = row.Communion | AsBoolean %}{% if communion %}\n"
               "                                <i class=\"fa fa-check\"></i>\n"
               "                            {% else %}\n"
               "                                <i class=\"fa fa-times\"></i>\n")]

# Report 1532 Computer Clearance by Campus, Security Check column: parentheses in if (Fluid parse error) and a filter
# chain inside if (always true on DotLiquid, so "Attendance verified more than 6 months ago" always showed). Same
# assign-then-compare form the report owner used in the Badges column on 2026-09-30.
R_RF = [("{% if (CriminalCheckStatus == 'Approved') or (CriminalCheckStatus == 'Reapproved') %}",
         "{% if CriminalCheckStatus == 'Approved' or CriminalCheckStatus == 'Reapproved' %}"),
        ("{% if  Attendance1stVerifiedDate| Date:'M/d/yyyy' | DateDiff:'Now','d' > 186 %}",
         "{% assign daysSinceFirstAttend = Attendance1stVerifiedDate | DateDiff:'Now','d' %}{% if daysSinceFirstAttend > 186 %}")]
# (table, id, column, dump name, fix-rule label, rules)
ROWS = [
    ("ReportField", 71196, "Selection", "p18-RF-71196", "report-if", R_RF),
    ("WorkflowType", 72, "SummaryViewText", "p18-WT-72-Summary", "occurred-if", R_OCCURRED),
    ("AttributeValue", 3094280, "Value", "p18-AV-3094280", "occurred-if", R_OCCURRED),
    ("AttributeValue", 80610811, "Value", "p18-AV-80610811", "occurred-if", R_OCCURRED),
    ("AttributeValue", 80610821, "Value", "p18-AV-80610821", "occurred-if", R_OCCURRED),
    ("AttributeValue", 82143392, "Value", "p18-AV-82143392", "occurred-if", R_OCCURRED),
    ("AttributeValue", 206537195, "Value", "p18-AV-206537195", "parens-if", R_SF),
    ("AttributeValue", 449129641, "Value", "p18-AV-449129641", "reopened-if", R_REOPENED),
    ("AttributeValue", 449130783, "Value", "p18-AV-449130783", "reopened-if", R_REOPENED),
    ("AttributeValue", 449130791, "Value", "p18-AV-449130791", "reopened-if", R_REOPENED),
    ("HtmlContent", 546, "Content", "p18-HC-546", "encoded-lte", R_LTE_CLOSED),
    ("HtmlContent", 547, "Content", "p18-HC-547", "encoded-lte", R_LTE_RELEASE),
    ("HtmlContent", 548, "Content", "p18-HC-548", "encoded-lte", R_LTE_RELEASE),
    ("HtmlContent", 550, "Content", "p18-HC-550", "encoded-lte", R_LTE_RELEASE),
    ("HtmlContent", 554, "Content", "p18-HC-554", "encoded-lte", R_LTE_RELEASE),
    ("HtmlContent", 557, "Content", "p18-HC-557", "encoded-lte", R_LTE_RELEASE),
    ("AttributeValue", 418777152, "Value", "p18-AV-418777152", "int-vs-empty", R_PASTORAL),
]

# AttributeValue Id -> (AttributeId, EntityId) on production; ids are not the same row on every database
AV_KEYS = {3094280: (1016, 814), 80610811: (1016, 6340), 80610821: (1016, 6343), 82143392: (1016, 6590),
           206537195: (17501, 8357), 449129641: (1016, 76889), 449130783: (1016, 76902), 449130791: (1016, 76903),
           418777152: (1201, 9054)}

def q(s):
    assert all(ord(c) < 128 for c in s), s
    return "N'" + s.replace("'", "''") + "'"

def cnt(col, o):
    # DATALENGTH, not LEN: LEN drops trailing spaces
    return "(DATALENGTH(%s) - DATALENGTH(REPLACE(%s, %s, N''))) / DATALENGTH(%s)" % (col, col, q(o), q(o))

L = ["""-- ROCK9086 pass 13 (generated by gen_018.py): rows production logged under Fluid verification 2026-09-30..10-07.
-- Behaviour changes on DotLiquid (same class as 016, approved 2026-09-30):
--   occurred-if (Event Request - Internal 72: summary view and 4 emails): a filter in an if was always true on
--     DotLiquid, so "When/Where was it held" always showed; now only when the event occurred before.
--   reopened-if (RSVP Weekend Prayer follow-up emails, prod only): "Their card had been closed" showed in every
--     email; now only when the card was reopened.
--   report-if (report 1532 Security Check column): "Attendance verified more than 6 months ago" showed for everyone
--     (filter chain inside if); now only past 186 days. The parenthesised or is a Fluid parse error, same on DotLiquid.
-- Same output on DotLiquid: parens-if (Sports & Fitness training Run Lava action), encoded-lte (2018 MIX camp
--   pages 1159-1164), int-vs-empty (Pastoral Care Nursing Home List: 0 == '' and bool == 'false' are TRUE on Fluid,
--   so a 0 visit count went blank and the Communion icon flipped; DotLiquid output unchanged).
--   LavaProbe cases 38, P1-P10, Q1-Q11.
-- Guarded REPLACE per row, @DryRun = 1 rolls back, the live run is all-or-nothing, one backup per row to
-- dbo._ROCK9086_LavaBackup, rows absent from the target database log -1. Clear the Rock cache after.
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
for tbl, i, col, dump, label, rules in ROWS:
    t = open(os.path.join(V, dump + ".txt"), encoding="utf-8", newline="").read()
    pairs = []
    for o, n in rules:
        c = t.count(o)
        if c:
            pairs.append((o, n, c))
    assert len(pairs) == len(rules), (tbl, i, "a rule did not match", [o for o, n in rules if o not in t])
    new = t
    for o, n, c in pairs:
        new = new.replace(o, n)
    for o, n, c in pairs:
        assert o not in new or o in n, (tbl, i, o)
    colq = "[%s]" % col
    # one backup per row, holding the original value; AttributeValue also matches AttributeId/EntityId
    key = " AND x.[AttributeId] = %d AND x.[EntityId] = %d" % AV_KEYS[i] if tbl == "AttributeValue" else ""
    L.append("INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT '%s', x.[Id], '%s', x.%s FROM [%s] x WHERE x.[Id] = %d%s\n"
             "  AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = '%s' AND b.Id = x.[Id] AND b.Col = '%s');"
             % (tbl, col, colq, tbl, i, key, tbl, col))
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
        print("%-19s %9d %-15s x%d  %s  ->  %s" % (tbl, i, col, c, o[:60].replace("\n", " "), n[:60].replace("\n", " ")))

L.append("""UPDATE l SET [Rows] = -1 FROM @log l WHERE l.[Rows] = 0 AND NOT EXISTS (
    SELECT 1 FROM WorkflowType x WHERE l.Tbl = 'WorkflowType' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM HtmlContent x WHERE l.Tbl = 'HtmlContent' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM WorkflowActionForm x WHERE l.Tbl = 'WorkflowActionForm' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM SystemCommunication x WHERE l.Tbl = 'SystemCommunication' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM Block x WHERE l.Tbl = 'Block' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM ReportField x WHERE l.Tbl = 'ReportField' AND x.Id = l.Id UNION ALL
    SELECT 1 FROM AttributeValue x JOIN @avkeys k ON k.Id = x.Id AND k.AttributeId = x.AttributeId AND k.EntityId = x.EntityId
        WHERE l.Tbl = 'AttributeValue' AND x.Id = l.Id);
SELECT * FROM @log ORDER BY Tbl, Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1)) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1))
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 13: a guarded update did not match exactly one row; nothing committed.', 1; END
""")
L.append("\nUNION ALL\n".join(verify) + ";\n")
L.append("IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END\n")
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L))
print("rows:", len(ROWS), "->", OUT)
