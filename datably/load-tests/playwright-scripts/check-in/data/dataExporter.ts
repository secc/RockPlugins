import d from './data.json';

type CheckInData = {
    Id: number;
    ConfigurationId: number;
    ConfigurationName: string;
    CampusId: number;
    CampusName: string;
    ScheduleId: number;
    ScheduleName: string;
    PrimaryFamilyId: number;
    Number: string;
}

const activeCheckIns: string[] = [
    'BB Kids',
    'BB Kids and Volunteers',
    'BB Other Volunteers'
]

export default function getData(): CheckInData[] {
    var data = d as CheckInData[];
    data = data.filter(d => activeCheckIns.includes(d.ConfigurationName));
    return data;
}