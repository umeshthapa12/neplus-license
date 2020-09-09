import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { adminRoutes } from './views/pages/modules/admin/pages.routes';
import { BaseComponent } from './views/theme/base/base.component';
import { ErrorPageComponent } from './views/theme/content/error-page/error-page.component';

const defaultRoutes: Routes = [

    { path: '', redirectTo: 'main/admin/dashboard', pathMatch: 'full' },
    { path: 'auth', loadChildren: () => import('./views/pages/auth/auth.module').then(m => m.AuthModule) },
    {
        path: '',
        component: BaseComponent,
        data: {
            moduleName: 'neplus.module.admin',
            moduleDisplayName: 'License Manager'
        },
        children: [
            {
                path: 'main/dashboard',
                loadChildren: () => import('./views/pages/modules/admin/dashboard/dashboard.module').then(m => m.DashboardModule)
            },
            // {
            //     path: 'error/403',
            //     component: ErrorPageComponent,
            //     data: {
            //         type: 'error-v6',
            //         code: 403,
            //         title: '403... Access forbidden',
            //         desc: 'Looks like you don\'t have permission to access for requested page.<br> Please, contact administrator',
            //     },
            // },
        ]
    },
    {
        path: 'error/403',
        component: ErrorPageComponent,
        data: {
            type: 'error-v6',
            code: 403,
            title: '403... Access forbidden',
            desc: 'Looks like you don\'t have permission to access for requested page.<br> Please, contact administrator',
        },
    },
    { path: 'main', redirectTo: 'main/dashboard', pathMatch: 'full' },
    { path: '**', redirectTo: 'error/403', pathMatch: 'full' },

];

const allRoutes = [

    // module pages routes
    ...adminRoutes,
];

@NgModule({
    imports: [
        RouterModule.forRoot([...allRoutes, ...defaultRoutes]),
    ],
    exports: [RouterModule],
})
export class AppRoutingModule { }
