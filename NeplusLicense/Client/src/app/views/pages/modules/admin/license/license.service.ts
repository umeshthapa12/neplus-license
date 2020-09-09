import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { BaseUrlCreator, ParamGenService } from '../../../../../../../src/app/utils';

@Injectable()
export class LicenseService {

    private readonly api = this.url.createUrl('License', 'Lic');

    constructor(
        private http: HttpClient,
        private url: BaseUrlCreator,
        private paramGen: ParamGenService
    ) { }

    getVendors<T extends any>(params?: any): Observable<T> {
        return this.http.get<T>(`${this.api}/GetVendors`);
    }

    createRequest<T extends any>(body: any): Observable<T> {
        return this.http.post<T>(`${this.api}/CreateRequest`, body);
    }

    getList(): Observable<any> {
        return of(DATA);
    }
}

let DATA = [
    { id: 23, name: 'Vendor Name', moduleName: 'neplus.admin.hrm', email: 'user@gmail.com', licenseType: 'Trial', ClientGuid: 'HHSAS', createdOn: '2020-04-04' },
    { id: 253, name: 'Vendor Name', moduleName: 'neplus.admin.hrm', email: 'user@gmail.com', licenseType: 'License', ClientGuid: 'HFHSAS', createdOn: '2020-04-04' },
    { id: 2523, name: 'Vendor Name', moduleName: 'neplus.admin.hrm', email: 'user@gmail.com', licenseType: 'License', ClientGuid: 'HFHSAS', createdOn: '2020-04-04' }
];

