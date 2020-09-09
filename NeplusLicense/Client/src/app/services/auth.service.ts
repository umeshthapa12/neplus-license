import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ResponseModel, TokenExchangeModel, UsersModel } from '../models';
import { BaseUrlCreator } from '../utils/extensions/base-url';

@Injectable({ providedIn: 'root' })
export class AuthService {

    private api = this.url.createUrl('Account', 'Admin');

    constructor(
        private http: HttpClient,
        private url: BaseUrlCreator
    ) { }


    /**
     * User login with credentials.
     * @param body username and password payload.
     */
    login = (body: UsersModel) => this.http.post<ResponseModel>(`${this.api}/Login`, body);


    changePassword = (body: { currentPassword: string, newPassword: string }) =>
        this.http.post<ResponseModel>(`${this.api}/ChangePassword`, body)

    /**
     * Refresh authentication token to access authorized resources if token expired.
     * @param body refresh token exchange model (payload)
     */
    refreshToken = (body: TokenExchangeModel) => this.http.post<ResponseModel>(`${this.api}/RefreshToken`, body);

    /**
     * log out active user authentication from server & client
     * @param {TokenExchangeModel} body payload to look for user and active token to invalidate/remove
     * @returns {Observable<ResponseModel>} success message
     */
    userLogout(body: TokenExchangeModel): Observable<ResponseModel> {
        return this.http.post<ResponseModel>(`${this.api}/Logout`, body);
    }

    isValidAuth = () => {
        const refresh = localStorage.getItem('refresh_token');
        const token = localStorage.getItem('token');
        const sessionId = +localStorage.getItem('session_id');
        return !(refresh || token || sessionId > 0);
    }

    /**
     * get user site navigation
     */
    getSiteNav(): Observable<ResponseModel> {

        const u = `${this.url.createUrl('Permission', 'admin')}/GetSideNav`;

        return this.http.get<ResponseModel>(u, {
            // this will make authentication of cookie of origins
            withCredentials: true,
        });

    }
}
