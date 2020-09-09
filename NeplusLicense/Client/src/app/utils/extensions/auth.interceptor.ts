import { Location } from '@angular/common';
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngxs/store';
import { BehaviorSubject, fromEvent, Observable, of, throwError } from 'rxjs';
import { catchError, filter, finalize, switchMap, take } from 'rxjs/operators';
import { UsersModel } from '../../models';
import { LogoutAction, RefreshTokenAction, SessionExpiredAction } from '../../store/actions';
import { AuthService } from '../../services/auth.service';

@Injectable()
export class RefreshTokenInterceptor implements HttpInterceptor {

    private isLoginPage: boolean;

    private isRefreshingToken = false;
    /* Refresh Token Subject tracks the current token, or is null if no token is currently
      available (e.g. refresh pending).*/
    private tokenSubject: BehaviorSubject<any> = new BehaviorSubject<boolean>(false);
    constructor(
        private store: Store,
        private location: Location,
        private router: Router,
        private auth: AuthService) {
        fromEvent(window, 'storage').subscribe({
            next: (e: StorageEvent) => {

                if (e.key === null || ((e.key === 'token' || e.key === 'refresh_token') && !e.newValue)) {
                    this.validateUrlAndRedirect();
                    this.store.dispatch(new LogoutAction());
                }
            }
        });
    }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return next.handle(this.addTokenToRequest(request))
            .pipe(
                catchError(res => {
                    if (res instanceof HttpErrorResponse) {
                        // server returns the token expired header so we need to refresh it,
                        const expiredToken = res.headers.get('token-expired');
                        switch (res.status) {
                            case 401:
                                // it means refresh the token
                                if (expiredToken) {
                                    return this.handle401Error(request, next);
                                } else {
                                    // it means need to re-authenticate
                                    this.validateUrlAndRedirect();
                                    this.store.dispatch(new SessionExpiredAction(true));
                                    return of(null);
                                }
                            default:
                                // if we have no valid data on the local storage
                                if (this.auth.isValidAuth()) {
                                    this.validateUrlAndRedirect();
                                    this.store.dispatch(new SessionExpiredAction(true));
                                    return of(null);
                                }
                                return throwError(res);

                        }

                    } else {
                        this.store.dispatch(new SessionExpiredAction(false));
                    }
                }));
    }

    // appends required tokens
    private addTokenToRequest(request: HttpRequest<any>): HttpRequest<any> {

        return request.clone({
            withCredentials: true,
            setHeaders: {
                Authorization: `Bearer ${localStorage.getItem('token')}`
            }
        });
    }

    private handle401Error(request: HttpRequest<any>, next: HttpHandler) {

        if (!this.isRefreshingToken) {
            this.isRefreshingToken = true;

            // Reset here so that the following requests wait until the token
            // comes back from the refreshToken call.
            this.tokenSubject.next(false);

            return this.store.dispatch(new RefreshTokenAction())
                .pipe(
                    switchMap((user: UsersModel) => {
                        if (user) {
                            this.tokenSubject.next(true);
                            return next.handle(this.addTokenToRequest(request));
                        }

                        setTimeout(() => {
                            this.store.dispatch(new SessionExpiredAction(true));
                        }, 1000);

                        return throwError('error');
                    }),
                    catchError(err => {
                        if (err instanceof HttpErrorResponse) {
                            setTimeout(() => {
                                this.store.dispatch(new SessionExpiredAction(true));

                            }, 1000);
                            this.validateUrlAndRedirect();
                        }
                        return throwError(err);
                    }),
                    finalize(() => {
                        this.isRefreshingToken = false;
                    })
                );
        } else {
            this.isRefreshingToken = false;

            return this.tokenSubject.pipe(filter(isSuccess => isSuccess), take(1),
                switchMap(() => next.handle(this.addTokenToRequest(request)))
            );
        }
    }

    private validateUrlAndRedirect() {
        const path = this.location.path();
        this.isLoginPage = path.search('/auth/login') >= 0;
        if (!this.isLoginPage) {
            this.router.navigate(['/auth/login'], { queryParams: { returnUrl: this.location.path() } });
        } else {
            this.router.navigate(['/auth/login'], { queryParams: null });
        }
    }
}
