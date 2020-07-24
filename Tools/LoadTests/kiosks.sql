SELECT 
	* 
FROM _org_secc_FamilyCheckin_Kiosk 
WHERE 
	NAME like '%KIOSK%' 
	AND KioskTypeId IS NOT NULL 
	AND KioskTypeId not in (40,42) order by newid();