import http from 'k6/http';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

export const requests = new Counter('http_reqs');
export const options = {
    vus: 3,
    scenarios: {
        sampleTest: {
            executor: 'constant-arrival-rate',
            exec: 'sampleTest',
            duration: '45s',
            rate: 5,
            timeUnit: `1s`,
            preAllocatedVUs: 3
        }
    }
};

export function sampleTest() {
    const res = http.get("https://localhost:7003/weatherforecast");
    check(res, {
        'status is 200': (r) => r.status === 200,
    });
}
