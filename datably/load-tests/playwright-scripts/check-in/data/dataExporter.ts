import d from './data.json';

type CheckInData = {
    Id: number;
    // ConfigurationId: number;
    ConfigurationName: string;
    // CampusId: number;
    // CampusName: string;
    // ScheduleId: number;
    // ScheduleName: string;
    // PrimaryFamilyId: number;
    Number: string;
    // KioskId: number;
}

export function getData(): CheckInData[] {
    return d as CheckInData[];
}

export function getUniqueConfigurationNames(): string[] {
    const data = getData();
    const configNames = data.map(d => d.ConfigurationName);
    const uniqueNames = configNames.filter((item, index, arr) => arr.indexOf(item) === index);
    return uniqueNames.sort();
}
