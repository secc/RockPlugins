"""ROCK-9086 pass 12: rows production logged under Fluid verification 2026-09-28 16:30 .. 2026-09-30.
Reads vals/p15-<Table>-<Id>[-<Col>].txt (exact prod values), applies literal (old, new) rules, and writes
016_apply_prodpass6.sql with per-row guarded REPLACEs (same shape as 014/015)."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
V = os.environ.get("ROCK9086_VALS", os.path.join(HERE, "vals"))  # prod dumps are not committed; point ROCK9086_VALS at them
OUT = os.path.join(HERE, "016_apply_prodpass6.sql")

# filter in parentheses inside if: DotLiquid treats the condition as always true, Fluid cannot parse it
R_VENUE = [("{% if (Workflow | Attribute:'WhatTypeOfVenueIsRequired') == 'Other' %}",
            "{% assign venueType = Workflow | Attribute:'WhatTypeOfVenueIsRequired' %}{% if venueType == 'Other' %}")]
# Event Request - Internal (type 72): HasItOccurredBefore is a Boolean, formatted Yes/No, so compare the raw value
R_OCCURRED = [("{% if (Workflow | Attribute:'HasItOccurredBefore') == 'True' %}",
               "{% assign occurredBefore = Workflow | Attribute:'HasItOccurredBefore','RawValue' %}{% if occurredBefore == 'True' %}")]
# Benevolence Request card: filter inside if (always true on DotLiquid); unquoted GUID is not a value on Fluid
R_BENEVOLENCE = [("{% if Workflow | Attribute:'MaritalStatus','RawValue' == 5fe5a540-7d9f-433e-b47e-4229d1472248 %}",
                  "{% assign maritalStatus = Workflow | Attribute:'MaritalStatus','RawValue' | Downcase %}{% if maritalStatus == '5fe5a540-7d9f-433e-b47e-4229d1472248' %}"),
                 ("{% if Workflow | Attribute:'WhyUnemployed' != null or Workflow | Attribute:'WhyUnemployed' != '' %}",
                  "{% assign whyUnemployed = Workflow | Attribute:'WhyUnemployed' %}{% if whyUnemployed != null and whyUnemployed != '' %}")]
# Deacon/Elder application: PageRedirect inside {% connectionrequest %} surfaces as an AggregateException on Fluid;
# keep the first URL and redirect after the block (same fix as LiveStreamRedirect.lava in pass 11)
R_DEACON = [("{% connectionrequest expression:", "{% assign redirectUrl = '' %}{% connectionrequest expression:"),
            ("        {{ redirectUrl | PageRedirect }}\n", ""),
            ("{% endconnectionrequest %}\n", "{% endconnectionrequest %}\n{% if redirectUrl != '' %}{{ redirectUrl | PageRedirect }}{% endif %}\n")]
# Sports & Fitness training: parentheses in if, {{ }} inside assign (LavaProbe C7-C11: same result, int compare)
R_SF_IF = [("{% if (connectStatus != '65') and (connectStatus != '146') and (connectStatus != '1473') %}",
            "{% if connectStatus != '65' and connectStatus != '146' and connectStatus != '1473' %}")]
R_SF_ASSIGN = [("{% assign connectStatus = {{CurrentPerson.ConnectionStatusValueId}} %}",
                "{% assign connectStatus = CurrentPerson.ConnectionStatusValueId %}")]
# W-9 Form: on Fluid any non-empty string == true; IsAutomated is a Boolean formatted Yes/No (LavaProbe A1-A5)
R_W9 = [("isAutomated == true", "isAutomated == 'True'")]
# TEST | PR Families: stray # after the number (DotLiquid matched nothing, so the column was always empty)
R_HOH = [("Where:'GroupRoleId', 10# |", "Where:'GroupRoleId', 10 |")]

# (table, id, column, dump name, fix-rule label, rules)
ROWS = [
    ("WorkflowActionForm", 495, "Header", "p15-WAF-495-Header", "venue-if", R_VENUE),
    ("WorkflowActionForm", 496, "Header", "p15-WAF-496-Header", "venue-if", R_VENUE),
    ("WorkflowActionForm", 497, "Header", "p15-WAF-497-Header", "venue-if", R_VENUE),
    ("WorkflowActionForm", 498, "Header", "p15-WAF-498-Header", "occurred-if", R_OCCURRED),
    ("WorkflowActionForm", 499, "Header", "p15-WAF-499-Header", "occurred-if", R_OCCURRED),
    ("WorkflowActionForm", 500, "Header", "p15-WAF-500-Header", "occurred-if", R_OCCURRED),
    ("AttributeValue", 3070786, "Value", "p15-AV-3070786", "venue-if", R_VENUE),
    ("AttributeValue", 80610647, "Value", "p15-AV-80610647", "venue-if", R_VENUE),
    ("AttributeValue", 80610655, "Value", "p15-AV-80610655", "venue-if", R_VENUE),
    ("AttributeValue", 80644504, "Value", "p15-AV-80644504", "venue-if", R_VENUE),
    ("WorkflowType", 71, "SummaryViewText", "p15-WT-71-Summary", "venue-if", R_VENUE),
    ("SystemCommunication", 62, "Body", "p15-SC-62", "venue-if", R_VENUE),
    ("AttributeValue", 248266514, "Value", "p15-AV-248266514", "benevolence-if", R_BENEVOLENCE),
    ("HtmlContent", 7906, "Content", "p15-HC-7906", "redirect-in-block", R_DEACON),
    ("HtmlContent", 10407, "Content", "p15-HC-10407", "parens-if", R_SF_IF),
    ("Block", 6928, "PreHtml", "p15-Block-6928-PreHtml", "parens-if", R_SF_IF + R_SF_ASSIGN),
    ("WorkflowActionForm", 569, "Header", "p15-WAF-569-Header", "rawvalue-bool", R_W9),
    ("ReportField", 64362, "Selection", "p15-RF-64362", "stray-hash", R_HOH),
    ("ReportField", 64364, "Selection", "p15-RF-64364", "stray-hash", R_HOH),
]

# AttributeValue Id -> (AttributeId, EntityId) on production; ids are not the same row on every database
AV_KEYS = {3070786: (1016, 791), 80610647: (1016, 6321), 80610655: (1016, 6318), 80644504: (1016, 6352),
           248266514: (17501, 8712)}

def q(s):
    assert all(ord(c) < 128 for c in s), s
    return "N'" + s.replace("'", "''") + "'"

def cnt(col, o):
    # DATALENGTH, not LEN: LEN drops trailing spaces
    return "(DATALENGTH(%s) - DATALENGTH(REPLACE(%s, %s, N''))) / DATALENGTH(%s)" % (col, col, q(o), q(o))

L = ["""-- ROCK9086 pass 12 (generated by gen_016.py): rows production logged under Fluid verification 2026-09-28..30.
-- Behaviour changes on DotLiquid (approved 2026-09-30):
--   venue-if / occurred-if (Event Requests external 71 and internal 72: forms, emails, summary, SystemCommunication 62)
--     and benevolence-if (Benevolence Request 715 card): a filter in an if was always true on DotLiquid, so the
--     "Other venue", "occurred before", spouse and "why unemployed" lines always showed; now only when they apply.
--   stray-hash (report 2069 TEST | PR Families): the head-of-household column was always empty; now it fills.
-- Same output on DotLiquid: redirect-in-block (Deacon/Elder application), parens-if (Sports & Fitness training),
--   rawvalue-bool (W-9 Form note). LavaProbe cases 33/34.
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
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 12: a guarded update did not match exactly one row; nothing committed.', 1; END
""")
L.append("\nUNION ALL\n".join(verify) + ";\n")
L.append("IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END\n")
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L))
print("rows:", len(ROWS), "->", OUT)
