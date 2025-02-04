import d from './data.json';
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

export default function getData(set: 0 | 1 | 2 | 3): CheckInData[] {
    switch (set) {
        case 0:
            return d as CheckInData[];
        case 1:
            return d1 as CheckInData[];
        case 2:
            return d2 as CheckInData[];
        case 3:
            return d3 as CheckInData[];
    }
}
