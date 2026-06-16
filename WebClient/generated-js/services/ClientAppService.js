import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ClientAppService {
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    static postClientAppRegister(requestBody) {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/client-app/register',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
