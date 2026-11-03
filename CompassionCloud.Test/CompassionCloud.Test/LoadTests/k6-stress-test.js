// k6 STRESS test for Compassion Cloud  (https://k6.io)
//
// Goal: find the breaking point. Load ramps well beyond normal traffic, then recovers.
// Run:  k6 run LoadTests/k6-stress-test.js
//       k6 run -e BASE_URL=https://<your-app>.azurewebsites.net LoadTests/k6-stress-test.js
// NOTE: Azure App Service B1 is a small plan - expect latency to climb at the higher stages; that is the finding.
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  insecureSkipTLSVerify: true,
  stages: [
    { duration: '30s', target: 10 },   // warm-up
    { duration: '1m',  target: 25 },   // above normal
    { duration: '1m',  target: 50 },   // stress
    { duration: '1m',  target: 100 },  // breaking-point search
    { duration: '30s', target: 0 },    // recovery
  ],
  thresholds: {
    http_req_failed:   ['rate<0.10'],   // tolerate up to 10% errors under stress
    http_req_duration: ['p(95)<5000'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:5031';

export default function () {
  const res = http.batch([
    ['GET', `${BASE}/`],
    ['GET', `${BASE}/Donation`],
    ['GET', `${BASE}/health`],
  ]);
  res.forEach((r) => check(r, { 'status is 200': (x) => x.status === 200 }));
  sleep(1);
}
