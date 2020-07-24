select
	ROW_NUMBER() OVER(ORDER BY f.Id ASC) AS Row#,
    f.Name,
	f.Id,
	count(*),
	right(max(pn.number), 4)
from person p
	INNER JOIN PhoneNumber pn on p.Id = pn.PersonId
	INNER JOIN GroupMember gm on p.Id = gm.PersonId
	INNER JOIN [Group] f on gm.GroupId = f.Id and f.GroupTypeId = 10
where RecordStatusValueId = 3
group by f.id, f.name
having count(*) >= 3;