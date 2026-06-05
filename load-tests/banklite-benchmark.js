import http from "k6/http";
import { check, group, sleep } from "k6";
import { Trend, Rate } from "k6/metrics";

const BASE_URL = (__ENV.BASE_URL || "https://banklite.ca").replace(/\/$/, "");
const EMAIL = __ENV.BANKLITE_EMAIL || "";
const PASSWORD = __ENV.BANKLITE_PASSWORD || "";
const ACCOUNT_ID = __ENV.BANKLITE_ACCOUNT_ID || "";
const MODE = (__ENV.MODE || "public").toLowerCase();

const pageLoad = new Trend("banklite_page_load");
const authRead = new Trend("banklite_auth_read");
const successfulJourney = new Rate("banklite_successful_journey");

export const options = {
  scenarios: {
    public_benchmark: {
      executor: "ramping-vus",
      exec: "publicBenchmark",
      stages: [
        { duration: "30s", target: 100 },
        { duration: "1m", target: 300 },
        { duration: "2m", target: 600 },
        { duration: "2m", target: 1000 },
        { duration: "1m", target: 1000 },
        { duration: "30s", target: 0 },
      ],
    },
    authenticated_reads: {
      executor: "ramping-vus",
      exec: "authenticatedReads",
      startTime: "7m",
      stages: [
        { duration: "30s", target: MODE === "auth" ? 50 : 0 },
        { duration: "1m", target: MODE === "auth" ? 150 : 0 },
        { duration: "1m", target: MODE === "auth" ? 250 : 0 },
        { duration: "30s", target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(50)<250", "p(95)<1000", "p(99)<2000"],
    banklite_page_load: ["p(95)<800"],
    banklite_successful_journey: ["rate>0.99"],
  },
};

const htmlHeaders = {
  Accept: "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
};

const jsonHeaders = {
  Accept: "application/json",
  "Content-Type": "application/json",
  Origin: BASE_URL,
};

function recordTrend(trend, response) {
  trend.add(response.timings.duration);
}

function ok(response, expectedStatus = 200) {
  return response.status === expectedStatus;
}

export function publicBenchmark() {
  let passed = true;

  group("public pages", () => {
    const landing = http.get(`${BASE_URL}/`, { headers: htmlHeaders });
    recordTrend(pageLoad, landing);
    passed = check(landing, { "landing 200": (r) => ok(r) }) && passed;

    const login = http.get(`${BASE_URL}/index.html`, { headers: htmlHeaders });
    recordTrend(pageLoad, login);
    passed = check(login, { "login page 200": (r) => ok(r) }) && passed;

    const register = http.get(`${BASE_URL}/register.html`, { headers: htmlHeaders });
    recordTrend(pageLoad, register);
    passed = check(register, { "register page 200": (r) => ok(r) }) && passed;
  });

  group("static assets", () => {
    const css = http.get(`${BASE_URL}/css/styles.css`);
    recordTrend(pageLoad, css);
    passed = check(css, { "styles 200": (r) => ok(r) }) && passed;

    const apiJs = http.get(`${BASE_URL}/js/api.js`);
    recordTrend(pageLoad, apiJs);
    passed = check(apiJs, { "api js 200": (r) => ok(r) }) && passed;
  });

  successfulJourney.add(passed);
  sleep(1);
}

export function authenticatedReads() {
  if (MODE !== "auth" || !EMAIL || !PASSWORD) {
    successfulJourney.add(true);
    sleep(1);
    return;
  }

  const cookieJar = new http.CookieJar();
  const params = { headers: jsonHeaders, cookieJar };
  let passed = true;

  const login = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    params,
  );
  recordTrend(authRead, login);
  passed = check(login, { "auth login 200": (r) => ok(r) }) && passed;

  if (!passed) {
    successfulJourney.add(false);
    sleep(1);
    return;
  }

  const profile = http.get(`${BASE_URL}/api/User/profile`, params);
  recordTrend(authRead, profile);
  passed = check(profile, { "profile 200": (r) => ok(r) }) && passed;

  const accounts = http.get(`${BASE_URL}/api/Account`, params);
  recordTrend(authRead, accounts);
  passed = check(accounts, { "accounts 200": (r) => ok(r) }) && passed;

  if (ACCOUNT_ID) {
    const transactions = http.get(
      `${BASE_URL}/api/Transaction/${ACCOUNT_ID}?page=1&pageSize=10`,
      params,
    );
    recordTrend(authRead, transactions);
    passed = check(transactions, { "transactions 200": (r) => ok(r) }) && passed;
  }

  const logout = http.post(`${BASE_URL}/api/Auth/refresh/logout`, null, params);
  recordTrend(authRead, logout);
  passed = check(logout, { "logout 200": (r) => ok(r) }) && passed;

  successfulJourney.add(passed);
  sleep(1);
}
