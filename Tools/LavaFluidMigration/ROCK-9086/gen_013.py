"""Date-as-number fix (ROCK-9086 pass 10).
Rock's Fluid bridge (Rock.Lava.Fluid.FluidExtensions.ToRealObjectValue) casts every whole-number decimal to Int32, so a
date formatted as a 10+ digit number (yyMMddHHmm = 2609251846 today) overflows the moment a shortcode or a Rock filter
touches it. Putting a decimal point after the day keeps the integer part at 6-8 digits, and ordering/equality unchanged.
  --apply-files   rewrite the repo files in place (byte-exact, CRLF kept)
  (always)        write Tools/LavaFluidMigration/ROCK-9086/013_apply_datenumber.sql from scratchpad/vals/dn-hc-*.txt"""
import os, re, sys, hashlib
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = r"C:\Users\stephenl\source\repos\RockPlugins-16"
OUT = os.path.join(REPO, r"Tools\LavaFluidMigration\ROCK-9086\013_apply_datenumber.sql")
FMT = re.compile(r"(Date:\s*')(yyMMdd|yyyyMMdd)((?:HH|hh)mm(?:ss)?)('\s*\|\s*)As(Double|Decimal|Integer)\b")
# hard-coded yyyyMMddHHmm[ss] literals fed to AsDouble next to these formats
LIT = re.compile(r"'(\d{8})(\d{4}|\d{6})'(\s*\|\s*AsDouble|\s*-?%\}|\s*\|)")

def rewrite(t):
    t2, n1 = FMT.subn(lambda m: m.group(1) + m.group(2) + "." + m.group(3) + m.group(4) + ("AsDouble" if m.group(5) == "Integer" else "As" + m.group(5)), t)
    n2 = 0
    if n1:
        def lit(m):
            nonlocal n2; n2 += 1
            return "'" + m.group(1) + "." + m.group(2) + "'" + m.group(3)
        t2 = re.sub(r"'(\d{8})(\d{4}|\d{6})'(\s*\|\s*AsDouble)", lit, t2)
        t2 = t2.replace("Default:'99999999999999'", "Default:'99999999.999999'")
        n2 += t.count("Default:'99999999999999'")
    return t2, n1, n2

if "--apply-files" in sys.argv:
    roots = [os.path.join(REPO, r"Plugins\org.secc.Themes\Themes"), os.path.join(REPO, "Content")]
    for root in roots:
        for d, _, fs in os.walk(root):
            for f in fs:
                if not f.endswith(".lava"): continue
                p = os.path.join(d, f)
                b = open(p, "rb").read()
                t = b.decode("utf-8")
                t2, n1, n2 = rewrite(t)
                if n1:
                    assert t2.count("\r\n") == t.count("\r\n") and t2.count("\n") == t.count("\n")
                    open(p, "wb").write(t2.encode("utf-8"))
                    print("file %-100s formats=%d literals=%d" % (os.path.relpath(p, REPO), n1, n2))

def q(s):
    assert all(ord(c) < 128 for c in s), s
    return "N'" + s.replace("'", "''") + "'"

rows = []
for f in sorted(os.listdir(os.path.join(HERE, "vals"))):
    if not f.startswith("dn-hc-"): continue
    i = int(f[6:-4])
    t = open(os.path.join(HERE, "vals", f), encoding="utf-8", newline="").read()
    t2, n1, n2 = rewrite(t)
    if not n1: continue
    # express the change as exact (old, new) snippet pairs, one per distinct changed tag
    pairs = {}
    for m in re.finditer(r"\{%-?[^%]*%\}", t):
        tag = m.group(0); nt, a, b = rewrite(tag)
        if nt != tag: pairs[tag] = nt
    # literal lines are separate assign tags without a Date format - rewrite them with the literal rule directly
    for m in re.finditer(r"\{%-?[^%]*'\d{12,14}'[^%]*%\}", t):
        tag = m.group(0)
        nt = re.sub(r"'(\d{8})(\d{4}|\d{6})'(\s*\|\s*AsDouble)", lambda mm: "'" + mm.group(1) + "." + mm.group(2) + "'" + mm.group(3), tag)
        nt = nt.replace("Default:'99999999999999'", "Default:'99999999.999999'")
        if nt != tag: pairs[tag] = nt
    applied = t
    for o, n in pairs.items(): applied = applied.replace(o, n)
    assert applied == t2 or all(k in t for k in pairs), f
    rows.append((i, [(o, n, t.count(o)) for o, n in sorted(pairs.items())]))

