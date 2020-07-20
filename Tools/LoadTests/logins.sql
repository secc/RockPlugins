SELECT g.id, g.name, ul.Username
FROM
    [UserLogin] ul
INNER JOIN Person p on ul.PersonId = p.Id
INNER JOIN [Group] g on p.PrimaryFamilyId = g.Id