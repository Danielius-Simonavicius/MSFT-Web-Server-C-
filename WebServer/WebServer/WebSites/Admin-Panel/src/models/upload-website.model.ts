import { Guid } from 'guid-typescript';

export class UploadWebsite {
    name!: Guid;
    allowedHosts!: string;
    path!: string;
    defaultPage!: string;
    folder!: File;
}