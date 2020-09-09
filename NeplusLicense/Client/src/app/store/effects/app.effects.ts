import { Injectable } from '@angular/core';
import { Action, State, StateContext, Store } from '@ngxs/store';
import { mergeMap, tap } from 'rxjs/operators';
import { TokenExchangeModel, UsersModel } from '../../models';
import { AuthService } from '../../services';
import { ChangePasswordAction, InitUserLoginAction, LogoutAction, RefreshTokenAction, SessionExpiredAction, UserLoginAction } from '../actions';
import { AppStateModel, initialAppState } from '../models';

/*----------------------------------------
    login state section
-----------------------------------------*/

@State<AppStateModel>({
    name: 'appStore',
    defaults: initialAppState
})
@Injectable()
export class AppState {

    constructor(private auth: AuthService) { }

    private static updateLocal(user: UsersModel) {
        if (user === null) {
            localStorage.removeItem('token');
            localStorage.removeItem('refresh_token');
        } else {
            localStorage.setItem('token', user?.jwt?.token);
            localStorage.setItem('refresh_token', user?.refreshToken);

            const encodedUser = atob(user.jwt.token.split('.')[1]);
            const parsed = encodedUser ? JSON.parse(encodedUser) : {};
            localStorage.setItem('session_id', parsed.sid);
        }
    }

    @Action(UserLoginAction)
    login(ctx: StateContext<AppStateModel>, action: UserLoginAction) {
        // returning the response object instead of user object because of
        // we might get error message to display to the end user.
        return this.auth.login(action.payload).pipe(
            mergeMap(res => [ctx.dispatch(new InitUserLoginAction(res))])
        );
    }

    @Action(InitUserLoginAction)
    initUser(ctx: StateContext<AppStateModel>, action: InitUserLoginAction) {
        const user: UsersModel = action.response.contentBody;
        AppState.updateLocal(user);
        ctx.patchState({ activeUser: user, loginSuccess: action.response, logoutSuccess: null });
        return action.response;
    }

    @Action(ChangePasswordAction)
    changePassword(ctx: StateContext<AppStateModel>, action: ChangePasswordAction) {
        return this.auth.changePassword(action.payload)
            .pipe(tap(res => ctx.patchState({ changePasswordSuccess: res })));
    }

    @Action(LogoutAction)
    logout(ctx: StateContext<AppStateModel>) {
        localStorage.removeItem('session_id');
        const body: TokenExchangeModel = {
            accessToken: localStorage.getItem('token'),
            refreshToken: localStorage.getItem('refresh_token')
        };
        if (!(body.accessToken || body.refreshToken)) { return; }
        return this.auth.userLogout(body).pipe(
            // cleanup the local storage as well.
            tap(res => [ctx.patchState({ logoutSuccess: res }), AppState.updateLocal(null)])
        );
    }

    @Action(RefreshTokenAction)
    refreshToken(ctx: StateContext<AppStateModel>) {
        const payload: TokenExchangeModel = {
            accessToken: localStorage.getItem('token'),
            refreshToken: localStorage.getItem('refresh_token')
        };
        return this.auth.refreshToken(payload).pipe(
            mergeMap(res => ctx.dispatch(new InitUserLoginAction(res)))
        );
    }

    @Action(SessionExpiredAction)
    isExpired(ctx: StateContext<AppStateModel>, action: SessionExpiredAction) {
        const expiredSessionToken = +localStorage.getItem('session_id');
        ctx.patchState({ isSessionExpired: action.isExpired && expiredSessionToken === 0 });
    }


}
