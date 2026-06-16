import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class MailService {
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    static postMailSend(requestBody) {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/mail/send',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
