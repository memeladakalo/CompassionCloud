// k6 load test for Compassion Cloud  (https://k6.io)
//
// Run locally:   k6 run LoadTests/k6-load-test.js
// Run vs Azure:  k6 run -e BASE_URL=https://<your-app>.azurewebsites.net LoadTests/k6-load-test.js
//
// Scenario: 5 concurrent users for 30 seconds browsing public pages and
// submitting an anonymous donation form.
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 5,                // 5 concurrent virtual users
  duration: '30s',
  insecureSkipTLSVerify: true,   // allow the local dev certificate
  thresholds: {
    http_req_failed:   ['rate<0.01'],   // <1% errors
    http_req_duration: ['p(95)<2000'],  // 95% of requests under 2s
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:5031';

export default function () {
  // 1. Public pages
  for (const path of ['/', '/Home/About', '/Donation', '/health']) {
    const res = http.get(`${BASE}${path}`);
    check(res, { [`GET ${path} is 200`]: (r) => r.status === 200 });
    sleep(1);
  }

  // 2. Anonymous donation form (needs the anti-forgery token from the GET)
  const page = http.get(`${BASE}/Donation`);
  const token = page.html().find('input[name="__RequestVerificationToken"]').first().attr('value');

  const post = http.post(`${BASE}/Donation`, {
    DonorName: `Load Tester ${__VU}`,
    DonorEmail: `load${__VU}@test.com`,
    Amount: '100',
    Currency: 'ZAR',
    DonationType: 'One-time',
    IsAnonymous: 'true',
    __RequestVerificationToken: token,
  });
  check(post, { 'donation accepted (redirect followed to 200)': (r) => r.status === 200 });
  sleep(1);
}
