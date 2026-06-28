/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { SendMailRequest } from '../models/SendMailRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class MailService {
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static postMailSend(
        requestBody?: SendMailRequest,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/mail/send',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
