import { Routes } from '@angular/router';
import { AuthGuard } from '../../../../services/auth.guard';
import { BaseComponent } from '../../../../views/theme/base/base.component';
import { ErrorPageComponent } from '../../../../views/theme/content/error-page/error-page.component';

export const adminRoutes: Routes = [

    {
        path: 'main/admin',
        component: BaseComponent,
        data: {
            moduleName: 'neplus.module.admin',
            moduleDisplayName: 'License Manager'
        },
        // canActivate: [AuthGuard],
        children: [
            {
                path: 'dashboard',
                loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule),
            },
            {
                path: 'license',
                loadChildren: () => import('./license/license.module').then(m => m.LicenseModule),
            },
            {
                path: 'error/403',
                component: ErrorPageComponent,
                data: {
                    type: 'error-v6',
                    code: 404,
                    title: '403... Access forbidden',
                    desc: 'Looks like you don\'t have permission to access for requested page.<br> Please, contact administrator',
                },
            },
            { path: '', redirectTo: 'main/welcome', pathMatch: 'full' },
            { path: '**', redirectTo: 'error/403', pathMatch: 'full' },
        ],
    },
];
