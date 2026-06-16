/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { RegisterClientAppRequest } from '../models/RegisterClientAppRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ClientAppService {
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static postClientAppRegister(
        requestBody?: RegisterClientAppRequest,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/client-app/register',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
