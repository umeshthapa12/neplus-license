import { Component, OnInit, ChangeDetectorRef, OnDestroy, Inject } from '@angular/core';
import { FormGroup, FormBuilder } from '@angular/forms';
import { Subject } from 'rxjs';
import { filter, takeUntil, delay } from 'rxjs/operators';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { fadeIn, fadeInOutStagger } from '../../../../../../../src/app/utils';
import { ErrorCollection, ResponseModel } from '../../../../../../../src/app/models';
import {
    ChangesConfirmComponent, AlertTypeClass, AlertType, SnackbarModel, SnackToastService
} from '../../../../../../../src/app/views/shared';
import { LicenseService } from './license.service';

@Component({
    templateUrl: './license-form.component.html',
    animations: [fadeIn, fadeInOutStagger],
    styleUrls: ['../license/license.scss'],
})
export class LicenseFormComponent implements OnInit, OnDestroy {
    private readonly toDestroy$ = new Subject<void>();

    licenseForm: FormGroup;

    responseMessage: string;
    isError: boolean;
    errors: ErrorCollection[];
    isWorking: boolean;

    constructor(
        private cdr: ChangeDetectorRef,
        private fb: FormBuilder,
        private dialog: MatDialog,
        private licenseService: LicenseService,
        private nodify: SnackToastService,
        private dialogRef: MatDialogRef<LicenseFormComponent>,
        @Inject(MAT_DIALOG_DATA)
        public data: any
    ) { }

    ngOnInit() {
        this.initForm();
    }

    private initForm() {
        this.licenseForm = this.fb.group({
            id: 0,
            trialExpireDate: null,
            licenseExpireDate: null
        });
    }

    saveChanges() {
        let value: any = this.data;
        let tDate = this.licenseForm.controls.trialExpireDate.value;
        let licDate = this.licenseForm.controls.licenseExpireDate.value;
        this.isWorking = true;

        value.forEach(x => {
            if (x.licenseType === 'Trail' || x.licenseType === 'Trial') {
                x.expiresDate = tDate;
                let l = x.licenseType;
                let name: string = x.moduleName;
                let n = JSON.stringify(name);
                let p = {
                    [n]: licDate
                };
                p[n] = x.licenseType;
                x.package = p;
            } else {
                x.expiresDate = licDate;

                let l = x.licenseType;
                let name: string = x.moduleName;
                let n= JSON.stringify(name);
                let p = {
                    [n]: licDate
                };
                p[n] = x.licenseType;
                x.package = p;
            }
        });
        this.licenseService.createRequest(value)
            .pipe(
                takeUntil(this.toDestroy$),
                delay(1500))
            .subscribe(res => [
                this.isWorking = false,
                this.dialogRef.close(res),
                this.isWorking = false,
                this.onSuccess(res),
            ], e => this.onError(e));
    }

    save() {
        const value: any = this.data;
        const tDate = this.licenseForm.controls.trialExpireDate.value;
        const licDate = this.licenseForm.controls.licenseExpireDate.value;

        let b = value.find(c => c);
        if (b.licenseType === 'Trial') {
            value.forEach(d => d.expiresDate = tDate);
        } else {
            value.forEach(d => d.expiresDate = licDate);
        }

        const name = b.moduleName;
        const n = JSON.stringify(name);
        const lic = b.licenseType;
        const p = {
            [n]: lic
        };
        p[n] = b.licenseType;
        value.forEach(t => t.package = p);
        // console.log(value);
        // this.licenseService.createRequest(value).subscribe(_ => console.log(_));

        // this.licenseService.createRequest(value)
        //     .pipe(
        //         takeUntil(this.toDestroy$),
        //         delay(1500))
        //     .subscribe(res => [
        //         this.dialogRef.close(res),
        //         this.isWorking = false,
        //         this.onSuccess(res),
        //     ], e => this.onError(e));
    }

    private onSuccess(res: ResponseModel) {

        this.cdr.markForCheck();
        const config: SnackbarModel = {
            title: 'Success',
            typeClass: AlertTypeClass.success,
            message: (res.messageBody || 'Success'),
            type: AlertType.success
        };
        // init snackbar
        this.nodify.initSnakbar(config);

    }

    private onError(ex: any) {

        this.cdr.markForCheck();

        /* handle error*/
        const c: SnackbarModel = {
            title: (ex.statusText || 'Error'),
            type: AlertType.Danger,
            typeClass: AlertTypeClass.Danger,
            message: (ex.error && ex.error.messageBody || 'Something went wrong.')
        };

        this.nodify.initSnakbar(c);
    }

    cancel() {
        if (this.licenseForm.dirty) {
            this.dialog.open(ChangesConfirmComponent).afterClosed()
                .pipe(
                    filter(_ => _)
                ).subscribe(_ => this.dialogRef.close());
        } else {
            this.dialogRef.close();
        }

    }

    ngOnDestroy() {
        this.toDestroy$.next();
        this.toDestroy$.complete();
    }

}