L = ["""-- ROCK9086 pass 10: dates stored as 10-14 digit numbers (Date:'yyMMddHHmm' | AsDouble and friends).
-- Rock's Fluid bridge (Rock.Lava.Fluid.FluidExtensions.ToRealObjectValue, line 247) returns (int) for every whole-number
-- decimal, so 2609251846 (today as yyMMddHHmm) throws "Value was either too large or too small for an Int32" as soon as a
-- shortcode collects merge fields or a Rock filter (Plus, etc.) receives it. Dates before 2021-06 fit in Int32, which is why
-- this only started recently. Fix: a decimal point after the day (yyMMdd.HHmm, yyyyMMdd.HHmm[ss]) - ordering and equality are
-- unchanged, the whole part stays at 6-8 digits. Hard-coded yyyyMMddHHmm literals get the same point. LavaProbe U1-U4, V1-V4.
-- HtmlContent 4257 used AsInteger on yyMMddHHmm, which overflows on DotLiquid as well (broken since mid-2021): now AsDouble.
-- HtmlContent 10546 keeps its 12-hour 'hh' (a pre-existing quirk; changing it would change behaviour).
-- Same shape as 005-012: guarded REPLACE per row, @DryRun = 1 rolls back, live run is all-or-nothing, backups to
-- dbo._ROCK9086_LavaBackup, rows absent from the target database log -1. Clear the Rock cache afterwards.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());
"""]
ids = ", ".join(str(r[0]) for r in rows)
L.append("INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT 'HtmlContent', x.[Id], 'Content', x.[Content] FROM HtmlContent x WHERE x.[Id] IN (%s)\n"
         "  AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'HtmlContent' AND b.Id = x.[Id] AND b.Col = 'Content');\n" % ids)
for i, pairs in rows:
    expr = "[Content]"
    for o, n, c in pairs: expr = "REPLACE(%s, %s, %s)" % (expr, q(o), q(n))
    conds = ["[Id] = %d" % i] + ["(LEN([Content]) - LEN(REPLACE([Content], %s, N''))) / LEN(%s) = %d" % (q(o), q(o), c) for o, n, c in pairs]
    L.append("UPDATE HtmlContent SET [Content] = %s\nWHERE %s\n  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);" % (expr, "\n  AND ".join(conds)))
    L.append("INSERT @log VALUES ('HtmlContent', %d, 'date-number', @@ROWCOUNT);" % i)
L.append("""
UPDATE l SET [Rows] = -1 FROM @log l WHERE l.[Rows] = 0 AND NOT EXISTS (SELECT 1 FROM HtmlContent x WHERE x.Id = l.Id);
SELECT * FROM @log ORDER BY Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1)) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1))
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 10: a guarded update did not match exactly one row; nothing committed.', 1; END

SELECT h.Id, (LEN(h.Content) - LEN(REPLACE(h.Content, N'MMddHHmm'' |', N''))) / 11 + (LEN(h.Content) - LEN(REPLACE(h.Content, N'MMddhhmmss'' |', N''))) / 13 StillOld,
       (LEN(h.Content) - LEN(REPLACE(h.Content, N'MMdd.', N''))) / 5 NewFormats
FROM HtmlContent h WHERE h.Id IN (""" + ids + """) ORDER BY h.Id;

IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END
""")
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(L))
for i, pairs in rows:
    for o, n, c in pairs: print("hc %5d x%d  %s  ->  %s" % (i, c, o.strip()[:95], n.strip()[:95]))
print("sql rows:", len(rows))
