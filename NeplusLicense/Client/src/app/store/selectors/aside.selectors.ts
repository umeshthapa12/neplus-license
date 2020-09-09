import { Selector, createSelector } from '@ngxs/store';
import _ from 'lodash';
import { AsideNavState } from '../effects';
import { SiteNavStateModel } from '../models';
import {AsideNavModel} from "../../models";

/**
 * App state selector
 */
export class AsideStateSelector {

    /**
     * Gets a slice of left aside.
     * @param stateKay name of the state key to get slice.
     */
    static SliceOf<K extends keyof SiteNavStateModel>(stateKay: K) {
        return createSelector([AsideNavState], (state: SiteNavStateModel) => {
            // since the state props are readonly, we need to copy for our mutation.
            switch (stateKay) {
                case 'filteredItems':
                    // return _.cloneDeep(state.filteredItems);
                    return _.cloneDeep(AsideStorage);
                case 'moduleNames':
                    return _.cloneDeep(state.masterAside).map(m => m.module);
                // more
            }
        });
    }
}
//
const AsideStorage:any[] = [
    {
        "id": 1,
        "title": "Dashboard",
        "root": true,
        "icon": "flaticon-home",
        "page": "/main/admin/dashboard",
        "rowOrder": 1,
        "module": "neplus.module.admin"
    },
    {
        "id": 2,
        "title": "License Manager",
        "root": true,
        "icon": "fa fa-certificate",
        "page": "/main/admin/license",
        "rowOrder": 2,
        "module": "neplus.module.admin"
    }
];
