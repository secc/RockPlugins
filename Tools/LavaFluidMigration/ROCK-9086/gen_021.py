"""ROCK-9086 pass 16: rows production logged under Fluid verification 2026-11-03 .. 2026-11-23.
Reads vals/p21-<Table>-<Id>-<Col>.txt (exact prod values), applies literal (old, new) rules, and writes
021_apply_prodpass10.sql with per-row guarded REPLACEs (same shape as 020)."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
V = os.environ.get("ROCK9086_VALS", os.path.join(HERE, "vals"))  # prod dumps are not committed; point ROCK9086_VALS at them
OUT = os.path.join(HERE, "021_apply_prodpass10.sql")
BS = chr(92)

# Report 1521 "Can Work with Minors By Campus" (page 1147), Lava report fields 67079 / 67094: parentheses in `assign`
# and `if` are a Fluid parse error ("A value was expected"). DotLiquid strips the parentheses in `assign`, and in `if`
# it reads `(a or b)` as `a` only: the clause that ends in `)` never matches (LavaProbe truth tables A-E).
# BEHAVIOUR CHANGE (asked for 2026-11-23): the conditions now do what the template says, matching report 1522.
R_HEAD = [
    ("{% assign add18years = (BirthDate | DateAdd:18,'y' | Date:'M/d/yyyy' ) %}",
     "{% assign add18years = BirthDate | DateAdd:18,'y' | Date:'M/d/yyyy' %}"),
    ("{% assign add60days = (add18years| DateAdd:60,'d' | Date:'M/d/yyyy' ) %}",
     "{% assign add60days = add18years | DateAdd:60,'d' | Date:'M/d/yyyy' %}"),
    ("{% assign DaysSinceBday = (BirthDate | DateAdd:18,'y' | Date:'M/d/yyyy' ) | DateDiff: 'Now','d' %}",
     "{% assign DaysSinceBday = BirthDate | DateAdd:18,'y' | Date:'M/d/yyyy' | DateDiff: 'Now','d' %}"),
    ("{% assign daysuntil= (add60days ) | DaysUntil %}",
     "{% assign daysuntil = add60days | DaysUntil %}"),
]
R_GRACE = R_HEAD + [
    # the grace period is 1-60 days left; `or` made the second half meaningless (DotLiquid: `daysuntil >= 1` only)
    ("{% if  Age == 18 and (daysuntil >=1 or daysuntil <=60)   %}", "{% if Age == 18 and daysuntil >= 1 and daysuntil <= 60 %}"),
]
R_SECURITY = R_HEAD + [
    # Adult rows. DotLiquid reads this as `daysuntil < 0` (18 and past the grace period). Kept that way: read
    # literally, `> 60` is everyone under 18, and every child would get four red adult rows on top of the minor rows.
    ("{% if (daysuntil < 0 or daysuntil > 60) %}", "{% if daysuntil < 0 %}"),
    ("{% if (CriminalCheckStatus == 'Approved') or (CriminalCheckStatus == 'Reapproved') %}",
     "{% if CriminalCheckStatus == 'Approved' or CriminalCheckStatus == 'Reapproved' %}"),
    # a filter inside `if`: DotLiquid ignored the filters and the comparison and tested the date for a value, so any
    # date read "more than 6 months ago". Now the 186 days are compared.
    ("{% if  Attendance1stVerifiedDate| Date:'M/d/yyyy' | DateDiff:'Now','d' > 186 %}",
     "{% assign daysSinceFirstAttend = Attendance1stVerifiedDate | DateDiff:'Now','d' %}{% if daysSinceFirstAttend > 186 %}"),
    # minor rows apply to a child or someone in the grace period (1-60 days left): `and`, as in report 1522.
    # Liquid has no parentheses and evaluates right to left, so this is Child or (>= 1 and <= 60) on both engines.
    ("{% if (AgeClassification == 'Child') or (daysuntil >= 1 or daysuntil <= 60) %}",
     "{% if AgeClassification == 'Child' or daysuntil >= 1 and daysuntil <= 60 %}"),
]
# Camp Financial Aid Report, page 1207 (Dynamic Data block 2350 FormattedOutput): the pass-15 string-vs-bool case.
# `isPublic` is the Public attribute's RawValue; `'False' == true` is TRUE on Fluid (LavaProbe B1/B2).
R_PUBLIC = [("{%if campus.IsActive == true and isPublic == true %}",
             "{%if campus.IsActive == true and isPublic == 'True' %}")]
# "Replace Newline Characters" actions 5302 (workflow type 379 Generic Text Response) and 5774 (495 Student Checkin):
# `Replace:'\n',newline`. DotLiquid looks for the two characters backslash + n; Fluid reads '\n' as a line break, finds
# none, and the SMS reply goes out with a visible "\n". Capture the two characters instead (LavaProbe N1/N2).
R_BSN = [("{% endcapture %}{{ Workflow | Attribute:'SMSResponse' | Replace:'" + BS + "n',newline}}",
          "{% endcapture %}{% capture bsn %}" + BS + "n{% endcapture %}{{ Workflow | Attribute:'SMSResponse' | Replace:bsn,newline}}")]
# Serve at Christmas, page 672 (Dynamic Data block 1186 FormattedOutput): unbalanced `(`. DotLiquid drops it (C1/C2).
R_PAREN920 = [("{% if (campus.ShortCode == '920' %}", "{% if campus.ShortCode == '920' %}")]
# Home Groups, page 1985 (Group Lava block 4501 LavaTemplate): an inline copy of HomeGroups.lava that still has the
# `when ... and` pass 6 removed from the theme files. DotLiquid ignores everything after the value (W cases).
R_WHEN = [("{%- when null and meetingType == '' -%}", "{%- when null -%}")]
# Take Away For Ferrari breadcrumbs, page 1537 (HtmlContent 999): a filter inside `if`. DotLiquid ignores the filter and
# the comparison, so the condition is always true and every story title prints (F1). BEHAVIOUR CHANGE: only the story
# whose Slug matches the page parameter prints now, which is what the template says.
R_SLUG = [("{% if item | Attribute:'Slug' == storySlug %}",
           "{% assign itemSlug = item | Attribute:'Slug' %}{% if itemSlug == storySlug %}")]
# Never Going Back share-ables, page 2464 (HtmlContent 4309): a comment inside a comment. Fluid ends the outer comment
# at the first endcomment ("Unknown tag 'endcomment'"). Drop the inner pair; everything stays commented out (K1/K2).
R_NESTED = [("\t\t{% comment %}", "\t\t"), ("\t\t{% endcomment %}", "\t\t")]

# (table, id, column, dump name, fix-rule label, rules)
ROWS = [
    ("ReportField", 67079, "Selection", "p21-RF-67079-Selection", "parens", R_GRACE),
    ("ReportField", 67094, "Selection", "p21-RF-67094-Selection", "parens", R_SECURITY),
    ("AttributeValue", 4517348, "Value", "p21-AV-4517348-Value", "string-vs-bool", R_PUBLIC),
    ("AttributeValue", 77085686, "Value", "p21-AV-77085686-Value", "backslash-n", R_BSN),
    ("AttributeValue", 79074431, "Value", "p21-AV-79074431-Value", "backslash-n", R_BSN),
    ("AttributeValue", 2453438, "Value", "p21-AV-2453438-Value", "unbalanced-paren", R_PAREN920),
    ("AttributeValue", 55093101, "Value", "p21-AV-55093101-Value", "when-and", R_WHEN),
    ("HtmlContent", 999, "Content", "p21-HC-999-Content", "filter-in-if", R_SLUG),
    ("HtmlContent", 4309, "Content", "p21-HC-4309-Content", "nested-comment", R_NESTED),
]

# AttributeValue Id -> (AttributeId, EntityId) on production
AV_KEYS = {4517348: (1201, 2350), 77085686: (17501, 5302), 79074431: (17501, 5774),
           2453438: (1201, 1186), 55093101: (81470, 4501)}

def q(s):
    assert all(ord(c) < 128 for c in s), s
    assert "\n" not in s and "\r" not in s, s  # single-line literals only: git may rewrite line endings
    return "N'" + s.replace("'", "''") + "'"

def cnt(col, o):
    # DATALENGTH, not LEN: LEN drops trailing spaces
    return "(DATALENGTH(%s) - DATALENGTH(REPLACE(%s, %s, N''))) / DATALENGTH(%s)" % (col, col, q(o), q(o))

L = ["""-- ROCK9086 pass 16 (generated by gen_021.py): rows production logged under Fluid verification 2026-11-03..11-23.
-- Same output on DotLiquid, except the two BEHAVIOUR CHANGE rules below:
--   string-vs-bool (Block 2350 FormattedOutput, Camp Financial Aid Report page 1207): `isPublic == true` on a
--     RawValue string; Fluid shows non-public campuses. Compare to 'True' (the pass-15 fix, LavaProbe B1/B2).
--   backslash-n (WorkflowActionType 5302 / 5774 "Replace Newline Characters"): `Replace:'\\n',newline` leaves a
--     visible \\n in the SMS reply on Fluid. Capture the two characters and replace that (LavaProbe N1/N2).
--   unbalanced-paren (Block 1186 FormattedOutput, Serve at Christmas page 672): `{% if (campus.ShortCode == '920' %}`.
--   when-and (Block 4501 LavaTemplate, Home Groups page 1985): `when null and meetingType == ''` -> `when null`.
--   nested-comment (HtmlContent 4309, Never Going Back share-ables page 2464): inner comment tags dropped.
-- BEHAVIOUR CHANGE on DotLiquid:
--   parens (ReportField 67079 / 67094, report 1521 Can Work with Minors By Campus, page 1147): parentheses in
--     `assign` and `if`. DotLiquid read `(a or b)` as `a` alone and ignored a filter inside `if`. The conditions now
--     do what the template says: grace period is 1-60 days left, attendance compares 186 days (as in report 1522).
--   filter-in-if (HtmlContent 999, Take Away For Ferrari breadcrumbs page 1537): `{% if item | Attribute:'Slug' ==
--     storySlug %}` is always true on DotLiquid, so every story title printed. Now only the matching story prints.
-- Guarded REPLACE per row, @DryRun = 1 rolls back, the live run is all-or-nothing, one backup per row to
-- dbo._ROCK9086_LavaBackup, rows absent from the target database log -1. Clear the Rock cache after.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
DECLARE @avkeys TABLE (Id int, AttributeId int, EntityId int);
INSERT @avkeys VALUES """ + (", ".join("(%d, %d, %d)" % (k, v[0], v[1]) for k, v in sorted(AV_KEYS.items())) or "(0, 0, 0)") + """;
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());
"""]
verify = []
NEWDIR = os.environ.get("ROCK9086_NEWVALS")  # optional: write the rewritten values here for LavaProbe
for tbl, i, col, dump, label, rules in ROWS:
    t = open(os.path.join(V, dump + ".txt"), encoding="utf-8", newline="").read()
    pairs = []
    for o, n in rules:
        c = t.count(o)
        if c:
            pairs.append((o, n, c))
    assert len(pairs) == len(rules), (tbl, i, "a rule did not match", [o for o, n in rules if o not in t])
    assert all(c == 1 for o, n, c in pairs), (tbl, i, "a rule matched more than once", [(o, c) for o, n, c in pairs if c != 1])
    new = t
    for o, n, c in pairs:
        new = new.replace(o, n)
    for o, n, c in pairs:
        assert o not in new or o in n, (tbl, i, o)
    if NEWDIR:
        open(os.path.join(NEWDIR, dump + ".new.txt"), "w", encoding="utf-8", newline="").write(new)
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
        print("%-15s %9d %-10s x%d  %s  ->  %s" % (tbl, i, col, c, o[:62].replace("\n", " "), n[:62].replace("\n", " ")))

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
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 16: a guarded update did not match exactly one row; nothing committed.', 1; END
""")
L.append("\nUNION ALL\n".join(verify) + ";\n")
L.append("IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END\n")
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L))
print("rows:", len(ROWS), "->", OUT)
