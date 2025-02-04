import d1 from './data1.json';
import d2 from './data2.json';
import d3 from './data3.json';

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

export default function getData(set: 1 | 2 | 3): CheckInData[] {
    switch (set) {
        case 1:
            return (d1 as CheckInData[]).filter(d => activeCheckIns.includes(d.ConfigurationName));
        case 2:
            return (d2 as CheckInData[]).filter(d => activeCheckIns.includes(d.ConfigurationName));
        case 3:
            return (d3 as CheckInData[]).filter(d => activeCheckIns.includes(d.ConfigurationName));
    }
}
