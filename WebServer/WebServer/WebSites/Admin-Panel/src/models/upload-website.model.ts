import { Guid } from 'guid-typescript';

export class UploadWebsite {
    websiteName!: Guid;
    allowedHosts!: string;
    path!: string;
    defaultPage!: string;
    folder!: File;
}